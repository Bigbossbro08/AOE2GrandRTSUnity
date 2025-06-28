using System;
using System.Collections.Generic;
using UnityEngine;

public interface IDeterministicYieldInstruction
{
    bool Tick(); // Returns true when done
}
public class WaitUntil : IDeterministicYieldInstruction
{
    private readonly Func<bool> condition;

    public WaitUntil(Func<bool> condition)
    {
        this.condition = condition;
    }

    public bool Tick()
    {
        return condition(); // MUST be deterministic across all clients
    }
}

public class DeterministicWaitTicks : IDeterministicYieldInstruction
{
    private ulong remaining;

    public DeterministicWaitTicks(ulong ticks)
    {
        remaining = ticks;
    }

    public bool Tick()
    {
        if (remaining > 0)
        {
            remaining--;
        }
        return remaining == 0;
    }
}

public class DeterministicWaitForSeconds : IDeterministicYieldInstruction
{
    private float remaining;

    public DeterministicWaitForSeconds(float time)
    {
        remaining = time;
    }

    public bool Tick()
    {
        if (remaining > 0)
        {
            remaining -= DeterministicUpdateManager.FixedStep;
        }
        return remaining <= 0;
    }
}

public class DeterministicCoroutine
{
    private IEnumerator<IDeterministicYieldInstruction> routine;
    private IDeterministicYieldInstruction currentYield;

    public bool IsComplete { get; private set; }

    public DeterministicCoroutine(IEnumerator<IDeterministicYieldInstruction> routine)
    {
        this.routine = routine;
        Advance(); // Prime the coroutine
    }

    private void Advance()
    {
        if (!routine.MoveNext())
        {
            IsComplete = true;
            return;
        }

        currentYield = routine.Current;
    }

    public void Tick()
    {
        if (IsComplete) return;

        if (currentYield == null || currentYield.Tick())
        {
            Advance();
        }
    }
}

public class DeterministicCoroutineManager
{
    private readonly List<DeterministicCoroutine> coroutines = new();

    public void StartCoroutine(IEnumerator<IDeterministicYieldInstruction> coroutine)
    {
        coroutines.Add(new DeterministicCoroutine(coroutine));
    }

    public void Tick()
    {
        for (int i = coroutines.Count - 1; i >= 0; i--)
        {
            var co = coroutines[i];
            co.Tick();
            if (co.IsComplete)
            {
                coroutines.RemoveAt(i);
            }
        }
    }
}