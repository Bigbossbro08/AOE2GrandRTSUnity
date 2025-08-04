using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

public class UnitMovementTests : BasePlayModeTest
{
    /*
    [UnityTest]
    public IEnumerator TestRegisteredUnits()
    {
        yield return LoadGameScene();

        System.Action CleanUp = () => { };

        List<string> registeredUnitNames = new List<string>() {
            "military_units\\Cannon",
            "military_units\\Rodelero",
            "military_units\\hand_cannon",
            "military_units\\Rodelero"
        };

        List<MovableUnit> units = new List<MovableUnit>();

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
            moveUnitsCommand.position = hitPoint;
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

            System.Action<MovableUnit> SetSpawnData = (unit) =>
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
                promptUnitForNewTest = false;
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

    */
    
    static readonly Vector3 DefaultSpawnPos = new(126.99f, 0f, 95.72f);
    static readonly Vector3 DefaultEuler = Vector3.zero;
    static readonly Vector3 TestTriggerPos = new(90.59f, 2.742795f, 99.88f);

    [UnityTest]
    public IEnumerator TestMovementInGroup()
    {
        yield return LoadGameScene();

        // 1) pick your start positions in a simple list
        var positions = new List<Vector3> {
            new Vector3(99.55f, 2.742795f, 99.02f),
            new Vector3(100.13f, 2.742795f, 99.06f),
            new Vector3(99.53003f, 2.742794f, 99.68001f),
            new Vector3(100.04f, 2.742795f, 99.68001f),
            new Vector3(99.53003f, 2.742795f, 100.34f),
            new Vector3(100.04f, 2.742795f, 100.34f),
            new Vector3(99.53003f, 2.742795f, 100.99f),
            new Vector3(100.04f, 2.742795f, 100.99f),
        };

        GameManager.Instance.CameraHandler.transform.position = TestTriggerPos;

        // 2) spawn them all in one call
        var units = SpawnUnitsAt(positions, u => u.playerId = 1);

        // 3) set up trigger and counter
        int hitCount = 0;

        var trigger = MissionCollisionTriggerChecker.SpawnBox(TestTriggerPos, Quaternion.identity, Vector3.one * 2, Vector3.one, Vector3.zero,
            null,
            (c) =>
            {
                if (c.CompareTag("Military Unit")) ++hitCount;
            },
            (c) => {
                if (c.CompareTag("Military Unit")) ++hitCount;
            });

        _cleanUps.Add(() => DestroyIfExistsAlongWithGameobject(trigger));

        // 4) issue your move command
        InputManager.Instance.SendInputCommand(new MoveUnitsCommand
        {
            action = MoveUnitsCommand.commandName,
            unitIDs = units.Select(u => u.id).ToList(),
            position = TestTriggerPos,
            IsAttackMove = false
        });

        // 5) wait until all of them have entered
        yield return WaitUntilCount(units.Count, () => hitCount);

        yield return new WaitForSeconds(5);

        // 6) clean up is automatic; final assert
        Assert.Pass("All units arrived successfully");
    }

    [UnityTest]
    public IEnumerator TestMeleeCombat()
    {
        yield return LoadGameScene();

        // 1) pick your start positions in a simple list
        var p1_positions = new List<Vector3> {
            new Vector3(99.55f, 2.742795f, 99.02f),
            new Vector3(100.13f, 2.742795f, 99.06f),
            new Vector3(99.53003f, 2.742794f, 99.68001f),
            new Vector3(100.04f, 2.742795f, 99.68001f),
            new Vector3(99.53003f, 2.742795f, 100.34f),
            new Vector3(100.04f, 2.742795f, 100.34f),
            new Vector3(99.53003f, 2.742795f, 100.99f),
            new Vector3(100.04f, 2.742795f, 100.99f),
        };

        var p2_positions = new List<Vector3>
        {
            new Vector3(103.12f, 2.742795f, 99.02f),
            new Vector3(103.7f, 2.742795f, 99.06f),
            new Vector3(103.1f, 2.742794f, 99.68001f),
            new Vector3(103.61f, 2.742795f, 99.68001f),
            new Vector3(103.1f, 2.742795f, 100.34f),
            new Vector3(103.61f, 2.742795f, 100.34f),
            new Vector3(103.1f, 2.742795f, 100.99f),
            new Vector3(103.62f, 2.742795f, 100.78f),
        };

        Vector3 centerPos = Vector3.zero;
        foreach (var position in p1_positions)
        {
            centerPos += position;
        }
        foreach (var position in p2_positions)
        {
            centerPos += position;
        }
        centerPos /= p1_positions.Count + p2_positions.Count;

        GameManager.Instance.CameraHandler.transform.position = centerPos;

        // 2) spawn them all in one call
        var p1_units = SpawnUnitsAt(p1_positions, u => u.playerId = 1);
        var p2_units = SpawnUnitsAt(p2_positions, u => u.playerId = 2);

        int playerOneUnitCounts = p1_units.Count;
        int playerTwoUnitCounts = p2_units.Count;
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
        _cleanUps.Add(() =>
        {
            UnitEventHandler.Instance.UnRegisterEvent((int)UnitEventHandler.EventID.OnDeath, Event_OnDeath);
        });

        bool UnitDeathCheck()
        {
            if (playerOneDeaths == playerOneUnitCounts)
            {
                return true;
            }
            if (playerTwoDeaths == playerTwoUnitCounts) {
                return true;
            }
            return false;
        }

        while (!UnitDeathCheck())
        {
            yield return null;
        }

        yield return new WaitForSeconds(5);

        // 6) clean up is automatic; final assert
        Assert.Pass("All units arrived successfully");
    }

    [UnityTest]
    public IEnumerator TestSingleUnitMovement()
    {
        yield return LoadGameScene();

        GameManager.Instance.CameraHandler.transform.position = TestTriggerPos;

        // spawn & init
        var unit = SpawnUnit(DefaultSpawnPos, DefaultEuler, u => u.playerId = 1);

        // create the trigger
        int hits = 0;

        var trigger = CreateTriggerBox(TestTriggerPos, Quaternion.identity, Vector3.one, Vector3.one, Vector3.zero,
            null, (c) => {
                if (c.CompareTag("Military Unit"))
                    hits++;
            }, 
            null);

        // issue move command
        InputManager.Instance.SendInputCommand(
          new MoveUnitsCommand {
              action = MoveUnitsCommand.commandName,
              unitIDs = new List<ulong> { unit.id }, 
              position = TestTriggerPos ,
              IsAttackMove = false
          });

        // wait for it to enter
        yield return WaitForCondition(() =>
        {
            return hits == 1 && unit.movementComponent.GetPathPositions().Count == 0 &&
                unit.movementComponent.movementState == MovementComponent.State.Idle;
        }, 30);

        // optional: verify final state
        Assert.That(unit.movementComponent.movementState,
                    Is.EqualTo(MovementComponent.State.Idle));
    }
    /*
    [UnityTest]
    public IEnumerator TestSingleUnitMovement()
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
        Vector3 hitPoint = new Vector3(114.44f, 0.0341f, 98.06f);
        MoveUnitsCommand moveUnitsCommand = new MoveUnitsCommand();
        moveUnitsCommand.action = MoveUnitsCommand.commandName;
        moveUnitsCommand.unitIDs = new List<ulong>();
        moveUnitsCommand.unitIDs.AddRange(ids);
        moveUnitsCommand.position = hitPoint;
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
            if (pathPositions.Count > 0)
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
        moveUnitsCommand.position = hitPoint;
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
            }
            else
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
    */
}
