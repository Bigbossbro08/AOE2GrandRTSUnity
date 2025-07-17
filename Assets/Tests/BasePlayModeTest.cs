using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class BasePlayModeTest
{
    protected List<System.Action> _cleanUps;

    [SetUp]
    public void CommonSetup()
    {
        _cleanUps = new List<System.Action>();
    }

    [TearDown]
    public void CommonTeardown()
    {
        // run in reverse just in case
        for (int i = _cleanUps.Count - 1; i >= 0; i--)
            _cleanUps[i]?.Invoke();
    }

    protected static IEnumerator LoadGameScene()
    {
        // Load your scene by name (must be in build settings!)
        SceneManager.LoadScene("Scenes/SampleScene");

        bool sceneLoaded = false;
        void OnSceneLoaded(Scene s, LoadSceneMode m) => sceneLoaded = true;
        SceneManager.sceneLoaded += OnSceneLoaded;

        while (!sceneLoaded)
        {
            // Wait until scene is loaded
            yield return null;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded; // clean up

        while (!GameManager.Instance.IsLoaded())
        {
            yield return null;
        }
    }

    protected MovableUnit SpawnUnit(Vector3 pos, Vector3 euler,
                                    System.Action<MovableUnit> init = null)
    {
        var unit = UnitManager.Instance.GetMovableUnitFromPool();
        Assert.IsNotNull(unit, "Failed to get unit from pool");
        unit.transform.position = pos;
        unit.transform.eulerAngles = euler;
        init?.Invoke(unit);
        _cleanUps.Add(() => UnitManager.Instance.ReleaseMovableUnitFromPool(unit));
        return unit;
    }

    protected MissionCollisionTriggerChecker CreateTriggerBox(Vector3 position, Quaternion rotation, Vector3 scale,
        Vector3 size, Vector3 center,
        System.Func<bool> CheckerCallback = null,
        System.Action<Collider> OnTriggerEnterCallback = null,
        System.Action<Collider> OnTriggerExitCallback = null)
    {
        var trigger = MissionCollisionTriggerChecker.SpawnBox(position, rotation, scale, size, center,
            CheckerCallback, OnTriggerEnterCallback, OnTriggerExitCallback);
        _cleanUps.Add(() => DestroyIfExistsAlongWithGameobject(trigger));
        return trigger;
    }

    protected List<MovableUnit> SpawnUnitsAt(IEnumerable<Vector3> positions,
                                        Action<Unit> init = null)
    {
        var list = new List<MovableUnit>();
        foreach (var pos in positions)
        {
            var u = UnitManager.Instance.GetMovableUnitFromPool(init);
            Assert.IsNotNull(u, "Pool ran dry!");
            u.transform.position = pos;
            u.transform.eulerAngles = Vector3.zero;
            _cleanUps.Add(() => UnitManager.Instance.ReleaseMovableUnitFromPool(u));
            list.Add(u);
        }
        return list;
    }

    protected IEnumerator WaitUntilCount(int expected, Func<int> getCount,
                                     float timeout = 30f)
    {
        float t = 0f;
        while (getCount() != expected && t < timeout)
        {
            t += Time.deltaTime;
            yield return null;
        }
        Assert.AreEqual(expected, getCount(),
            $"Expected {expected} hits but got {getCount()}");
    }

    static protected void DestroyIfExistsAlongWithGameobject(Component component)
    {
        if (component != null)
        {
            DestroyIfExists(component.gameObject);
        }
    }
    
    static void DestroyIfExists(GameObject go)
    {
        if (go != null)
            GameObject.Destroy(go);
    }

    protected IEnumerator WaitForCondition(System.Func<bool> check, float timeout = 30f)
    {
        float t = 0;
        while (!check() && t < timeout)
        {
            t += Time.deltaTime;
            yield return null;
        }
        Assert.IsTrue(check(), "Condition never met within timeout");
    }
}
