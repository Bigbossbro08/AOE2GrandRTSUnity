using System;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEditor.Rendering;
using UnityEngine;

public interface IDeterministicUpdate
{
    void DeterministicUpdate(float deltaTime, ulong tickID);
}

public static class TypeIndex
{
    private static readonly Dictionary<Type, int> typeToIndex = new();
    private static int nextIndex = 0;

    public static int GetIndex<T>()
    {
        Type type = typeof(T);
        if (!typeToIndex.TryGetValue(type, out int index))
        {
            index = nextIndex++;
            typeToIndex[type] = index;
        }
        return index;
    }
}


public class DeterministicTimer
{
    private struct TimerEvent
    {
        public float TimeRemaining;
        public int EventID;

        public TimerEvent(float time, int eventID)
        {
            TimeRemaining = time;
            EventID = eventID;
        }
    }

    private List<TimerEvent> activeTimers = new List<TimerEvent>(32); // Pre-allocate for efficiency
    private Queue<TimerEvent> timerPool = new Queue<TimerEvent>(32); // Object pooling

    private Dictionary<int, Action> eventCallbacks = new Dictionary<int, Action>();
    private int eventCounter = 0;

    // Add a new deterministic timer with an event ID
    public int AddTimer(float duration, Action callback)
    {
        int eventID = ++eventCounter;
        eventCallbacks[eventID] = callback;

        if (timerPool.Count > 0)
        {
            TimerEvent reusedTimer = timerPool.Dequeue();
            reusedTimer.TimeRemaining = duration;
            reusedTimer.EventID = eventID;
            activeTimers.Add(reusedTimer);
        }
        else
        {
            activeTimers.Add(new TimerEvent(duration, eventID));
        }

        return eventID;
    }

    // Remove a timer by event ID (useful if unit is destroyed)
    public void RemoveTimer(int eventID)
    {
        for (int i = activeTimers.Count - 1; i >= 0; i--)
        {
            if (activeTimers[i].EventID == eventID)
            {
                timerPool.Enqueue(activeTimers[i]); // Recycle object
                activeTimers.RemoveAt(i);
                eventCallbacks.Remove(eventID);
                break;
            }
        }
    }

    // Update function inside deterministic simulation loop
    public void Update(float fixedDeltaTime)
    {
        for (int i = activeTimers.Count - 1; i >= 0; i--)
        {
            TimerEvent timer = activeTimers[i];
            timer.TimeRemaining -= fixedDeltaTime;

            if (timer.TimeRemaining <= 0f)
            {
                if (eventCallbacks.TryGetValue(timer.EventID, out var callback))
                {
                    callback.Invoke();
                    eventCallbacks.Remove(timer.EventID);
                }

                timerPool.Enqueue(timer); // Recycle object
                activeTimers.RemoveAt(i);
            }
            else
            {
                activeTimers[i] = timer;
            }
        }
    }

    // Cleanup all timers
    public void ClearAllTimers()
    {
        activeTimers.Clear();
        eventCallbacks.Clear();
    }
}

public class DeterministicUpdateManager : MonoBehaviour
{
    public static DeterministicUpdateManager Instance { get; private set; }

    public ulong tickCount = 0;
    public float elapsedTime = 0.0f;

    public int seed = 42;

    private float accumulatedTime = 0f;
    public const float FixedStep = 1/ 25f; // 60 Hz
    public DeterministicTimer timer = new DeterministicTimer();
    public DeterministicCoroutineManager CoroutineManager { get; private set; }

    bool initialized = false;

    private bool paused = true;             // start paused until you explicitly start

    // **Use a list array instead of a dictionary**
    private readonly List<IDeterministicUpdate>[] categorizedObjects = new List<IDeterministicUpdate>[64];

    // **Use a list array instead of a dictionary**
    private readonly List<IDeterministicUpdate>[] categorizedPostPhysicsObjects = new List<IDeterministicUpdate>[64];

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        UnityEngine.Random.InitState(seed);

