using ENet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using static DeterministicUpdateManager.NewDeterministicInputManager;
using static InputManager;

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
    [System.Serializable]
    public class NewDeterministicInputManager
    {
        [System.Serializable]
        public class CommandLog
        {
            public List<InputCommand> commands = new List<InputCommand>();
            public ulong nextSafeFrame = 0;
        }

        [SerializeField] private CommandLog log = new CommandLog();
        [SerializeField] private int commandIndex = 0;

        public InputCommand ParseCommand(JObject obj)
        {
            string action = obj["action"]?.ToString();
            InputCommand parsedCommand = obj.ToObject<InputCommand>();
            if (log.nextSafeFrame < parsedCommand.frame)
            {
                log.nextSafeFrame = parsedCommand.frame;
            }
            return action switch
            {
                MoveUnitsCommand.commandName => obj.ToObject<MoveUnitsCommand>(),
                _ => obj.ToObject<InputCommand>()
            };
        }

        public void LoadFromFile(string path)
        {
            string json = System.IO.File.ReadAllText(path);
            //log = JsonConvert.DeserializeObject<CommandLog>(json);

            JObject root = JObject.Parse(json);
            log = new CommandLog();
            foreach (var token in root["commands"])
            {
                var obj = (JObject)token;
                log.commands.Add(ParseCommand(obj));
            }
            commandIndex = 0;
        }

        public void SaveToFile(string path)
        {
            string jsonString = JsonConvert.SerializeObject(log, Formatting.Indented);
            System.IO.File.WriteAllText(path, jsonString);
        }

        public void LoadOrSave(string path)
        {
            if (System.IO.File.Exists(path))
            {
                LoadFromFile(path);
                System.IO.File.Delete(path);
            }
            else
            {
                SaveToFile(path);
            }
        }

        public void Update(float deltaTime, ulong tickId)
        {
            while (commandIndex < log.commands.Count && log.commands[commandIndex].frame == tickId)
            {
                var cmd = log.commands[commandIndex];
                InputManager.Instance.ExecuteCommand(cmd);
                commandIndex++;
            }
            if (log.nextSafeFrame < tickId)
            {
                log.nextSafeFrame = tickId;
            }
        }

        internal void SendInputCommand(InputCommand command)
        {
            ulong tickFrame = log.nextSafeFrame + 1;
            command.frame = tickFrame;
            log.commands.Add(command);
        }
    }

    [System.Serializable]
    public class ENetMultiplayerInputManager
    {
        public enum PacketType
        {
            Connected = 0,
            Disconnected,
            Tick,
            ClientInput,   // Client -> Server
            ServerInputBundle// Server -> Clients
        }

        private Host client;
        private Thread clientThread;
        private Peer peer;

        bool HasSentRequest = false;
        bool clientRunning = false;

        public ulong serverTick { get; private set; }
        public ulong safeTick { get; private set; }

        public List<InputCommand> commands = new List<InputCommand>();

        public ENetMultiplayerInputManager() {
            ENet.Library.Initialize();
            StartClient();
        }

        void StartClient()
        {
            client = new Host();
            Address address = new Address();
            address.SetHost("127.0.0.1"); // TODO: add proper host
            address.Port = 1234; // TODO: add proper port

            client.Create();

            // Connect to the server
            peer = client.Connect(address);

            // Start a server thread to handle client connections and messages
            clientThread = new Thread(ClientLoop);
            clientThread.Start();
            clientRunning = true;
        }

        private void ClientLoop()
        {
            while (clientRunning)
            {
                bool polled = false;

                // Poll for events like client connect, disconnect, or messages
                ENet.Event netEvent;
                while (!polled)
                {
                    if (client.CheckEvents(out netEvent) <= 0)
                    {
                        if (client.Service(15, out netEvent) <= 0)
                            break;

                        polled = true;
                    }
                    switch (netEvent.Type)
                    {
                        case ENet.EventType.None:
                            break;
                        case ENet.EventType.Connect:
                            NativeLogger.Log("Client connected: " + netEvent.Peer.IP);
                            //connected = true;
                            break;
                        case ENet.EventType.Receive:
                            //NativeLogger.Log("Received message from client: " + netEvent.Packet.ToString());

                            HandleRecievingPacket(netEvent.Packet, netEvent.Peer);

                            netEvent.Packet.Dispose();
                            break;
                        case ENet.EventType.Disconnect:
                            NativeLogger.Log("Client disconnected: " + netEvent.Peer.IP);
                            //connected = false;
                            break;
                    }
                }

                // Sleep for a bit to avoid maxing out the CPU
                Thread.Sleep(10);
            }
        }

        private void HandleRecievingPacket(Packet packet, Peer peer)
        {
            byte[] buffer = new byte[4096];

            packet.CopyTo(buffer);

            using (var ms = new MemoryStream(buffer))
            using (var reader = new BinaryReader(ms))
            {
                PacketType packetType = (PacketType)reader.ReadByte();
                switch (packetType)
                {
                    case PacketType.Tick:
                        {
                            ulong tick = reader.ReadUInt64();
                            serverTick = tick;
                        }
                        break;
                    case PacketType.ServerInputBundle:
                        {
                            ulong fromTick = reader.ReadUInt64();
                            ulong toTick = reader.ReadUInt64();
                            int inputCount = reader.ReadInt32(); 
                            
                            var settings = new JsonSerializerSettings
                            {
                                Converters = { new InputCommandConverter() }
                            };
                            for (int i = 0; i < inputCount; i++)
                            {
                                ulong tick = reader.ReadUInt64();
                                string cmdString = reader.ReadString();
                                var inputCommand = JsonConvert.DeserializeObject<InputCommand>(cmdString, settings);

                                inputCommand.frame = tick;
                                commands.Add(inputCommand);
                                NativeLogger.Log($"Tick ID: {tick} and current: {Instance.tickCount}, Command: {cmdString}, type: {inputCommand.GetType().Name}");
                            }
                            NativeLogger.Log($"Recieved input range request from tick: {fromTick}, To tick: {toTick}");
                            safeTick = toTick;
                            HasSentRequest = false;
                        }
                        break;
                }
            }
        }

        bool IsConnected()
        {
            return peer.State == PeerState.Connected;
        }

        public void StopClient()
        {
            peer.DisconnectNow(0);
            clientRunning = false;
            if (clientThread != null && clientThread.IsAlive)
            {
                clientThread.Join();
            }
            //clientThread?.Abort();
            client.Flush();
            Library.Deinitialize();
        }

        ~ENetMultiplayerInputManager()
        {
            StopClient();
        }

        public void RequestCommandInRange(ulong from, ulong to)
        {
            if (HasSentRequest)
            {
                return;
            }
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                Packet packet = default(Packet);
                writer.Write((byte)PacketType.ServerInputBundle);
                writer.Write(from);
                writer.Write(to);
                byte[] data = stream.ToArray();
                packet.Create(data, PacketFlags.Reliable);
                peer.Send(0, ref packet);
                HasSentRequest = true;
            }
        }

        public void SentCommandInput(string command)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                Packet packet = default(Packet);
                writer.Write((byte)PacketType.ClientInput);
                string cmdString = command;
                writer.Write(cmdString);
                byte[] data = stream.ToArray();
                packet.Create(data, PacketFlags.Reliable);
                peer.Send(0, ref packet);
            }
        }

        int commandIndex = 0;

        internal void Update(float fixedStep, ulong tickCount)
        {
            while (commandIndex < commands.Count && commands[commandIndex].frame == tickCount)
            {
                var cmd = commands[commandIndex];
                InputManager.Instance.ExecuteCommand(cmd);
                commandIndex++;
            }
        }
    };

    public class GameServerInstance
    {
        public enum Status
        {
            Ready,
            Stalling,
            Paused
        }

        public Status status = Status.Ready;
    }

    public static DeterministicUpdateManager Instance { get; private set; }

    public ulong tickCount = 0;
    public float elapsedTime = 0.0f;

    public int seed = 42;

    private float accumulatedTime = 0f;
    public const float FixedStep = 1/ 25f; // 60 Hz
    public DeterministicTimer timer = new DeterministicTimer();

    public DeterministicCoroutineManager CoroutineManager { get; private set; }
    public GameServerInstance gameServerInstance { get; private set; }
    public NewDeterministicInputManager newDeterministicInputManager { get; private set; }
    public ENetMultiplayerInputManager enetMultiplayerInputManager { get; private set; }
    // **Use a list array instead of a dictionary**
    private readonly List<IDeterministicUpdate>[] categorizedObjects = new List<IDeterministicUpdate>[256];

    // **Use a list array instead of a dictionary**
    private readonly List<IDeterministicUpdate>[] categorizedPostPhysicsObjects = new List<IDeterministicUpdate>[256];

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        //DontDestroyOnLoad(gameObject);

        UnityEngine.Random.InitState(seed);

        enabled = false;
        Time.fixedDeltaTime = FixedStep;
        Physics.simulationMode = SimulationMode.Script;
        CoroutineManager = new DeterministicCoroutineManager(); ;
        gameServerInstance = new GameServerInstance();

        // Singleplayer
        //newDeterministicInputManager = new NewDeterministicInputManager();
        //CanTick = () =>
        //{
        //    return gameServerInstance.status == GameServerInstance.Status.Ready;
        //};
        //
        //string jsonPath = Path.Combine(MapLoader.GetDataPath(), "saves\\simulation.json");
        //newDeterministicInputManager.LoadFromFile(jsonPath);

        // Multiplayer
        enetMultiplayerInputManager = new ENetMultiplayerInputManager();

        CanTick = () =>
        {
            if (!GameManager.Instance.IsLoaded())
            {
                return false;
            }

            ulong clientTick = tickCount;
            if (clientTick >= enetMultiplayerInputManager.safeTick)
            {
                ulong requestFrom = clientTick + 1;
                ulong rangeOfTicks = 5;
                ulong requestTo = (ulong)Mathf.Min(requestFrom + rangeOfTicks, enetMultiplayerInputManager.serverTick);
                NativeLogger.Log($"Sending command request from {requestFrom} to {requestTo}");
                enetMultiplayerInputManager.RequestCommandInRange(requestFrom, requestTo);
                return false;
            }

            return true;
        };

        GetAccumulatorValue = () =>
        {
            ulong clientTick = tickCount;
            if (clientTick >= enetMultiplayerInputManager.safeTick)
            {
                return 0.0f;
            }

            ulong tickOffset = enetMultiplayerInputManager.safeTick - clientTick;
            if (tickOffset == 0)
            {
                return 0.0f;
            }

            float valueToAccumulate = Time.deltaTime * Mathf.Min(8, tickOffset);

            return valueToAccumulate;
        };
    }

    public void SendInputCommand(InputCommand command)
    {
        string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(command);
        enetMultiplayerInputManager.SentCommandInput(jsonString);
    }

    public System.Func<bool> CanTick = () => { return true; };
    public System.Func<float> GetAccumulatorValue = () => { return Time.deltaTime; };

    private void Update()
    {
        InputManager.Instance.NetworkTick();

        if (Input.GetKeyDown(KeyCode.Home))
        {
            if (gameServerInstance.status == GameServerInstance.Status.Ready)
            {
                gameServerInstance.status = GameServerInstance.Status.Stalling;
            }
            else if (gameServerInstance.status == GameServerInstance.Status.Stalling)
            {
                gameServerInstance.status = GameServerInstance.Status.Ready;
            }
        }

        if (!CanTick())
        {
            return;
        }

        accumulatedTime += GetAccumulatorValue();// Time.deltaTime;
        while (accumulatedTime >= FixedStep)
        {
            accumulatedTime -= FixedStep;

            // a) input callbacks
            // InputManager.Instance.DeterministicUpdate(FixedStep, tickCount);
            // newDeterministicInputManager.Update(FixedStep, tickCount);
            enetMultiplayerInputManager.Update(FixedStep, tickCount);

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
        enetMultiplayerInputManager?.StopClient();
        // Ensure cleanup when the GameObject is destroyed
        timer.ClearAllTimers();
        string jsonPath = Path.Combine(MapLoader.GetDataPath(), "saves\\simulation.json");
        //newDeterministicInputManager.SaveToFile(jsonPath);
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

    public void Pause()
    {
        NativeLogger.Log("Simulation Paused.", true);
    }

    public void Resume()
    {
        NativeLogger.Log("Simulation Resumed.");
    }
}