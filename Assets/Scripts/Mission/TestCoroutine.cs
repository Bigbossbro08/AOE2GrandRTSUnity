using log4net;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using static PathfinderTest;
using static UnityEditor.PlayerSettings;

public class TestCoroutine : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DeterministicUpdateManager.Instance.CoroutineManager.StartCoroutine(SetupEnemyShip());
    }

    IEnumerator<IDeterministicYieldInstruction> SetupEnemyShip()
    {
        System.Action CleanUp = () => { };
        try
        {
            List<(Vector3, float)> newEnemyPos = new()
            {
                new (new Vector3(44.04001f, 1.53375f, 31.58001f), 68.44099f),
                new (new Vector3(44.53402f, 1.446641f, 31.775f), 68.44099f),
                new (new Vector3(44.20001f, 1.52628f, 31.17601f), 68.44099f),
                new (new Vector3(44.69402f, 1.439171f, 31.37102f), 68.44099f),
                new (new Vector3(43.86f, 1.542044f, 32.03701f), 68.44099f),
                new (new Vector3(44.35402f, 1.454935f, 32.23202f), 68.44099f),
                new (new Vector3(44.36301f, 1.518688f, 30.76402f), 68.44099f),
                new (new Vector3(44.85701f, 1.431581f, 30.95902f), 68.44099f),
            };

            var PlayerSpawnDatas = new List<(Vector3, float)>
            {
                new (new Vector3(51.78801f, 0.2630772f, 32.487f),       68.44099f),
                new (new Vector3(52.28202f, 0.1759691f, 32.682f),       68.44099f),
                new (new Vector3(51.94801f, 0.255607f, 32.083f),        68.44099f),
                new (new Vector3(52.44202f, 0.1684988f, 32.27801f),     68.44099f),
                new (new Vector3(51.608f, 0.2713701f, 32.944f),         68.44099f),
                new (new Vector3(52.10201f, 0.1842639f, 33.139f),       68.44099f),
                new (new Vector3(52.111f, 0.2480164f, 31.67101f),       68.44099f),
                new (new Vector3(52.60501f, 0.1609079f, 31.86601f),     68.44099f),
            };

            List<MovableUnit> playerUnits = new();
            // Spawn player units
            foreach (var (pos, angle) in PlayerSpawnDatas)
            {
                System.Action<Unit> PreSpawnAction = (unit) =>
                {
                    MovableUnit movableUnit = unit as MovableUnit;
                    movableUnit.unitDataName = "military_units\\Rodelero";
                    movableUnit.playerId = 1;
                    movableUnit.transform.position = pos;
                    movableUnit.transform.eulerAngles = new Vector3(0, angle, 0);

                    CleanUp += () =>
                    {
                        UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit);
                    };
                };
                var playerUnit = UnitManager.Instance.GetMovableUnitFromPool(PreSpawnAction);
                playerUnit.statComponent.SetHealth(200, playerUnit, 200);
                playerUnits.Add(playerUnit);
            }

            MovableUnit playerShip = null;
            {
                // Spawn ship
                Vector3 pos = new Vector3(54.387f, -3.699065E-06f, 31.019f);
                float angle = 346.5634f;

                System.Action<Unit> PreSpawnAction = (unit) =>
                {
                    MovableUnit movableUnit = unit as MovableUnit;
                    movableUnit.unitDataName = "ship_units\\TestShip";
                    movableUnit.playerId = 1;
                    movableUnit.transform.position = pos;
                    movableUnit.transform.eulerAngles = new Vector3(0, angle, 0);

                    CleanUp += () =>
                    {
                        UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit);
                    };
                };
                playerShip = UnitManager.Instance.GetMovableUnitFromPool(PreSpawnAction);
            }
            List<MovableUnit> enemyUnits = new();
            var enemyPositions = new List<(Vector3, float)> {
                new (new Vector3(79.547f, 0.2167008f, 56.25601f), 238.6242f),
                new (new Vector3(79.363f, 0.2167008f, 56.557f), 238.6242f),
                new (new Vector3(79.82101f, 0.2095419f, 55.805f), 238.6242f),
                new (new Vector3(79.151f, 0.2167008f, 56.903f), 238.6242f),
                new (new Vector3(78.62701f, 0.1178082f, 56.58301f), 238.6242f),
                new (new Vector3(78.85401f, 0.1119432f, 56.21001f), 238.6242f),
                new (new Vector3(79.05301f, 0.1069054f, 55.884f), 238.6242f),
                new (new Vector3(79.23701f, 0.1021865f, 55.58201f), 238.6242f),
            };

            void SpawnNPC(Vector3 pos, float angle, ulong playerId, List<MovableUnit> refList)
            {
                System.Action<Unit> PreSpawnAction = (unit) =>
                {
                    MovableUnit movableUnit = unit as MovableUnit;
                    movableUnit.unitDataName = "military_units\\Rodelero";
                    movableUnit.playerId = playerId;
                    movableUnit.transform.position = pos;
                    movableUnit.transform.eulerAngles = new Vector3(0, angle, 0);

                    CleanUp += () =>
                    {
                        UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit);
                    };
                };
                var npc = UnitManager.Instance.GetMovableUnitFromPool(PreSpawnAction);
                if (refList != null)
                {
                    refList.Add(npc);
                }
            }

            // Spawn units
            foreach (var (pos, angle) in enemyPositions)
            {
                //SpawnNPC(pos, angle, 2, enemyUnits);
            }

            // Spawn units
            foreach (var (pos, angle) in newEnemyPos)
            {
                Vector3 newPos = pos;
                newPos.y += 0.1f;
                SpawnNPC(newPos, angle, 2, null);
            }

            MovableUnit enemy_ship = null;
            {
                // Spawn ship
                Vector3 pos = new Vector3(78.03f, -6.5943E-06f, 55.335f);
                float angle = 322.5246f;
                System.Action<Unit> PreSpawnAction = (unit) =>
                {
                    MovableUnit movableUnit = unit as MovableUnit;
                    movableUnit.unitDataName = "ship_units\\TestShip";
                    movableUnit.playerId = 2;
                    movableUnit.transform.position = pos;
                    movableUnit.transform.eulerAngles = new Vector3(0, angle, 0);

                    CleanUp += () =>
                    {
                        UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit);
                    };
                };
                enemy_ship = UnitManager.Instance.GetMovableUnitFromPool(PreSpawnAction);
            }

            yield return new DeterministicWaitForSeconds(2);

            if (enemy_ship)
            {
                List<ulong> unitIds = new List<ulong>();
                foreach (var npc in enemyUnits)
                {
                    unitIds.Add(npc.id);
                }

                MoveUnitsCommand moveUnitsCommand = new MoveUnitsCommand();
                moveUnitsCommand.action = MoveUnitsCommand.commandName;
                moveUnitsCommand.unitIDs = unitIds;
                moveUnitsCommand.position = enemy_ship.transform.position;
                moveUnitsCommand.IsAttackMove = false;
                InputManager.Instance.SendInputCommand(moveUnitsCommand);
            }

            yield return new DeterministicWaitForSeconds(1);

            if (playerShip)
            {
                List<ulong> unitIds = new List<ulong>();
                foreach (var npc in playerUnits)
                {
                    unitIds.Add(npc.id);
                }

                MoveUnitsCommand moveUnitsCommand = new MoveUnitsCommand();
                moveUnitsCommand.action = MoveUnitsCommand.commandName;
                moveUnitsCommand.unitIDs = unitIds;
                moveUnitsCommand.position = playerShip.transform.position;
                moveUnitsCommand.IsAttackMove = false;
                InputManager.Instance.SendInputCommand(moveUnitsCommand);
            }

            bool CheckIfAllHaveUnitsOnBoard()
            {
                if (enemy_ship.shipData.unitsOnShip.Count != enemyUnits.Count)
                {
                    return false;
                }
                if (playerShip.shipData.unitsOnShip.Count != playerUnits.Count)
                {
                    return false;
                }
                return true;
            }
            
            while (!CheckIfAllHaveUnitsOnBoard())
            {
                yield return new DeterministicWaitForSeconds(0);
            }

            {
                Vector3 newCopiedPosition_0 = new Vector3(69.9f, -3.699065E-06f, 39.59f);

                MoveUnitsCommand moveUnitsCommand = new MoveUnitsCommand();
                moveUnitsCommand.action = MoveUnitsCommand.commandName;
                moveUnitsCommand.unitIDs = new List<ulong>() { enemy_ship.id };
                moveUnitsCommand.position = newCopiedPosition_0;
                moveUnitsCommand.IsAttackMove = false;
                InputManager.Instance.SendInputCommand(moveUnitsCommand);

                yield return new DeterministicWaitForSeconds(1);

                BoardToShipCommand dockToShipCommand = new BoardToShipCommand();
                dockToShipCommand.action = BoardToShipCommand.commandName;
                dockToShipCommand.unitIDs = new List<ulong> { playerShip.id };
                dockToShipCommand.targetID = enemy_ship.id;
                InputManager.Instance.SendInputCommand(dockToShipCommand);
            }

            bool CheckShipAndNPCsDead()
            {
                // Exit early if any command unit is alive
                foreach (var u in playerUnits)
                {
                    if (StatComponent.IsUnitAliveOrValid(u))
                        return false;
                }

                // Check if ship is alive
                if (StatComponent.IsUnitAliveOrValid(playerShip))
                    return false;

                // Exit early if any command unit is alive
                foreach (var u in enemyUnits)
                {
                    if (StatComponent.IsUnitAliveOrValid(u))
                        return false;
                }

                // Check if ship is alive
                if (StatComponent.IsUnitAliveOrValid(enemy_ship))
                    return false;

                // All are dead
                return true;
            }

            while (!CheckShipAndNPCsDead())
            {
                yield return new DeterministicWaitForSeconds(0);
            }

            CleanUp?.Invoke();

        }
        finally
        {
            CleanUp?.Invoke();
        }
    }
}
