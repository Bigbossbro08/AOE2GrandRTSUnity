using System.Collections.Generic;
using UnityEngine;

public class TestCoroutine : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DeterministicUpdateManager.Instance.CoroutineManager.StartCoroutine(TestRoutine());
    }

    IEnumerator<IDeterministicYieldInstruction> TestRoutine()
    {
        Debug.Log($"Start logic at tick {DeterministicUpdateManager.Instance.tickCount}");
        yield return new DeterministicWaitForSeconds(5);
        Debug.Log($"Waited 5 seconds, tick is now {DeterministicUpdateManager.Instance.tickCount}");
        yield return new DeterministicWaitForSeconds(10);
        Debug.Log($"Waited 10 more ticks, tick is now {DeterministicUpdateManager.Instance.tickCount}");
    }
}