        enabled = false;
        Time.fixedDeltaTime = FixedStep;
        Physics.simulationMode = SimulationMode.Script;
        CoroutineManager = new DeterministicCoroutineManager(); ;
    }

    private void Update()
    {
        //if (!initialized)
        //{
        //    CoroutineManager = new DeterministicCoroutineManager();
        //}

        // 1) Always run input & networking
        InputManager.Instance.NetworkTick();

        // 2) Only run deterministic simulation when unpaused
        if (!paused)
        {
            accumulatedTime += Time.deltaTime;
            while (accumulatedTime >= FixedStep)
            {
                accumulatedTime -= FixedStep;

                // a) input callbacks
                InputManager.Instance.DeterministicUpdate(FixedStep, tickCount);

                // b) game logic
                RunDeterministicUpdate(FixedStep, tickCount);

                // c) pathfinding, timers, physics
                if (PathfindingManager.Instance.enabled)
                    PathfindingManager.Instance.DeterministicUpdate(FixedStep, tickCount);
                
                CoroutineManager?.Tick();

                timer.Update(FixedStep);
                Physics.Simulate(FixedStep);
                
                PostPhysicsUpdate(FixedStep, tickCount);

                elapsedTime += FixedStep;
                tickCount++;
            }
        } else
        {
            // a) input callbacks
            InputManager.Instance.DeterministicUpdate(FixedStep, tickCount);
        }

        // TODO:
        //SpriteManager.Instance.Render();
    }

    //private void Update()
    //{
    //    accumulatedTime += Time.deltaTime;
    //    while (accumulatedTime >= FixedStep)
    //    {
    //        accumulatedTime -= FixedStep;
    //
    //        // Implement Input Callbacks here
    //        InputManager.Instance.DeterministicUpdate(FixedStep, tickCount);
    //
    //        // Run deterministic game logic here
    //        RunDeterministicUpdate(FixedStep, tickCount);
    //
    //        if (PathfindingManager.Instance.enabled)
    //            PathfindingManager.Instance.DeterministicUpdate(FixedStep, tickCount);
    //
    //        // Step the deterministic timer
    //        timer.Update(FixedStep);
    //
    //        // Manually simulate physics for this step
    //        Physics.Simulate(FixedStep);
    //
    //        elapsedTime += Time.deltaTime;
    //        tickCount++;
    //    }
    //
    //    //if (PathfindingManager.Instance.enabled)
    //    //    PathfindingManager.Instance.DefaultUpdate();
    //}

    private void OnDestroy()
    {
        // Ensure cleanup when the GameObject is destroyed
        timer.ClearAllTimers();
    }

    private void RunDeterministicUpdate(float deltaTime, ulong tickID)
    {
        for (int i = 0; i < categorizedObjects.Length; i++)
        {
            var objList = categorizedObjects[i];
            if (objList != null)
            {
                for (int j = 0; j < objList.Count; j++)
                {
                    objList[j].DeterministicUpdate(deltaTime, tickCount);
                }
            }
        }
    }

    private void PostPhysicsUpdate(float deltaTime, ulong tickID)
    {
        for (int i = 0; i < categorizedPostPhysicsObjects.Length; i++)
        {
            var objList = categorizedPostPhysicsObjects[i];
            if (objList != null)
            {
                for (int j = 0; j < objList.Count; j++)
                {
                    objList[j].DeterministicUpdate(deltaTime, tickCount);
                }
            }
        }
    }

    public void Register<T>(T obj) where T : IDeterministicUpdate
    {
        int typeIndex = TypeIndex.GetIndex<T>();

        // Ensure list exists
        categorizedObjects[typeIndex] ??= new List<IDeterministicUpdate>();
        categorizedObjects[typeIndex].Add(obj);
    }

    public void RegisterPostPhysics<T>(T obj) where T : IDeterministicUpdate
    {
        int typeIndex = TypeIndex.GetIndex<T>();

        // Ensure list exists
        categorizedPostPhysicsObjects[typeIndex] ??= new List<IDeterministicUpdate>();
        categorizedPostPhysicsObjects[typeIndex].Add(obj);
    }

    public void Unregister<T>(T obj) where T : IDeterministicUpdate
    {
        int typeIndex = TypeIndex.GetIndex<T>();

        if (categorizedObjects[typeIndex] != null)
        {
            categorizedObjects[typeIndex].Remove(obj);
        }
    }

    public void UnregisterPostPhysics<T>(T obj) where T : IDeterministicUpdate
    {
        int typeIndex = TypeIndex.GetIndex<T>();

        if (categorizedPostPhysicsObjects[typeIndex] != null)
        {
            categorizedPostPhysicsObjects[typeIndex].Remove(obj);
        }
    }

    public bool IsPaused() => paused;

    public void Pause()
    {
        paused = true;
        NativeLogger.Log("Simulation Paused.", true);
    }

    public void Resume()
    {
        if (paused)
        {
            paused = false;
            NativeLogger.Log("Simulation Resumed.");
        }
    }
}