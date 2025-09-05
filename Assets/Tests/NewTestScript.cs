using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using TMPro;
using System;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;

public class NewTestScript
{
    IEnumerator LoadGameScene()
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

    // A Test behaves as an ordinary method
    //[Test]
    //public void NewTestScriptSimplePasses()
    //{
    //    // Use the Assert class to test conditions
    //}

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.

    [UnityTest]
    public IEnumerator TestRegisteredUnits()
    {
        yield return LoadGameScene();

        System.Action CleanUp = () => {};

        List<string> registeredUnitNames = new List<string>() {
            "ship_units\\TestShip",
            "military_units\\Cannon",
            "military_units\\Rodelero",
            "military_units\\hand_cannon"
        };

        List <MovableUnit> units = new List<MovableUnit>();

        Vector3 newCopiedPosition_0 = new Vector3(124.428f, 0f, 95.833f);
        Vector3 newCopiedEulerAngles_0 = new Vector3(0f, 0f, 0f);

        {
            List<MovableUnit> copyCommandUnits = new();

            MovableUnit movableUnit_0 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_0 != null)
            {
                movableUnit_0.transform.position = newCopiedPosition_0;
                movableUnit_0.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_0.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_0);
                };
                copyCommandUnits.Add(movableUnit_0);
            }

            units.AddRange(copyCommandUnits);
        }

        void MoveNewUnit(ulong id)
        {
            Vector3 hitPoint = new Vector3(114.44f, 0.0341f, 98.06f);
            MoveUnitsCommand moveUnitsCommand = new MoveUnitsCommand();
            moveUnitsCommand.action = MoveUnitsCommand.commandName;
            moveUnitsCommand.unitIDs = new List<ulong>();
            moveUnitsCommand.unitIDs.Add(id);
            moveUnitsCommand.position = (CommonStructures.SerializableVector3)hitPoint;
            moveUnitsCommand.IsAttackMove = false;
            InputManager.Instance.SendInputCommand(moveUnitsCommand);
        }

        var objectiveText = AddObjective("Time passed for testing: 0");
        CleanUp += () => DestroyIfExists(objectiveText?.gameObject);
        var objectiveText2 = AddObjective("Units tested: 0");
        CleanUp += () => DestroyIfExistsAlongWithGameobject(objectiveText2);

        float timer = 0;
        int counter = 0;

        void TestNewUnit()
        {
            if (units.Count == 0 || units.Count > 1)
            {
                Assert.Fail();
            }

            UnitManager.Instance.ReleaseMovableUnitFromPool(units[0]);

            System.Action<Unit> SetSpawnData = (unit) =>
            {
                unit.unitDataName = registeredUnitNames[counter];
                unit.playerId = 1;
            };
            MovableUnit movableUnit = UnitManager.Instance.GetMovableUnitFromPool(SetSpawnData);

            movableUnit.transform.position = newCopiedPosition_0;
            movableUnit.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_0.y, 0);

            CleanUp += () =>
            {
                UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit);
            };
            units.Clear();
            units.Add(movableUnit);
            MoveNewUnit(movableUnit.id);
            counter++;
        }

        int hitBoxCounter = 0;
        bool promptUnitForNewTest = false;
        System.Func<bool> Checker = () =>
        {
            if (promptUnitForNewTest && counter < registeredUnitNames.Count)
            {
                TestNewUnit();
                promptUnitForNewTest= false;
            }

            timer += Time.deltaTime;
            objectiveText.text = $"Time passed for testing: {(int)timer}";
            objectiveText2.text = $"Units tested: {hitBoxCounter}";

            if (hitBoxCounter == registeredUnitNames.Count)
            {
                return true;
            }
            return false;
        };
        
        // === Spawn Mission Area Trigger ===
        var firstChecker = MissionCollisionTriggerChecker.SpawnBox(new Vector3(114.44f, 0.341f, 98.06f),
            Quaternion.Euler(0f, 0f, 0f),
            new Vector3(1f, 1f, 1f),
            new Vector3(1f, 1f, 1f),
            new Vector3(0f, 0f, 0f));

        CleanUp += () =>
        {
            DestroyIfExistsAlongWithGameobject(firstChecker);
        };

        firstChecker.OnTriggerEnterCallback = (collider) =>
        {
            if (collider.CompareTag("Military Unit"))
            {
                promptUnitForNewTest = true;
                hitBoxCounter++;
                //TestNewUnit();
            }
        };
        firstChecker.OnTriggerExitCallback = (collider) =>
        {
            //if (collider.CompareTag("Military Unit"))
            //    militaryUnitCounter--;
        };

        TestNewUnit();

        while (!Checker())
        {
            yield return null;
        }

        objectiveText2.color = Color.green;
        objectiveText2.fontStyle = TMPro.FontStyles.Strikethrough | TMPro.FontStyles.Bold;

        objectiveText.text = "Test looks passed. Should exit in 5 seconds";
        yield return new WaitForSeconds(5);

        CleanUp?.Invoke();
    }

    [UnityTest]
    public IEnumerator TestSingleUnitMovement()
    {
        yield return LoadGameScene();

        System.Action CleanUp = () => { };

        List<MovableUnit> copyCommandUnits = new(); 

        Vector3 newCopiedPosition_0 = new Vector3(102.14f, 2.742795f, 98.78004f);
        Vector3 newCopiedEulerAngles_0 = new Vector3(0f, 0f, 0f);

        MovableUnit movableUnit_0 = UnitManager.Instance.GetMovableUnitFromPool();
        if (movableUnit_0 != null)
        {
            movableUnit_0.transform.position = newCopiedPosition_0;
            movableUnit_0.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_0.y, 0);

            CleanUp += () =>
            {
                UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_0);
            };
            copyCommandUnits.Add(movableUnit_0);
        }

        List<ulong> ids = new List<ulong>();
        foreach (var unit in copyCommandUnits)
        {
            ids.Add(unit.id);
        }

        // === Spawn Mission Area Trigger ===
        var firstChecker = MissionCollisionTriggerChecker.SpawnBox(new Vector3(114.44f, 0.341f, 98.06f),
            Quaternion.Euler(0f, 0f, 0f),
            new Vector3(1f, 1f, 1f),
            new Vector3(1f, 1f, 1f),
            new Vector3(0f, 0f, 0f));

        CleanUp += () =>
        {
            DestroyIfExistsAlongWithGameobject(firstChecker);
        };

        // === Set Trigger Logic ===
        int militaryUnitCounter = 0;

        firstChecker.OnTriggerEnterCallback = (collider) =>
        {
            if (collider.CompareTag("Military Unit"))
                militaryUnitCounter++;
        };
        firstChecker.OnTriggerExitCallback = (collider) =>
        {
            if (collider.CompareTag("Military Unit"))
                militaryUnitCounter--;
        };

        bool attackMove = false;
        Vector3 hitPoint = new Vector3(99.53003f, 2.742795f, 98.78004f);
        MoveUnitsCommand moveUnitsCommand = new MoveUnitsCommand();
        moveUnitsCommand.action = MoveUnitsCommand.commandName;
        moveUnitsCommand.unitIDs = new List<ulong>();
        moveUnitsCommand.unitIDs.AddRange(ids);
        moveUnitsCommand.position = (CommonStructures.SerializableVector3)hitPoint;
        moveUnitsCommand.IsAttackMove = attackMove;
        InputManager.Instance.SendInputCommand(moveUnitsCommand);

        int unitCount = ids.Count;
        //bool success = false;
        float timer = 0f;

        var objectiveText = AddObjective("Time left for testing: 0");
        CleanUp += () => DestroyIfExists(objectiveText?.gameObject);
        var objectiveText2 = AddObjective("Military Units in area: 0");
        CleanUp += () => DestroyIfExistsAlongWithGameobject(objectiveText2);

        firstChecker.CheckerCallback = () =>
        {
            List<Vector3> pathPositions = movableUnit_0.movementComponent.GetPathPositions();
            if (pathPositions.Count >  0)
                Debug.DrawLine(movableUnit_0.transform.position, pathPositions[0], Color.red);

            RaycastHit? hit = SelectionController.FindProperHit(movableUnit_0.transform.position, 1);
            if (hit.HasValue)
            {
                DebugExtension.DebugWireSphere(hit.Value.point, Color.blue, 0.1f);
            }

            if (timer < 20)
            {
                timer += Time.deltaTime;
                if (movableUnit_0)
                {
                    GameManager.Instance.CameraHandler.transform.position = movableUnit_0.transform.position;
                }
                objectiveText.text = $"Time passed for testing: {(int)timer}";
            }
            else
            {
                return true;
            }

            if (militaryUnitCounter == unitCount)
            {
                //success = true;
                return true;
            }
            objectiveText2.text = $"Military Units in area: {militaryUnitCounter} and expected count is: {unitCount}";
            return false;
        };

        // === Wait for Condition ===
        while (!firstChecker.CheckerCallback())
        {
            yield return null;
        }

        timer = 0;
        while (timer < 5)
        {
            timer += Time.deltaTime;
            objectiveText.text = $"Time passed to exit testing: {(int)timer}";
            yield return null;
        }

        CleanUp?.Invoke();
        Assert.IsTrue(movableUnit_0.movementComponent.movementState == MovementComponent.State.Idle && !movableUnit_0.movementComponent.HasPathPositions());
    }

    [UnityTest]
    public IEnumerator TestMovementInGroup()
    {
        yield return LoadGameScene();

        System.Action CleanUp = () => { };

        List<MovableUnit> copyCommandUnits = new();
        {
            Vector3 newCopiedPosition_0 = new Vector3(126.99f, 0f, 95.72f);
            Vector3 newCopiedEulerAngles_0 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_0 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_0 != null)
            {
                movableUnit_0.transform.position = newCopiedPosition_0;
                movableUnit_0.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_0.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_0);
                };
                copyCommandUnits.Add(movableUnit_0);
            }


            Vector3 newCopiedPosition_1 = new Vector3(126.99f, 0f, 95.187f);
            Vector3 newCopiedEulerAngles_1 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_1 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_1 != null)
            {
                movableUnit_1.transform.position = newCopiedPosition_1;
                movableUnit_1.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_1.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_1);
                };
                copyCommandUnits.Add(movableUnit_1);
            }


            Vector3 newCopiedPosition_2 = new Vector3(126.99f, 0f, 96.26f);
            Vector3 newCopiedEulerAngles_2 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_2 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_2 != null)
            {
                movableUnit_2.transform.position = newCopiedPosition_2;
                movableUnit_2.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_2.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_2);
                };
                copyCommandUnits.Add(movableUnit_2);
            }


            Vector3 newCopiedPosition_3 = new Vector3(126.99f, 0f, 98.49699f);
            Vector3 newCopiedEulerAngles_3 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_3 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_3 != null)
            {
                movableUnit_3.transform.position = newCopiedPosition_3;
                movableUnit_3.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_3.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_3);
                };
                copyCommandUnits.Add(movableUnit_3);
            }


            Vector3 newCopiedPosition_4 = new Vector3(127.45f, 0f, 97.517f);
            Vector3 newCopiedEulerAngles_4 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_4 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_4 != null)
            {
                movableUnit_4.transform.position = newCopiedPosition_4;
                movableUnit_4.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_4.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_4);
                };
                copyCommandUnits.Add(movableUnit_4);
            }


            Vector3 newCopiedPosition_5 = new Vector3(127.45f, 0f, 96.26f);
            Vector3 newCopiedEulerAngles_5 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_5 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_5 != null)
            {
                movableUnit_5.transform.position = newCopiedPosition_5;
                movableUnit_5.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_5.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_5);
                };
                copyCommandUnits.Add(movableUnit_5);
            }


            Vector3 newCopiedPosition_6 = new Vector3(127.45f, 0f, 96.7f);
            Vector3 newCopiedEulerAngles_6 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_6 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_6 != null)
            {
                movableUnit_6.transform.position = newCopiedPosition_6;
                movableUnit_6.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_6.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_6);
                };
                copyCommandUnits.Add(movableUnit_6);
            }


            Vector3 newCopiedPosition_7 = new Vector3(126.99f, 0f, 96.7f);
            Vector3 newCopiedEulerAngles_7 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_7 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_7 != null)
            {
                movableUnit_7.transform.position = newCopiedPosition_7;
                movableUnit_7.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_7.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_7);
                };
                copyCommandUnits.Add(movableUnit_7);
            }


            Vector3 newCopiedPosition_8 = new Vector3(127.45f, 0f, 98.057f);
            Vector3 newCopiedEulerAngles_8 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_8 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_8 != null)
            {
                movableUnit_8.transform.position = newCopiedPosition_8;
                movableUnit_8.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_8.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_8);
                };
                copyCommandUnits.Add(movableUnit_8);
            }


            Vector3 newCopiedPosition_9 = new Vector3(126.99f, 0f, 94.74701f);
            Vector3 newCopiedEulerAngles_9 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_9 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_9 != null)
            {
                movableUnit_9.transform.position = newCopiedPosition_9;
                movableUnit_9.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_9.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_9);
                };
                copyCommandUnits.Add(movableUnit_9);
            }


            Vector3 newCopiedPosition_10 = new Vector3(127.45f, 0f, 98.49699f);
            Vector3 newCopiedEulerAngles_10 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_10 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_10 != null)
            {
                movableUnit_10.transform.position = newCopiedPosition_10;
                movableUnit_10.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_10.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_10);
                };
                copyCommandUnits.Add(movableUnit_10);
            }


            Vector3 newCopiedPosition_11 = new Vector3(127.45f, 0f, 94.74701f);
            Vector3 newCopiedEulerAngles_11 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_11 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_11 != null)
            {
                movableUnit_11.transform.position = newCopiedPosition_11;
                movableUnit_11.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_11.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_11);
                };
                copyCommandUnits.Add(movableUnit_11);
            }


            Vector3 newCopiedPosition_12 = new Vector3(126.99f, 0f, 94.20701f);
            Vector3 newCopiedEulerAngles_12 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_12 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_12 != null)
            {
                movableUnit_12.transform.position = newCopiedPosition_12;
                movableUnit_12.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_12.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_12);
                };
                copyCommandUnits.Add(movableUnit_12);
            }


            Vector3 newCopiedPosition_13 = new Vector3(127.45f, 0f, 95.72f);
            Vector3 newCopiedEulerAngles_13 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_13 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_13 != null)
            {
                movableUnit_13.transform.position = newCopiedPosition_13;
                movableUnit_13.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_13.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_13);
                };
                copyCommandUnits.Add(movableUnit_13);
            }


            Vector3 newCopiedPosition_14 = new Vector3(126.99f, 0f, 98.057f);
            Vector3 newCopiedEulerAngles_14 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_14 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_14 != null)
            {
                movableUnit_14.transform.position = newCopiedPosition_14;
                movableUnit_14.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_14.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_14);
                };
                copyCommandUnits.Add(movableUnit_14);
            }


            Vector3 newCopiedPosition_15 = new Vector3(126.99f, 0f, 97.517f);
            Vector3 newCopiedEulerAngles_15 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_15 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_15 != null)
            {
                movableUnit_15.transform.position = newCopiedPosition_15;
                movableUnit_15.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_15.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_15);
                };
                copyCommandUnits.Add(movableUnit_15);
            }


            Vector3 newCopiedPosition_16 = new Vector3(127.45f, 0f, 95.187f);
            Vector3 newCopiedEulerAngles_16 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_16 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_16 != null)
            {
                movableUnit_16.transform.position = newCopiedPosition_16;
                movableUnit_16.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_16.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_16);
                };
                copyCommandUnits.Add(movableUnit_16);
            }


            Vector3 newCopiedPosition_17 = new Vector3(127.45f, 0f, 94.20701f);
            Vector3 newCopiedEulerAngles_17 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_17 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_17 != null)
            {
                movableUnit_17.transform.position = newCopiedPosition_17;
                movableUnit_17.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_17.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_17);
                };
                copyCommandUnits.Add(movableUnit_17);
            }
        }

        bool attackMove = false;
        List<ulong> ids = new List<ulong>();
        foreach (var unit in copyCommandUnits)
        {
            ids.Add(unit.id);
        }

        // === Spawn Mission Area Trigger ===
        var firstChecker = MissionCollisionTriggerChecker.SpawnBox(new Vector3(114.44f, 0.341f, 98.06f),
            Quaternion.Euler(0f, 0f, 0f),
            new Vector3(2f, 2f, 2f),
            new Vector3(1f, 1f, 1f),
            new Vector3(0f, 0f, 0f));

        CleanUp += () =>
        {
            DestroyIfExistsAlongWithGameobject(firstChecker);
        };

        // === Set Trigger Logic ===
        int militaryUnitCounter = 0;

        firstChecker.OnTriggerEnterCallback = (collider) =>
        {
            if (collider.CompareTag("Military Unit"))
                militaryUnitCounter++;
        };
        firstChecker.OnTriggerExitCallback = (collider) =>
        {
            if (collider.CompareTag("Military Unit"))
                militaryUnitCounter--;
        };

        Vector3 hitPoint = new Vector3(114.2f, 0.0233f, 98.04f);
        MoveUnitsCommand moveUnitsCommand = new MoveUnitsCommand();
        moveUnitsCommand.action = MoveUnitsCommand.commandName;
        moveUnitsCommand.unitIDs = new List<ulong>();
        moveUnitsCommand.unitIDs.AddRange(ids);
        moveUnitsCommand.position = (CommonStructures.SerializableVector3)hitPoint;
        moveUnitsCommand.IsAttackMove = attackMove;
        InputManager.Instance.SendInputCommand(moveUnitsCommand);

        int unitCount = ids.Count;

        var objectiveText = AddObjective("Time left for testing: 0");
        CleanUp += () => DestroyIfExists(objectiveText?.gameObject);
        var objectiveText2 = AddObjective("Military Units in area: 0");
        CleanUp += () => DestroyIfExistsAlongWithGameobject(objectiveText2);

        bool success = false;
        float timer = 0f;

        firstChecker.CheckerCallback = () =>
        {
            if (timer < 30)
            {
                timer += Time.deltaTime;
                objectiveText.text = $"Time left for testing: {(int)timer}";
            } else
            {
                timer = 0f;
                return true;
            }

            if (militaryUnitCounter == unitCount)
            {
                success = true;
                return true;
            }
            objectiveText2.text = $"Military Units in area: {militaryUnitCounter} and expected count is: {unitCount}";
            return false;
        };

        // === Wait for Condition ===
        while (!firstChecker.CheckerCallback())
        {
            yield return null;
        }

        DestroyIfExistsAlongWithGameobject(firstChecker);
        objectiveText2.color = Color.green;
        objectiveText2.fontStyle = TMPro.FontStyles.Strikethrough | TMPro.FontStyles.Bold;

        while (timer < 5)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        CleanUp?.Invoke();
        Assert.IsTrue(success);
    }

    [UnityTest]
    public IEnumerator TestRangedUnitSingleUnit()
    {
        yield return LoadGameScene();

        List<MovableUnit> units = new List<MovableUnit>();

        System.Action CleanUp = () => { };

        int playerOneUnitCounts = 0;
        int playerTwoUnitCounts = 0;

        {
            List<MovableUnit> copyCommandUnits = new();
            Vector3 newCopiedPosition_0 = new Vector3(124.428f, 0f, 95.833f);
            Vector3 newCopiedEulerAngles_0 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_0 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_0 != null)
            {
                movableUnit_0.transform.position = newCopiedPosition_0;
                movableUnit_0.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_0.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_0);
                };
                copyCommandUnits.Add(movableUnit_0);
            }

            // TODO: FIX NEEDED
            foreach (var unit in copyCommandUnits)
            {
                unit.gameObject.SetActive(false);
                unit.unitDataName = "military_units\\hand_cannon";
                unit.playerId = 2;
                unit.gameObject.SetActive(true);
            }

            playerTwoUnitCounts = copyCommandUnits.Count;
            units.AddRange(copyCommandUnits);
        }

        {
            List<MovableUnit> copyCommandUnits = new();

            Vector3 newCopiedPosition_0 = new Vector3(126.99f, 0f, 95.72f);
            Vector3 newCopiedEulerAngles_0 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_0 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_0 != null)
            {
                movableUnit_0.transform.position = newCopiedPosition_0;
                movableUnit_0.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_0.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_0);
                };
                copyCommandUnits.Add(movableUnit_0);
            }
            playerOneUnitCounts = copyCommandUnits.Count;

            // TODO: FIX NEEDED
            foreach (var unit in copyCommandUnits)
            {
                unit.gameObject.SetActive(false);
                unit.unitDataName = "military_units\\hand_cannon";
                unit.playerId = 1;
                unit.gameObject.SetActive(true);
            }

            units.AddRange(copyCommandUnits);
        }

        int playerOneDeaths = 0;
        int playerTwoDeaths = 0;

        void Event_OnDeath(object[] obj)
        {
            ulong selfId = (ulong)obj[0];
            Unit unit = UnitManager.Instance.GetUnit(selfId);
            if (unit)
            {
                if (unit.playerId == 1)
                {
                    playerOneDeaths++;
                }
                if (unit.playerId == 2)
                {
                    playerTwoDeaths++;
                }
            }

            //NativeLogger.Log($"OnDeath Event fired and values are {selfId}");
        }

        UnitEventHandler.Instance.RegisterEvent((int)UnitEventHandler.EventID.OnDeath, Event_OnDeath);
        CleanUp += () =>
        {
            UnitEventHandler.Instance.UnRegisterEvent((int)UnitEventHandler.EventID.OnDeath, Event_OnDeath);
        };

        var objectiveText = AddObjective("Player One Units left: 0");
        CleanUp += () => DestroyIfExists(objectiveText?.gameObject);
        var objectiveText2 = AddObjective("Player Two Units left: 0");
        CleanUp += () => DestroyIfExistsAlongWithGameobject(objectiveText2);
        var objectiveText3 = AddObjective("Time passed for testing: 0");
        CleanUp += () => DestroyIfExistsAlongWithGameobject(objectiveText3);

        float timer = 0.0f;

        System.Func<bool> CheckWhoseUnitsDieEarlier = () =>
        {
            if (timer < 60)
            {
                timer += Time.deltaTime;
                objectiveText3.text = $"Time passed for testing: {(int)timer}";
            }
            else
            {
                Debug.Log("Oddly took far too long for test");
                Assert.Fail();
            }

            if (playerOneUnitCounts == playerOneDeaths)
            {
                objectiveText.color = Color.red;
                objectiveText.fontStyle = TMPro.FontStyles.Strikethrough | TMPro.FontStyles.Bold;
                objectiveText2.color = Color.green;
                objectiveText2.fontStyle = TMPro.FontStyles.Bold;
                return true;
            }
            Assert.Greater(playerOneUnitCounts, playerOneDeaths);

            if (playerTwoUnitCounts == playerTwoDeaths)
            {
                objectiveText2.color = Color.red;
                objectiveText2.fontStyle = TMPro.FontStyles.Strikethrough | TMPro.FontStyles.Bold;
                objectiveText.color = Color.green;
                objectiveText.fontStyle = TMPro.FontStyles.Bold;
                return true;
            }
            Assert.Greater(playerTwoUnitCounts, playerTwoDeaths);

            return false;
        };

        while (!CheckWhoseUnitsDieEarlier())
        {
            objectiveText.text = $"Player One Units left: {playerOneUnitCounts - playerOneDeaths}";
            objectiveText2.text = $"Player Two Units left: {playerTwoUnitCounts - playerTwoDeaths}";
            yield return null;
        }

        var objectiveText4 = AddObjective("Exiting test in 5 seconds...");
        CleanUp += () => DestroyIfExistsAlongWithGameobject(objectiveText4);

        timer = 0;
        while (timer < 5)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        CleanUp?.Invoke();
    }

    [UnityTest]
    public IEnumerator TestAttackBetweenEachOtherMelee()
    {
        yield return LoadGameScene();

        List<MovableUnit> units = new List<MovableUnit>();

        System.Action CleanUp = () => { };
        {
            Vector3 newCopiedPosition_0 = new Vector3(99.53003f, 2.742795f, 98.78004f);
            Vector3 newCopiedEulerAngles_0 = new Vector3(0f, 0f, 0f);


            Vector3 newCopiedPosition_1 = new Vector3(100.04f, 2.742795f, 98.78004f);
            Vector3 newCopiedEulerAngles_1 = new Vector3(0f, 0f, 0f);


            Vector3 newCopiedPosition_2 = new Vector3(99.53003f, 2.742794f, 99.68001f);
            Vector3 newCopiedEulerAngles_2 = new Vector3(0f, 0f, 0f);


            Vector3 newCopiedPosition_3 = new Vector3(100.04f, 2.742795f, 99.68001f);
            Vector3 newCopiedEulerAngles_3 = new Vector3(0f, 0f, 0f);


            Vector3 newCopiedPosition_4 = new Vector3(99.53003f, 2.742795f, 100.34f);
            Vector3 newCopiedEulerAngles_4 = new Vector3(0f, 0f, 0f);


            Vector3 newCopiedPosition_5 = new Vector3(100.04f, 2.742795f, 100.34f);
            Vector3 newCopiedEulerAngles_5 = new Vector3(0f, 0f, 0f);


            Vector3 newCopiedPosition_6 = new Vector3(99.53003f, 2.742795f, 100.99f);
            Vector3 newCopiedEulerAngles_6 = new Vector3(0f, 0f, 0f);


            Vector3 newCopiedPosition_7 = new Vector3(100.04f, 2.742795f, 100.99f);
            Vector3 newCopiedEulerAngles_7 = new Vector3(0f, 0f, 0f);

        }

        int playerOneUnitCounts = 0;
        int playerTwoUnitCounts = 0;

        {
            List<MovableUnit> copyCommandUnits = new();
            Vector3 newCopiedPosition_0 = new Vector3(124.428f, 0f, 95.833f);
            Vector3 newCopiedEulerAngles_0 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_0 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_0 != null)
            {
                movableUnit_0.transform.position = newCopiedPosition_0;
                movableUnit_0.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_0.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_0);
                };
                copyCommandUnits.Add(movableUnit_0);
            }


            Vector3 newCopiedPosition_1 = new Vector3(124.888f, 0f, 95.833f);
            Vector3 newCopiedEulerAngles_1 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_1 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_1 != null)
            {
                movableUnit_1.transform.position = newCopiedPosition_1;
                movableUnit_1.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_1.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_1);
                };
                copyCommandUnits.Add(movableUnit_1);
            }


            Vector3 newCopiedPosition_2 = new Vector3(124.428f, 0f, 97.63f);
            Vector3 newCopiedEulerAngles_2 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_2 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_2 != null)
            {
                movableUnit_2.transform.position = newCopiedPosition_2;
                movableUnit_2.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_2.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_2);
                };
                copyCommandUnits.Add(movableUnit_2);
            }


            Vector3 newCopiedPosition_3 = new Vector3(124.888f, 0f, 97.63f);
            Vector3 newCopiedEulerAngles_3 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_3 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_3 != null)
            {
                movableUnit_3.transform.position = newCopiedPosition_3;
                movableUnit_3.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_3.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_3);
                };
                copyCommandUnits.Add(movableUnit_3);
            }


            Vector3 newCopiedPosition_4 = new Vector3(124.428f, 0f, 94.32001f);
            Vector3 newCopiedEulerAngles_4 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_4 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_4 != null)
            {
                movableUnit_4.transform.position = newCopiedPosition_4;
                movableUnit_4.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_4.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_4);
                };
                copyCommandUnits.Add(movableUnit_4);
            }


            Vector3 newCopiedPosition_5 = new Vector3(124.888f, 0f, 94.32001f);
            Vector3 newCopiedEulerAngles_5 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_5 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_5 != null)
            {
                movableUnit_5.transform.position = newCopiedPosition_5;
                movableUnit_5.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_5.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_5);
                };
                copyCommandUnits.Add(movableUnit_5);
            }


            Vector3 newCopiedPosition_6 = new Vector3(124.428f, 0f, 96.383f);
            Vector3 newCopiedEulerAngles_6 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_6 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_6 != null)
            {
                movableUnit_6.transform.position = newCopiedPosition_6;
                movableUnit_6.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_6.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_6);
                };
                copyCommandUnits.Add(movableUnit_6);
            }


            Vector3 newCopiedPosition_7 = new Vector3(124.888f, 0f, 96.383f);
            Vector3 newCopiedEulerAngles_7 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_7 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_7 != null)
            {
                movableUnit_7.transform.position = newCopiedPosition_7;
                movableUnit_7.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_7.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_7);
                };
                copyCommandUnits.Add(movableUnit_7);
            }


            Vector3 newCopiedPosition_8 = new Vector3(124.428f, 0f, 98.17999f);
            Vector3 newCopiedEulerAngles_8 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_8 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_8 != null)
            {
                movableUnit_8.transform.position = newCopiedPosition_8;
                movableUnit_8.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_8.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_8);
                };
                copyCommandUnits.Add(movableUnit_8);
            }


            Vector3 newCopiedPosition_9 = new Vector3(124.888f, 0f, 98.17999f);
            Vector3 newCopiedEulerAngles_9 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_9 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_9 != null)
            {
                movableUnit_9.transform.position = newCopiedPosition_9;
                movableUnit_9.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_9.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_9);
                };
                copyCommandUnits.Add(movableUnit_9);
            }


            Vector3 newCopiedPosition_10 = new Vector3(124.428f, 0f, 94.87f);
            Vector3 newCopiedEulerAngles_10 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_10 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_10 != null)
            {
                movableUnit_10.transform.position = newCopiedPosition_10;
                movableUnit_10.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_10.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_10);
                };
                copyCommandUnits.Add(movableUnit_10);
            }


            Vector3 newCopiedPosition_11 = new Vector3(124.888f, 0f, 94.87f);
            Vector3 newCopiedEulerAngles_11 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_11 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_11 != null)
            {
                movableUnit_11.transform.position = newCopiedPosition_11;
                movableUnit_11.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_11.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_11);
                };
                copyCommandUnits.Add(movableUnit_11);
            }


            Vector3 newCopiedPosition_12 = new Vector3(124.428f, 0f, 96.983f);
            Vector3 newCopiedEulerAngles_12 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_12 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_12 != null)
            {
                movableUnit_12.transform.position = newCopiedPosition_12;
                movableUnit_12.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_12.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_12);
                };
                copyCommandUnits.Add(movableUnit_12);
            }


            Vector3 newCopiedPosition_13 = new Vector3(124.888f, 0f, 96.983f);
            Vector3 newCopiedEulerAngles_13 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_13 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_13 != null)
            {
                movableUnit_13.transform.position = newCopiedPosition_13;
                movableUnit_13.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_13.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_13);
                };
                copyCommandUnits.Add(movableUnit_13);
            }


            Vector3 newCopiedPosition_14 = new Vector3(124.428f, 0f, 98.78f);
            Vector3 newCopiedEulerAngles_14 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_14 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_14 != null)
            {
                movableUnit_14.transform.position = newCopiedPosition_14;
                movableUnit_14.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_14.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_14);
                };
                copyCommandUnits.Add(movableUnit_14);
            }


            Vector3 newCopiedPosition_15 = new Vector3(124.888f, 0f, 98.78f);
            Vector3 newCopiedEulerAngles_15 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_15 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_15 != null)
            {
                movableUnit_15.transform.position = newCopiedPosition_15;
                movableUnit_15.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_15.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_15);
                };
                copyCommandUnits.Add(movableUnit_15);
            }


            Vector3 newCopiedPosition_16 = new Vector3(124.428f, 0f, 95.47001f);
            Vector3 newCopiedEulerAngles_16 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_16 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_16 != null)
            {
                movableUnit_16.transform.position = newCopiedPosition_16;
                movableUnit_16.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_16.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_16);
                };
                copyCommandUnits.Add(movableUnit_16);
            }


            Vector3 newCopiedPosition_17 = new Vector3(124.888f, 0f, 95.47001f);
            Vector3 newCopiedEulerAngles_17 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_17 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_17 != null)
            {
                movableUnit_17.transform.position = newCopiedPosition_17;
                movableUnit_17.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_17.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_17);
                };
                copyCommandUnits.Add(movableUnit_17);
            }

            foreach (var unit in copyCommandUnits)
            {
                unit.unitDataName = "military_units\\Rodelero";
                unit.playerId = 2;
            }

            playerTwoUnitCounts = copyCommandUnits.Count;
            units.AddRange(copyCommandUnits);
        }

        {
            List<MovableUnit> copyCommandUnits = new();

            Vector3 newCopiedPosition_0 = new Vector3(126.99f, 0f, 95.72f);
            Vector3 newCopiedEulerAngles_0 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_0 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_0 != null)
            {
                movableUnit_0.transform.position = newCopiedPosition_0;
                movableUnit_0.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_0.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_0);
                };
                copyCommandUnits.Add(movableUnit_0);
            }


            Vector3 newCopiedPosition_1 = new Vector3(126.99f, 0f, 95.187f);
            Vector3 newCopiedEulerAngles_1 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_1 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_1 != null)
            {
                movableUnit_1.transform.position = newCopiedPosition_1;
                movableUnit_1.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_1.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_1);
                };
                copyCommandUnits.Add(movableUnit_1);
            }


            Vector3 newCopiedPosition_2 = new Vector3(126.99f, 0f, 96.26f);
            Vector3 newCopiedEulerAngles_2 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_2 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_2 != null)
            {
                movableUnit_2.transform.position = newCopiedPosition_2;
                movableUnit_2.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_2.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_2);
                };
                copyCommandUnits.Add(movableUnit_2);
            }


            Vector3 newCopiedPosition_3 = new Vector3(126.99f, 0f, 98.49699f);
            Vector3 newCopiedEulerAngles_3 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_3 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_3 != null)
            {
                movableUnit_3.transform.position = newCopiedPosition_3;
                movableUnit_3.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_3.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_3);
                };
                copyCommandUnits.Add(movableUnit_3);
            }


            Vector3 newCopiedPosition_4 = new Vector3(127.45f, 0f, 97.517f);
            Vector3 newCopiedEulerAngles_4 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_4 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_4 != null)
            {
                movableUnit_4.transform.position = newCopiedPosition_4;
                movableUnit_4.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_4.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_4);
                };
                copyCommandUnits.Add(movableUnit_4);
            }


            Vector3 newCopiedPosition_5 = new Vector3(127.45f, 0f, 96.26f);
            Vector3 newCopiedEulerAngles_5 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_5 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_5 != null)
            {
                movableUnit_5.transform.position = newCopiedPosition_5;
                movableUnit_5.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_5.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_5);
                };
                copyCommandUnits.Add(movableUnit_5);
            }


            Vector3 newCopiedPosition_6 = new Vector3(127.45f, 0f, 96.7f);
            Vector3 newCopiedEulerAngles_6 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_6 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_6 != null)
            {
                movableUnit_6.transform.position = newCopiedPosition_6;
                movableUnit_6.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_6.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_6);
                };
                copyCommandUnits.Add(movableUnit_6);
            }


            Vector3 newCopiedPosition_7 = new Vector3(126.99f, 0f, 96.7f);
            Vector3 newCopiedEulerAngles_7 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_7 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_7 != null)
            {
                movableUnit_7.transform.position = newCopiedPosition_7;
                movableUnit_7.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_7.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_7);
                };
                copyCommandUnits.Add(movableUnit_7);
            }


            Vector3 newCopiedPosition_8 = new Vector3(127.45f, 0f, 98.057f);
            Vector3 newCopiedEulerAngles_8 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_8 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_8 != null)
            {
                movableUnit_8.transform.position = newCopiedPosition_8;
                movableUnit_8.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_8.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_8);
                };
                copyCommandUnits.Add(movableUnit_8);
            }


            Vector3 newCopiedPosition_9 = new Vector3(126.99f, 0f, 94.74701f);
            Vector3 newCopiedEulerAngles_9 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_9 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_9 != null)
            {
                movableUnit_9.transform.position = newCopiedPosition_9;
                movableUnit_9.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_9.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_9);
                };
                copyCommandUnits.Add(movableUnit_9);
            }


            Vector3 newCopiedPosition_10 = new Vector3(127.45f, 0f, 98.49699f);
            Vector3 newCopiedEulerAngles_10 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_10 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_10 != null)
            {
                movableUnit_10.transform.position = newCopiedPosition_10;
                movableUnit_10.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_10.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_10);
                };
                copyCommandUnits.Add(movableUnit_10);
            }


            Vector3 newCopiedPosition_11 = new Vector3(127.45f, 0f, 94.74701f);
            Vector3 newCopiedEulerAngles_11 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_11 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_11 != null)
            {
                movableUnit_11.transform.position = newCopiedPosition_11;
                movableUnit_11.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_11.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_11);
                };
                copyCommandUnits.Add(movableUnit_11);
            }


            Vector3 newCopiedPosition_12 = new Vector3(126.99f, 0f, 94.20701f);
            Vector3 newCopiedEulerAngles_12 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_12 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_12 != null)
            {
                movableUnit_12.transform.position = newCopiedPosition_12;
                movableUnit_12.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_12.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_12);
                };
                copyCommandUnits.Add(movableUnit_12);
            }


            Vector3 newCopiedPosition_13 = new Vector3(127.45f, 0f, 95.72f);
            Vector3 newCopiedEulerAngles_13 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_13 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_13 != null)
            {
                movableUnit_13.transform.position = newCopiedPosition_13;
                movableUnit_13.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_13.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_13);
                };
                copyCommandUnits.Add(movableUnit_13);
            }


            Vector3 newCopiedPosition_14 = new Vector3(126.99f, 0f, 98.057f);
            Vector3 newCopiedEulerAngles_14 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_14 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_14 != null)
            {
                movableUnit_14.transform.position = newCopiedPosition_14;
                movableUnit_14.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_14.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_14);
                };
                copyCommandUnits.Add(movableUnit_14);
            }


            Vector3 newCopiedPosition_15 = new Vector3(126.99f, 0f, 97.517f);
            Vector3 newCopiedEulerAngles_15 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_15 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_15 != null)
            {
                movableUnit_15.transform.position = newCopiedPosition_15;
                movableUnit_15.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_15.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_15);
                };
                copyCommandUnits.Add(movableUnit_15);
            }


            Vector3 newCopiedPosition_16 = new Vector3(127.45f, 0f, 95.187f);
            Vector3 newCopiedEulerAngles_16 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_16 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_16 != null)
            {
                movableUnit_16.transform.position = newCopiedPosition_16;
                movableUnit_16.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_16.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_16);
                };
                copyCommandUnits.Add(movableUnit_16);
            }


            Vector3 newCopiedPosition_17 = new Vector3(127.45f, 0f, 94.20701f);
            Vector3 newCopiedEulerAngles_17 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_17 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_17 != null)
            {
                movableUnit_17.transform.position = newCopiedPosition_17;
                movableUnit_17.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_17.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_17);
                };
                copyCommandUnits.Add(movableUnit_17);
            }

            playerOneUnitCounts = copyCommandUnits.Count;

            foreach (var unit in copyCommandUnits)
            {
                unit.unitDataName = "military_units\\Rodelero";
            }

            units.AddRange(copyCommandUnits);
        }

        int playerOneDeaths = 0;
        int playerTwoDeaths = 0;

        void Event_OnDeath(object[] obj)
        {
            ulong selfId = (ulong)obj[0];
            Unit unit = UnitManager.Instance.GetUnit(selfId);
            if (unit)
            {
                if (unit.playerId == 1)
                {
                    playerOneDeaths++;
                }
                if (unit.playerId == 2)
                {
                    playerTwoDeaths++;
                }
            }

            //NativeLogger.Log($"OnDeath Event fired and values are {selfId}");
        }

        UnitEventHandler.Instance.RegisterEvent((int)UnitEventHandler.EventID.OnDeath, Event_OnDeath);
        CleanUp += () =>
        {
            UnitEventHandler.Instance.UnRegisterEvent((int)UnitEventHandler.EventID.OnDeath, Event_OnDeath);
        };

        var objectiveText = AddObjective("Player One Units left: 0");
        CleanUp += () => DestroyIfExists(objectiveText?.gameObject);
        var objectiveText2 = AddObjective("Player Two Units left: 0");
        CleanUp += () => DestroyIfExistsAlongWithGameobject(objectiveText2);

        System.Func<bool> CheckWhoseUnitsDieEarlier = () =>
        {
            if (playerOneUnitCounts == playerOneDeaths)
            {
                objectiveText.color = Color.red;
                objectiveText.fontStyle = TMPro.FontStyles.Strikethrough | TMPro.FontStyles.Bold;
                objectiveText2.color = Color.green;
                objectiveText2.fontStyle = TMPro.FontStyles.Bold;
                return true;
            }
            Assert.Greater(playerOneUnitCounts, playerOneDeaths);

            if (playerTwoUnitCounts == playerTwoDeaths)
            {
                objectiveText2.color = Color.red;
                objectiveText2.fontStyle = TMPro.FontStyles.Strikethrough | TMPro.FontStyles.Bold;
                objectiveText.color = Color.green;
                objectiveText.fontStyle = TMPro.FontStyles.Bold;
                return true;
            }
            Assert.Greater(playerTwoUnitCounts, playerTwoDeaths);

            return false;
        };

        while (!CheckWhoseUnitsDieEarlier())
        {
            objectiveText.text = $"Player One Units left: {playerOneUnitCounts - playerOneDeaths}";
            objectiveText2.text = $"Player Two Units left: {playerTwoUnitCounts - playerTwoDeaths}";
            yield return null;
        }

        var objectiveText3 = AddObjective("Exiting test in 5 seconds...");
        CleanUp += () => DestroyIfExistsAlongWithGameobject(objectiveText3);

        float timer = 0;
        while (timer < 5)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        CleanUp?.Invoke();
    }

    [UnityTest]
    public IEnumerator TestAttackBetweenEachOtherRanged()
    {
        yield return LoadGameScene();

        List<MovableUnit> units = new List<MovableUnit>();

        System.Action CleanUp = () => { };

        string rangedUnitName = "military_units\\Cannon";
        string rangedUnitName2 = "military_units\\hand_cannon";

        int playerOneUnitCounts = 0;
        int playerTwoUnitCounts = 0;

        {
            List<MovableUnit> copyCommandUnits = new();
            Vector3 newCopiedPosition_0 = new Vector3(124.428f, 0f, 95.833f);
            Vector3 newCopiedEulerAngles_0 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_0 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_0 != null)
            {
                movableUnit_0.transform.position = newCopiedPosition_0;
                movableUnit_0.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_0.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_0);
                };
                copyCommandUnits.Add(movableUnit_0);
            }


            Vector3 newCopiedPosition_1 = new Vector3(124.888f, 0f, 95.833f);
            Vector3 newCopiedEulerAngles_1 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_1 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_1 != null)
            {
                movableUnit_1.transform.position = newCopiedPosition_1;
                movableUnit_1.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_1.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_1);
                };
                copyCommandUnits.Add(movableUnit_1);
            }


            Vector3 newCopiedPosition_2 = new Vector3(124.428f, 0f, 97.63f);
            Vector3 newCopiedEulerAngles_2 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_2 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_2 != null)
            {
                movableUnit_2.transform.position = newCopiedPosition_2;
                movableUnit_2.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_2.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_2);
                };
                copyCommandUnits.Add(movableUnit_2);
            }


            Vector3 newCopiedPosition_3 = new Vector3(124.888f, 0f, 97.63f);
            Vector3 newCopiedEulerAngles_3 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_3 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_3 != null)
            {
                movableUnit_3.transform.position = newCopiedPosition_3;
                movableUnit_3.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_3.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_3);
                };
                copyCommandUnits.Add(movableUnit_3);
            }


            Vector3 newCopiedPosition_4 = new Vector3(124.428f, 0f, 94.32001f);
            Vector3 newCopiedEulerAngles_4 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_4 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_4 != null)
            {
                movableUnit_4.transform.position = newCopiedPosition_4;
                movableUnit_4.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_4.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_4);
                };
                copyCommandUnits.Add(movableUnit_4);
            }


            Vector3 newCopiedPosition_5 = new Vector3(124.888f, 0f, 94.32001f);
            Vector3 newCopiedEulerAngles_5 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_5 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_5 != null)
            {
                movableUnit_5.transform.position = newCopiedPosition_5;
                movableUnit_5.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_5.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_5);
                };
                copyCommandUnits.Add(movableUnit_5);
            }


            Vector3 newCopiedPosition_6 = new Vector3(124.428f, 0f, 96.383f);
            Vector3 newCopiedEulerAngles_6 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_6 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_6 != null)
            {
                movableUnit_6.transform.position = newCopiedPosition_6;
                movableUnit_6.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_6.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_6);
                };
                copyCommandUnits.Add(movableUnit_6);
            }


            Vector3 newCopiedPosition_7 = new Vector3(124.888f, 0f, 96.383f);
            Vector3 newCopiedEulerAngles_7 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_7 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_7 != null)
            {
                movableUnit_7.transform.position = newCopiedPosition_7;
                movableUnit_7.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_7.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_7);
                };
                copyCommandUnits.Add(movableUnit_7);
            }


            Vector3 newCopiedPosition_8 = new Vector3(124.428f, 0f, 98.17999f);
            Vector3 newCopiedEulerAngles_8 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_8 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_8 != null)
            {
                movableUnit_8.transform.position = newCopiedPosition_8;
                movableUnit_8.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_8.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_8);
                };
                copyCommandUnits.Add(movableUnit_8);
            }


            Vector3 newCopiedPosition_9 = new Vector3(124.888f, 0f, 98.17999f);
            Vector3 newCopiedEulerAngles_9 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_9 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_9 != null)
            {
                movableUnit_9.transform.position = newCopiedPosition_9;
                movableUnit_9.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_9.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_9);
                };
                copyCommandUnits.Add(movableUnit_9);
            }


            Vector3 newCopiedPosition_10 = new Vector3(124.428f, 0f, 94.87f);
            Vector3 newCopiedEulerAngles_10 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_10 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_10 != null)
            {
                movableUnit_10.transform.position = newCopiedPosition_10;
                movableUnit_10.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_10.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_10);
                };
                copyCommandUnits.Add(movableUnit_10);
            }


            Vector3 newCopiedPosition_11 = new Vector3(124.888f, 0f, 94.87f);
            Vector3 newCopiedEulerAngles_11 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_11 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_11 != null)
            {
                movableUnit_11.transform.position = newCopiedPosition_11;
                movableUnit_11.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_11.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_11);
                };
                copyCommandUnits.Add(movableUnit_11);
            }


            Vector3 newCopiedPosition_12 = new Vector3(124.428f, 0f, 96.983f);
            Vector3 newCopiedEulerAngles_12 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_12 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_12 != null)
            {
                movableUnit_12.transform.position = newCopiedPosition_12;
                movableUnit_12.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_12.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_12);
                };
                copyCommandUnits.Add(movableUnit_12);
            }


            Vector3 newCopiedPosition_13 = new Vector3(124.888f, 0f, 96.983f);
            Vector3 newCopiedEulerAngles_13 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_13 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_13 != null)
            {
                movableUnit_13.transform.position = newCopiedPosition_13;
                movableUnit_13.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_13.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_13);
                };
                copyCommandUnits.Add(movableUnit_13);
            }


            Vector3 newCopiedPosition_14 = new Vector3(124.428f, 0f, 98.78f);
            Vector3 newCopiedEulerAngles_14 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_14 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_14 != null)
            {
                movableUnit_14.transform.position = newCopiedPosition_14;
                movableUnit_14.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_14.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_14);
                };
                copyCommandUnits.Add(movableUnit_14);
            }


            Vector3 newCopiedPosition_15 = new Vector3(124.888f, 0f, 98.78f);
            Vector3 newCopiedEulerAngles_15 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_15 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_15 != null)
            {
                movableUnit_15.transform.position = newCopiedPosition_15;
                movableUnit_15.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_15.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_15);
                };
                copyCommandUnits.Add(movableUnit_15);
            }


            Vector3 newCopiedPosition_16 = new Vector3(124.428f, 0f, 95.47001f);
            Vector3 newCopiedEulerAngles_16 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_16 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_16 != null)
            {
                movableUnit_16.transform.position = newCopiedPosition_16;
                movableUnit_16.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_16.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_16);
                };
                copyCommandUnits.Add(movableUnit_16);
            }


            Vector3 newCopiedPosition_17 = new Vector3(124.888f, 0f, 95.47001f);
            Vector3 newCopiedEulerAngles_17 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_17 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_17 != null)
            {
                movableUnit_17.transform.position = newCopiedPosition_17;
                movableUnit_17.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_17.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_17);
                };
                copyCommandUnits.Add(movableUnit_17);
            }

            // TODO: FIX NEEDED
            foreach (var unit in copyCommandUnits)
            {
                unit.gameObject.SetActive(false);
                unit.unitDataName = rangedUnitName;
                unit.playerId = 2;
                unit.gameObject.SetActive(true);
            }

            playerTwoUnitCounts = copyCommandUnits.Count;
            units.AddRange(copyCommandUnits);
        }

        {
            List<MovableUnit> copyCommandUnits = new();

            Vector3 newCopiedPosition_0 = new Vector3(126.99f, 0f, 95.72f);
            Vector3 newCopiedEulerAngles_0 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_0 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_0 != null)
            {
                movableUnit_0.transform.position = newCopiedPosition_0;
                movableUnit_0.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_0.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_0);
                };
                copyCommandUnits.Add(movableUnit_0);
            }


            Vector3 newCopiedPosition_1 = new Vector3(126.99f, 0f, 95.187f);
            Vector3 newCopiedEulerAngles_1 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_1 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_1 != null)
            {
                movableUnit_1.transform.position = newCopiedPosition_1;
                movableUnit_1.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_1.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_1);
                };
                copyCommandUnits.Add(movableUnit_1);
            }


            Vector3 newCopiedPosition_2 = new Vector3(126.99f, 0f, 96.26f);
            Vector3 newCopiedEulerAngles_2 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_2 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_2 != null)
            {
                movableUnit_2.transform.position = newCopiedPosition_2;
                movableUnit_2.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_2.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_2);
                };
                copyCommandUnits.Add(movableUnit_2);
            }


            Vector3 newCopiedPosition_3 = new Vector3(126.99f, 0f, 98.49699f);
            Vector3 newCopiedEulerAngles_3 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_3 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_3 != null)
            {
                movableUnit_3.transform.position = newCopiedPosition_3;
                movableUnit_3.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_3.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_3);
                };
                copyCommandUnits.Add(movableUnit_3);
            }


            Vector3 newCopiedPosition_4 = new Vector3(127.45f, 0f, 97.517f);
            Vector3 newCopiedEulerAngles_4 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_4 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_4 != null)
            {
                movableUnit_4.transform.position = newCopiedPosition_4;
                movableUnit_4.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_4.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_4);
                };
                copyCommandUnits.Add(movableUnit_4);
            }


            Vector3 newCopiedPosition_5 = new Vector3(127.45f, 0f, 96.26f);
            Vector3 newCopiedEulerAngles_5 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_5 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_5 != null)
            {
                movableUnit_5.transform.position = newCopiedPosition_5;
                movableUnit_5.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_5.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_5);
                };
                copyCommandUnits.Add(movableUnit_5);
            }


            Vector3 newCopiedPosition_6 = new Vector3(127.45f, 0f, 96.7f);
            Vector3 newCopiedEulerAngles_6 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_6 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_6 != null)
            {
                movableUnit_6.transform.position = newCopiedPosition_6;
                movableUnit_6.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_6.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_6);
                };
                copyCommandUnits.Add(movableUnit_6);
            }


            Vector3 newCopiedPosition_7 = new Vector3(126.99f, 0f, 96.7f);
            Vector3 newCopiedEulerAngles_7 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_7 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_7 != null)
            {
                movableUnit_7.transform.position = newCopiedPosition_7;
                movableUnit_7.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_7.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_7);
                };
                copyCommandUnits.Add(movableUnit_7);
            }


            Vector3 newCopiedPosition_8 = new Vector3(127.45f, 0f, 98.057f);
            Vector3 newCopiedEulerAngles_8 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_8 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_8 != null)
            {
                movableUnit_8.transform.position = newCopiedPosition_8;
                movableUnit_8.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_8.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_8);
                };
                copyCommandUnits.Add(movableUnit_8);
            }


            Vector3 newCopiedPosition_9 = new Vector3(126.99f, 0f, 94.74701f);
            Vector3 newCopiedEulerAngles_9 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_9 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_9 != null)
            {
                movableUnit_9.transform.position = newCopiedPosition_9;
                movableUnit_9.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_9.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_9);
                };
                copyCommandUnits.Add(movableUnit_9);
            }


            Vector3 newCopiedPosition_10 = new Vector3(127.45f, 0f, 98.49699f);
            Vector3 newCopiedEulerAngles_10 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_10 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_10 != null)
            {
                movableUnit_10.transform.position = newCopiedPosition_10;
                movableUnit_10.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_10.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_10);
                };
                copyCommandUnits.Add(movableUnit_10);
            }


            Vector3 newCopiedPosition_11 = new Vector3(127.45f, 0f, 94.74701f);
            Vector3 newCopiedEulerAngles_11 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_11 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_11 != null)
            {
                movableUnit_11.transform.position = newCopiedPosition_11;
                movableUnit_11.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_11.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_11);
                };
                copyCommandUnits.Add(movableUnit_11);
            }


            Vector3 newCopiedPosition_12 = new Vector3(126.99f, 0f, 94.20701f);
            Vector3 newCopiedEulerAngles_12 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_12 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_12 != null)
            {
                movableUnit_12.transform.position = newCopiedPosition_12;
                movableUnit_12.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_12.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_12);
                };
                copyCommandUnits.Add(movableUnit_12);
            }


            Vector3 newCopiedPosition_13 = new Vector3(127.45f, 0f, 95.72f);
            Vector3 newCopiedEulerAngles_13 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_13 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_13 != null)
            {
                movableUnit_13.transform.position = newCopiedPosition_13;
                movableUnit_13.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_13.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_13);
                };
                copyCommandUnits.Add(movableUnit_13);
            }


            Vector3 newCopiedPosition_14 = new Vector3(126.99f, 0f, 98.057f);
            Vector3 newCopiedEulerAngles_14 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_14 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_14 != null)
            {
                movableUnit_14.transform.position = newCopiedPosition_14;
                movableUnit_14.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_14.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_14);
                };
                copyCommandUnits.Add(movableUnit_14);
            }


            Vector3 newCopiedPosition_15 = new Vector3(126.99f, 0f, 97.517f);
            Vector3 newCopiedEulerAngles_15 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_15 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_15 != null)
            {
                movableUnit_15.transform.position = newCopiedPosition_15;
                movableUnit_15.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_15.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_15);
                };
                copyCommandUnits.Add(movableUnit_15);
            }


            Vector3 newCopiedPosition_16 = new Vector3(127.45f, 0f, 95.187f);
            Vector3 newCopiedEulerAngles_16 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_16 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_16 != null)
            {
                movableUnit_16.transform.position = newCopiedPosition_16;
                movableUnit_16.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_16.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_16);
                };
                copyCommandUnits.Add(movableUnit_16);
            }


            Vector3 newCopiedPosition_17 = new Vector3(127.45f, 0f, 94.20701f);
            Vector3 newCopiedEulerAngles_17 = new Vector3(0f, 0f, 0f);

            MovableUnit movableUnit_17 = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit_17 != null)
            {
                movableUnit_17.transform.position = newCopiedPosition_17;
                movableUnit_17.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_17.y, 0);

                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_17);
                };
                copyCommandUnits.Add(movableUnit_17);
            }

            playerOneUnitCounts = copyCommandUnits.Count;

            // TODO: FIX NEEDED
            foreach (var unit in copyCommandUnits)
            {
                unit.gameObject.SetActive(false);
                unit.unitDataName = rangedUnitName2;
                unit.playerId = 1;
                unit.gameObject.SetActive(true);
            }

            units.AddRange(copyCommandUnits);
        }

        int playerOneDeaths = 0;
        int playerTwoDeaths = 0;

        void Event_OnDeath(object[] obj)
        {
            ulong selfId = (ulong)obj[0];
            Unit unit = UnitManager.Instance.GetUnit(selfId);
            if (unit)
            {
                if (unit.playerId == 1)
                {
                    playerOneDeaths++;
                }
                if (unit.playerId == 2)
                {
                    playerTwoDeaths++;
                }
            }

            //NativeLogger.Log($"OnDeath Event fired and values are {selfId}");
        }

        UnitEventHandler.Instance.RegisterEvent((int)UnitEventHandler.EventID.OnDeath, Event_OnDeath);
        CleanUp += () =>
        {
            UnitEventHandler.Instance.UnRegisterEvent((int)UnitEventHandler.EventID.OnDeath, Event_OnDeath);
        };

        var objectiveText = AddObjective("Player One Units left: 0");
        CleanUp += () => DestroyIfExists(objectiveText?.gameObject);
        var objectiveText2 = AddObjective("Player Two Units left: 0");
        CleanUp += () => DestroyIfExistsAlongWithGameobject(objectiveText2);

        System.Func<bool> CheckWhoseUnitsDieEarlier = () =>
        {
            if (playerOneUnitCounts == playerOneDeaths)
            {
                objectiveText.color = Color.red;
                objectiveText.fontStyle = TMPro.FontStyles.Strikethrough | TMPro.FontStyles.Bold;
                objectiveText2.color = Color.green;
                objectiveText2.fontStyle = TMPro.FontStyles.Bold;
                return true;
            }
            Assert.Greater(playerOneUnitCounts, playerOneDeaths);

            if (playerTwoUnitCounts == playerTwoDeaths)
            {
                objectiveText2.color = Color.red;
                objectiveText2.fontStyle = TMPro.FontStyles.Strikethrough | TMPro.FontStyles.Bold;
                objectiveText.color = Color.green;
                objectiveText.fontStyle = TMPro.FontStyles.Bold;
                return true;
            }
            Assert.Greater(playerTwoUnitCounts, playerTwoDeaths);

            return false;
        };

        while (!CheckWhoseUnitsDieEarlier())
        {
            objectiveText.text = $"Player One Units left: {playerOneUnitCounts - playerOneDeaths}";
            objectiveText2.text = $"Player Two Units left: {playerTwoUnitCounts - playerTwoDeaths}";
            yield return null;
        }

        var objectiveText3 = AddObjective("Exiting test in 5 seconds...");
        CleanUp += () => DestroyIfExistsAlongWithGameobject(objectiveText3);

        float timer = 0;
        while (timer < 5)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        CleanUp?.Invoke();
    }

    [UnityTest]
    public IEnumerator TestDeathUnitSpawn()
    {
        yield return LoadGameScene();

        System.Action CleanUp = () => { };

        List<MovableUnit> copyCommandUnits = new();
        Vector3 newCopiedPosition_0 = new Vector3(126.99f, 0f, 95.72f);
        Vector3 newCopiedEulerAngles_0 = new Vector3(0f, 0f, 0f);

        MovableUnit movableUnit_0 = UnitManager.Instance.GetMovableUnitFromPool();
        if (movableUnit_0 != null)
        {
            movableUnit_0.transform.position = newCopiedPosition_0;
            movableUnit_0.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_0.y, 0);

            CleanUp += () =>
            {
                UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_0);
            };
            copyCommandUnits.Add(movableUnit_0);
        }

        List<ulong> ids = new List<ulong>();
        foreach (var unit in copyCommandUnits)
        {
            ids.Add(unit.id);
        }

        bool hasFiredOnDeathEndAction = false;

        void Event_OnActionEnd(object[] obj)
        {
            string actionEndType = (string)obj[0];
            ulong selfId = (ulong)obj[1];

            if (actionEndType == "DeathEndAction")
            {
                hasFiredOnDeathEndAction = true;
            }
        }

        bool IsCorpseSpawnedProperly = false;

        void Event_OnCorpseSpawn(object[] obj)
        {
            ulong selfId = (ulong)obj[0];
            ulong corpseId = (ulong)obj[1];

            if (selfId == movableUnit_0.id)
            {
                IsCorpseSpawnedProperly = true;
            }
        }

        UnitEventHandler.Instance.RegisterEvent((int)UnitEventHandler.EventID.OnCorpseSpawn, Event_OnCorpseSpawn);
        CleanUp += () =>
        {
            UnitEventHandler.Instance.UnRegisterEvent((int)UnitEventHandler.EventID.OnCorpseSpawn, Event_OnCorpseSpawn);
        };

        UnitEventHandler.Instance.RegisterEvent((int)UnitEventHandler.EventID.OnActionEnd, Event_OnActionEnd);
        CleanUp += () =>
        {
            UnitEventHandler.Instance.UnRegisterEvent((int)UnitEventHandler.EventID.OnActionEnd, Event_OnActionEnd);
        };

        yield return new WaitForSeconds(2);

        DeleteUnitsCommand deleteUnitsCommand = new DeleteUnitsCommand();
        deleteUnitsCommand.action = DeleteUnitsCommand.commandName;
        deleteUnitsCommand.unitIDs = new List<ulong>();
        deleteUnitsCommand.unitIDs.AddRange(ids);
        InputManager.Instance.SendInputCommand(deleteUnitsCommand);

        var objectiveText = AddObjective("Time left for testing: 0");
        CleanUp += () => DestroyIfExists(objectiveText?.gameObject);

        float timer = 0;
        System.Func<bool> ConditionToCheckIfCorpseSpawnCalled = () =>
        {
            if (hasFiredOnDeathEndAction)
            {
                return true;
            }

            if (timer < 5)
            {
                timer += Time.deltaTime;
                objectiveText.text = $"Time passed for testing: {(int)timer}";
                return false;
            }
            return true;
        };

        while (!ConditionToCheckIfCorpseSpawnCalled())
        {
            yield return null;
        }

        timer = 0;

        Assert.IsTrue(hasFiredOnDeathEndAction);

        System.Func<bool> ConditionToSeeTheCorpseAndSeeEndResult = () =>
        {
            if (timer < 5)
            {
                timer += Time.deltaTime;
                objectiveText.text = $"Test is over but time to see the corpse to visualize result. Exiting in: {(int)timer}";
                return false;
            }
            return true;
        };

        while (!ConditionToSeeTheCorpseAndSeeEndResult())
        {
            yield return null;
        }

        Assert.IsTrue(IsCorpseSpawnedProperly);

        CleanUp?.Invoke();
    }

    [UnityTest]
    public IEnumerator TestSubtitle()
    {
        yield return LoadGameScene();

        yield return null;

        Assert.IsTrue(SubtitlePanel.Instance);

        bool hasTestingSubtitlesEnded = false;
        IEnumerator<IDeterministicYieldInstruction> TestDeterministicSubstitles()
        {
            float delay = 3;
            SubtitlePanel.SetText("First Text");
            yield return new DeterministicWaitForSeconds(delay);
            SubtitlePanel.SetText("First Second Text");
            yield return new DeterministicWaitForSeconds(delay);
            SubtitlePanel.SetText("First Third Text");
            yield return new DeterministicWaitForSeconds(delay);
            SubtitlePanel.SetText("1st line\n2nd line\n3rd line");
            yield return new DeterministicWaitForSeconds(delay);
            SubtitlePanel.SetText("Should exit in few seconds....");
            yield return new DeterministicWaitForSeconds(delay);
            hasTestingSubtitlesEnded = true;
        }
        DeterministicUpdateManager.Instance.CoroutineManager.StartCoroutine(TestDeterministicSubstitles());

        while (!hasTestingSubtitlesEnded)
        {
            yield return null;
        }

        yield return new WaitForSeconds(3);
    }

    void DestroyIfExists(GameObject go)
    {
        if (go != null)
            GameObject.Destroy(go);
    }

    void DestroyIfExistsAlongWithGameobject(Component component)
    {
        if (component != null)
        {
            DestroyIfExists(component.gameObject);
        }
    }

    TMPro.TextMeshProUGUI AddObjective(string text)
    {
        var obj = ObjectivePanel.Instance.AddObjectiveText();
        if (obj != null)
            obj.text = text;
        Debug.Assert(obj != null, "ObjectivePanel returned null Text object!");
        return obj;
    }

    bool IsMissionRunning = false;
    int objectiveStatus = 0;

    [UnityTest]
    public IEnumerator TestMissionCoroutine()
    {
        yield return LoadGameScene();

        DeterministicUpdateManager.Instance.CoroutineManager.StartCoroutine(TestRoutine());
        while (!IsMissionRunning)
        {
            yield return null;
        }
        
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        //yield return new WaitForSeconds(2);

        // === Your actual test ===
        //Assert.IsNotNull(UnitManager.Instance, "UnitManager is not available after scene load.");

        System.Action CleanUp = () => { };

        List<MovableUnit> copyCommandUnits = new();
        Vector3 newCopiedPosition_0 = new Vector3(126.99f, 0f, 95.72f);
        Vector3 newCopiedEulerAngles_0 = new Vector3(0f, 0f, 0f);

        MovableUnit movableUnit_0 = UnitManager.Instance.GetMovableUnitFromPool();
        if (movableUnit_0 != null)
        {
            movableUnit_0.transform.position = newCopiedPosition_0;
            movableUnit_0.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_0.y, 0);

            CleanUp += () =>
            {
                UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_0);
            };
            copyCommandUnits.Add(movableUnit_0);
        }


        Vector3 newCopiedPosition_1 = new Vector3(126.99f, 0f, 95.187f);
        Vector3 newCopiedEulerAngles_1 = new Vector3(0f, 0f, 0f);

        MovableUnit movableUnit_1 = UnitManager.Instance.GetMovableUnitFromPool();
        if (movableUnit_1 != null)
        {
            movableUnit_1.transform.position = newCopiedPosition_1;
            movableUnit_1.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_1.y, 0);

            CleanUp += () =>
            {
                UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_1);
            };
            copyCommandUnits.Add(movableUnit_1);
        }


        Vector3 newCopiedPosition_2 = new Vector3(126.99f, 0f, 96.26f);
        Vector3 newCopiedEulerAngles_2 = new Vector3(0f, 0f, 0f);

        MovableUnit movableUnit_2 = UnitManager.Instance.GetMovableUnitFromPool();
        if (movableUnit_2 != null)
        {
            movableUnit_2.transform.position = newCopiedPosition_2;
            movableUnit_2.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_2.y, 0);

            CleanUp += () =>
            {
                UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_2);
            };
            copyCommandUnits.Add(movableUnit_2);
        }


        Vector3 newCopiedPosition_3 = new Vector3(126.99f, 0f, 98.49699f);
        Vector3 newCopiedEulerAngles_3 = new Vector3(0f, 0f, 0f);

        MovableUnit movableUnit_3 = UnitManager.Instance.GetMovableUnitFromPool();
        if (movableUnit_3 != null)
        {
            movableUnit_3.transform.position = newCopiedPosition_3;
            movableUnit_3.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_3.y, 0);

            CleanUp += () =>
            {
                UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_3);
            };
            copyCommandUnits.Add(movableUnit_3);
        }


        Vector3 newCopiedPosition_4 = new Vector3(127.45f, 0f, 97.517f);
        Vector3 newCopiedEulerAngles_4 = new Vector3(0f, 0f, 0f);

        MovableUnit movableUnit_4 = UnitManager.Instance.GetMovableUnitFromPool();
        if (movableUnit_4 != null)
        {
            movableUnit_4.transform.position = newCopiedPosition_4;
            movableUnit_4.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_4.y, 0);

            CleanUp += () =>
            {
                UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_4);
            };
            copyCommandUnits.Add(movableUnit_4);
        }


        Vector3 newCopiedPosition_5 = new Vector3(127.45f, 0f, 96.26f);
        Vector3 newCopiedEulerAngles_5 = new Vector3(0f, 0f, 0f);

        MovableUnit movableUnit_5 = UnitManager.Instance.GetMovableUnitFromPool();
        if (movableUnit_5 != null)
        {
            movableUnit_5.transform.position = newCopiedPosition_5;
            movableUnit_5.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_5.y, 0);

            CleanUp += () =>
            {
                UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_5);
            };
            copyCommandUnits.Add(movableUnit_5);
        }


        Vector3 newCopiedPosition_6 = new Vector3(127.45f, 0f, 96.7f);
        Vector3 newCopiedEulerAngles_6 = new Vector3(0f, 0f, 0f);

        MovableUnit movableUnit_6 = UnitManager.Instance.GetMovableUnitFromPool();
        if (movableUnit_6 != null)
        {
            movableUnit_6.transform.position = newCopiedPosition_6;
            movableUnit_6.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_6.y, 0);

            CleanUp += () =>
            {
                UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_6);
            };
            copyCommandUnits.Add(movableUnit_6);
        }


        Vector3 newCopiedPosition_7 = new Vector3(126.99f, 0f, 96.7f);
        Vector3 newCopiedEulerAngles_7 = new Vector3(0f, 0f, 0f);

        MovableUnit movableUnit_7 = UnitManager.Instance.GetMovableUnitFromPool();
        if (movableUnit_7 != null)
        {
            movableUnit_7.transform.position = newCopiedPosition_7;
            movableUnit_7.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_7.y, 0);

            CleanUp += () =>
            {
                UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_7);
            };
            copyCommandUnits.Add(movableUnit_7);
        }


        Vector3 newCopiedPosition_8 = new Vector3(127.45f, 0f, 98.057f);
        Vector3 newCopiedEulerAngles_8 = new Vector3(0f, 0f, 0f);

        MovableUnit movableUnit_8 = UnitManager.Instance.GetMovableUnitFromPool();
        if (movableUnit_8 != null)
        {
            movableUnit_8.transform.position = newCopiedPosition_8;
            movableUnit_8.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_8.y, 0);

            CleanUp += () =>
            {
                UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_8);
            };
            copyCommandUnits.Add(movableUnit_8);
        }


        Vector3 newCopiedPosition_9 = new Vector3(126.99f, 0f, 94.74701f);
        Vector3 newCopiedEulerAngles_9 = new Vector3(0f, 0f, 0f);

        MovableUnit movableUnit_9 = UnitManager.Instance.GetMovableUnitFromPool();
        if (movableUnit_9 != null)
        {
            movableUnit_9.transform.position = newCopiedPosition_9;
            movableUnit_9.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_9.y, 0);

            CleanUp += () =>
            {
                UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_9);
            };
            copyCommandUnits.Add(movableUnit_9);
        }


        Vector3 newCopiedPosition_10 = new Vector3(127.45f, 0f, 98.49699f);
        Vector3 newCopiedEulerAngles_10 = new Vector3(0f, 0f, 0f);

        MovableUnit movableUnit_10 = UnitManager.Instance.GetMovableUnitFromPool();
        if (movableUnit_10 != null)
        {
            movableUnit_10.transform.position = newCopiedPosition_10;
            movableUnit_10.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_10.y, 0);

            CleanUp += () =>
            {
                UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_10);
            };
            copyCommandUnits.Add(movableUnit_10);
        }


        Vector3 newCopiedPosition_11 = new Vector3(127.45f, 0f, 94.74701f);
        Vector3 newCopiedEulerAngles_11 = new Vector3(0f, 0f, 0f);

        MovableUnit movableUnit_11 = UnitManager.Instance.GetMovableUnitFromPool();
        if (movableUnit_11 != null)
        {
            movableUnit_11.transform.position = newCopiedPosition_11;
            movableUnit_11.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_11.y, 0);

            CleanUp += () =>
            {
                UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_11);
            };
            copyCommandUnits.Add(movableUnit_11);
        }


        Vector3 newCopiedPosition_12 = new Vector3(126.99f, 0f, 94.20701f);
        Vector3 newCopiedEulerAngles_12 = new Vector3(0f, 0f, 0f);

        MovableUnit movableUnit_12 = UnitManager.Instance.GetMovableUnitFromPool();
        if (movableUnit_12 != null)
        {
            movableUnit_12.transform.position = newCopiedPosition_12;
            movableUnit_12.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_12.y, 0);

            CleanUp += () =>
            {
                UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_12);
            };
            copyCommandUnits.Add(movableUnit_12);
        }


        Vector3 newCopiedPosition_13 = new Vector3(127.45f, 0f, 95.72f);
        Vector3 newCopiedEulerAngles_13 = new Vector3(0f, 0f, 0f);

        MovableUnit movableUnit_13 = UnitManager.Instance.GetMovableUnitFromPool();
        if (movableUnit_13 != null)
        {
            movableUnit_13.transform.position = newCopiedPosition_13;
            movableUnit_13.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_13.y, 0);

            CleanUp += () =>
            {
                UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_13);
            };
            copyCommandUnits.Add(movableUnit_13);
        }


        Vector3 newCopiedPosition_14 = new Vector3(126.99f, 0f, 98.057f);
        Vector3 newCopiedEulerAngles_14 = new Vector3(0f, 0f, 0f);

        MovableUnit movableUnit_14 = UnitManager.Instance.GetMovableUnitFromPool();
        if (movableUnit_14 != null)
        {
            movableUnit_14.transform.position = newCopiedPosition_14;
            movableUnit_14.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_14.y, 0);

            CleanUp += () =>
            {
                UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_14);
            };
            copyCommandUnits.Add(movableUnit_14);
        }


        Vector3 newCopiedPosition_15 = new Vector3(126.99f, 0f, 97.517f);
        Vector3 newCopiedEulerAngles_15 = new Vector3(0f, 0f, 0f);

        MovableUnit movableUnit_15 = UnitManager.Instance.GetMovableUnitFromPool();
        if (movableUnit_15 != null)
        {
            movableUnit_15.transform.position = newCopiedPosition_15;
            movableUnit_15.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_15.y, 0);

            CleanUp += () =>
            {
                UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_15);
            };
            copyCommandUnits.Add(movableUnit_15);
        }


        Vector3 newCopiedPosition_16 = new Vector3(127.45f, 0f, 95.187f);
        Vector3 newCopiedEulerAngles_16 = new Vector3(0f, 0f, 0f);

        MovableUnit movableUnit_16 = UnitManager.Instance.GetMovableUnitFromPool();
        if (movableUnit_16 != null)
        {
            movableUnit_16.transform.position = newCopiedPosition_16;
            movableUnit_16.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_16.y, 0);

            CleanUp += () =>
            {
                UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_16);
            };
            copyCommandUnits.Add(movableUnit_16);
        }


        Vector3 newCopiedPosition_17 = new Vector3(127.45f, 0f, 94.20701f);
        Vector3 newCopiedEulerAngles_17 = new Vector3(0f, 0f, 0f);

        MovableUnit movableUnit_17 = UnitManager.Instance.GetMovableUnitFromPool();
        if (movableUnit_17 != null)
        {
            movableUnit_17.transform.position = newCopiedPosition_17;
            movableUnit_17.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_17.y, 0);

            CleanUp += () =>
            {
                UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit_17);
            };
            copyCommandUnits.Add(movableUnit_17);
        }

        bool attackMove = false;
        List<ulong> ids = new List<ulong>();
        foreach (var unit in copyCommandUnits)
        {
            ids.Add(unit.id);
        }

        while (objectiveStatus <= 0)
        {
            yield return null;
        }

        Vector3 hitPoint = new Vector3(114.2f, 0.0233f, 98.04f);
        MoveUnitsCommand moveUnitsCommand = new MoveUnitsCommand();
        moveUnitsCommand.action = MoveUnitsCommand.commandName;
        moveUnitsCommand.unitIDs = new List<ulong>();
        moveUnitsCommand.unitIDs.AddRange(ids);
        moveUnitsCommand.position = (CommonStructures.SerializableVector3)hitPoint;
        moveUnitsCommand.IsAttackMove = attackMove;
        InputManager.Instance.SendInputCommand(moveUnitsCommand);

        yield return new WaitForSeconds(1);

        while (IsMissionRunning)
        {
            yield return null;
        }

        CleanUp?.Invoke();

        yield return new WaitForSeconds(5);
    }

    IEnumerator<IDeterministicYieldInstruction> TestRoutine()
    {
        System.Action CleanUp = () => { };

        try
        {
            IsMissionRunning = true;
            objectiveStatus = 0;
            // === Setup Objective UI ===
            var objectiveText = AddObjective("Preparing mission...");
            CleanUp += () => DestroyIfExists(objectiveText?.gameObject);

            // === Countdown ===
            float timer = 0.0f;
            while (timer < 5.0f)
            {
                timer += DeterministicUpdateManager.FixedStep;
                objectiveText.text = $"Time needed to go to next objective: {(int)timer}";
                yield return new DeterministicWaitForSeconds(0);

                // if (MissionFailedCondition()) yield break; // Add your fail logic
            }

            objectiveText.color = Color.green;
            objectiveText.fontStyle = TMPro.FontStyles.Strikethrough | TMPro.FontStyles.Bold;
            Debug.Log($"Waited 5 seconds, tick is now {DeterministicUpdateManager.Instance.tickCount}");
            objectiveStatus = 1;

            // === Spawn Mission Area Trigger ===
            var firstChecker = MissionCollisionTriggerChecker.SpawnBox(new Vector3(114.44f, 0.341f, 98.06f),
                Quaternion.Euler(0f, 0f, 0f),
                new Vector3(1f, 1f, 1f),
                new Vector3(1f, 1f, 1f),
                new Vector3(0f, 0f, 0f));
            
            CleanUp += () =>
            {
                DestroyIfExistsAlongWithGameobject(firstChecker);
            };

            // === Set Trigger Logic ===
            int militaryUnitCounter = 0;

            firstChecker.OnTriggerEnterCallback = (collider) =>
            {
                if (collider.CompareTag("Military Unit"))
                    militaryUnitCounter++;
            };
            firstChecker.OnTriggerExitCallback = (collider) =>
            {
                if (collider.CompareTag("Military Unit"))
                    militaryUnitCounter--;
            };

            var objectiveText2 = AddObjective("Military Units in area: 0");
            CleanUp += () => DestroyIfExistsAlongWithGameobject(objectiveText2);

            firstChecker.CheckerCallback = () =>
            {
                if (militaryUnitCounter >= 5)
                {
                    timer += DeterministicUpdateManager.FixedStep;
                    objectiveText2.text = $"Keep your units in position for {(int)timer} seconds";
                    if (timer > 5)
                    {
                        return true;
                    }
                }
                else
                {
                    timer = 0;
                    objectiveText2.text = $"Military Units in area: {militaryUnitCounter}";
                }
                return false;
            };

            // === Wait for Condition ===
            while (!firstChecker.CheckerCallback())
            {
                yield return new DeterministicWaitForSeconds(0);
            }

            DestroyIfExistsAlongWithGameobject(firstChecker);
            objectiveText2.color = Color.green;
            objectiveText2.fontStyle = TMPro.FontStyles.Strikethrough | TMPro.FontStyles.Bold;

            MovableUnit movableUnit = UnitManager.Instance.GetMovableUnitFromPool();
            if (movableUnit)
            {
                CleanUp += () =>
                {
                    UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit);
                };
                Vector3 newCopiedPosition = new Vector3(114.76f, 0.0f, 102.34f);
                movableUnit.playerId = 2;
                movableUnit.transform.position = newCopiedPosition;
                movableUnit.transform.eulerAngles = new Vector3(0, 68.423f, 0);
            }

            while (StatComponent.IsUnitAliveOrValid(movableUnit))
            {
                yield return new DeterministicWaitForSeconds(0);
            }

            IsMissionRunning = false;
            //yield return new DeterministicWaitForSeconds(10);
            // I want dialogue texts here

            //yield return new DeterministicWaitForSeconds(15);
        }
        finally
        {
            CleanUp?.Invoke();
        }

        // === Local Helpers ===

        TMPro.TextMeshProUGUI AddObjective(string text)
        {
            var obj = ObjectivePanel.Instance.AddObjectiveText();
            if (obj != null)
                obj.text = text;
            Debug.Assert(obj != null, "ObjectivePanel returned null Text object!");
            return obj;
        }

        void DestroyIfExists(GameObject go)
        {
            if (go != null)
                GameObject.Destroy(go);
        }

        void DestroyIfExistsAlongWithGameobject(Component component)
        {
            if (component != null)
            {
                DestroyIfExists(component.gameObject);
            }
        }
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    //[UnityTest]
    //public IEnumerator NewTestScriptWithEnumeratorPasses()
    //{
    //    // Use the Assert class to test conditions.
    //    // Use yield to skip a frame.
    //    yield return null;
    //}
}