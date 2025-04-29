using System.Collections.Generic;
using System;
using UnityEngine;
using static UnityEditor.Progress;

public class PriorityQueue<T>
{
    private readonly List<(T item, int priority)> _heap = new();

    public int Count => _heap.Count;

    /// <summary>
    /// Add an item with a given priority.
    /// </summary>
    public void Enqueue(T item, int priority)
    {
        _heap.Add((item, priority));
        HeapifyUp(_heap.Count - 1);
    }

    /// <summary>
    /// Remove and return the highest priority item (min-priority by default).
    /// </summary>
    public T Dequeue()
    {
        if (_heap.Count == 0)
            throw new InvalidOperationException("The priority queue is empty.");

        T rootItem = _heap[0].item;
        _heap[0] = _heap[^1];
        _heap.RemoveAt(_heap.Count - 1);
        HeapifyDown(0);

        return rootItem;
    }

    /// <summary>
    /// Peek at the highest priority item without removing it.
    /// </summary>
    public T Peek()
    {
        if (_heap.Count == 0)
            throw new InvalidOperationException("The priority queue is empty.");

        return _heap[0].item;
    }

    /// <summary>
    /// Remove all items matching the given predicate, then rebuild the heap.
    /// </summary>
    public void RemoveAll(Predicate<T> match)
    {
        // Remove matching entries
        _heap.RemoveAll(entry => match(entry.item));
        // Rebuild heap property
        Heapify();
    }

    /// <summary>
    /// Internal: rebuild the heap in O(n).
    /// </summary>
    private void Heapify()
    {
        for (int i = _heap.Count / 2 - 1; i >= 0; i--)
            HeapifyDown(i);
    }

    private void HeapifyUp(int index)
    {
        while (index > 0)
        {
            int parent = (index - 1) / 2;
            if (_heap[index].priority >= _heap[parent].priority)
                break;

            Swap(index, parent);
            index = parent;
        }
    }

    private void HeapifyDown(int index)
    {
        int lastIndex = _heap.Count - 1;
        while (true)
        {
            int leftChild = index * 2 + 1;
            int rightChild = index * 2 + 2;
            int smallest = index;

            if (leftChild <= lastIndex && _heap[leftChild].priority < _heap[smallest].priority)
                smallest = leftChild;

            if (rightChild <= lastIndex && _heap[rightChild].priority < _heap[smallest].priority)
                smallest = rightChild;

            if (smallest == index)
                break;

            Swap(index, smallest);
            index = smallest;
        }
    }

    private void Swap(int i, int j)
    {
        (_heap[i], _heap[j]) = (_heap[j], _heap[i]);
    }
}

public class PathfindingManager : MonoBehaviour, IDeterministicUpdate
{
    public static PathfindingManager Instance;

    private int currentQueueCounter = 0;
    private int countToExecutePerFixedTick = 25;
    //private int countToExecutePerUpdate = 10;

    private PriorityQueue<PathfindingRequest> pathfindingQueue = new PriorityQueue<PathfindingRequest>();

    void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this);
        else
            Instance = this;
    }

    // Called once per rendered frame
    //public void DefaultUpdate()
    //{
    //    int defaultExecuted = 0;
    //
    //    // run up to per-Update allotment…
    //    // but never let (alreadyDone + thisFrame) exceed the fixed-tick budget
    //    while (defaultExecuted < countToExecutePerUpdate
    //           && currentQueueCounter + defaultExecuted < countToExecutePerFixedTick)
    //    {
    //        if (pathfindingQueue.Count == 0)
    //        {
    //            // nothing left at all
    //            enabled = false;
    //            break;
    //        }
    //
    //        var req = pathfindingQueue.Dequeue();
    //        req.PathfindingAction?.Invoke();
    //        defaultExecuted++;
    //    }
    //
    //    // commit how many we actually ran this Update
    //    currentQueueCounter += defaultExecuted;
    //}

    // Called in lock-step on every client
    public void DeterministicUpdate(float deltaTime, ulong tickID)
    {
        int fixedExecuted = 0;

        // finish out the remainder up to the fixed-tick budget
        while (fixedExecuted + currentQueueCounter < countToExecutePerFixedTick)
        {
            if (pathfindingQueue.Count == 0)
            {
                enabled = false;
                break;
            }

            var req = pathfindingQueue.Dequeue();
            req.PathfindingAction?.Invoke();
            fixedExecuted++;
        }

        // reset for next simulation tick
        currentQueueCounter = 0;
    }

    //public void DefaultUpdate()
    //{
    //    int executed = currentQueueCounter;
    //    while (executed < countToExecutePerUpdate)
    //    {
    //        if (currentQueueCounter >= countToExecutePerFixedTick) break;
    //        if (pathfindingQueue.Count == 0)
    //        {
    //            enabled = false;
    //            currentQueueCounter = 0;
    //            break;
    //        }
    //
    //        var request = pathfindingQueue.Dequeue();
    //        request?.PathfindingAction?.Invoke();
    //        executed++;
    //    }
    //    currentQueueCounter = executed;
    //}
    //
    //public void DeterministicUpdate(float deltaTime, ulong tickID)
    //{
    //    int executed = currentQueueCounter;
    //    while (executed < countToExecutePerFixedTick)
    //    {
    //        if (pathfindingQueue.Count == 0)
    //        {
    //            enabled = false;
    //            currentQueueCounter = 0;
    //            break;
    //        }
    //
    //        var request = pathfindingQueue.Dequeue();
    //        request?.PathfindingAction?.Invoke();
    //        executed++;
    //    }
    //    currentQueueCounter = 0;
    //}

    public void RequestPathfinding(Component sender, Action pathfindingAction, int priority)
    {
        if (pathfindingAction == null)
            throw new ArgumentNullException(nameof(pathfindingAction));

        CancelRequest(sender);

        pathfindingQueue.Enqueue(new PathfindingRequest
        {
            Sender = sender,
            PathfindingAction = pathfindingAction,
            Priority = priority
        }, priority);

        enabled = true;
    }

    public void CancelRequest(Component sender)
    {
        if (sender == null)
            return;

        pathfindingQueue.RemoveAll(req => req.Sender == sender);
    }
}

public class PathfindingRequest
{
    public Component Sender;
    public Action PathfindingAction;
    public int Priority;
}