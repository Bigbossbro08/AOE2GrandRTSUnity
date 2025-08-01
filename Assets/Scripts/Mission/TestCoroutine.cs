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
            var playerPositions = new List<Vector3>()
            {
                new Vector3(127.45f, 0f, 99.6f),
                new Vector3(127.45f, 0f, 97.517f),
                new Vector3(127.45f, 0f, 98.54f),
                new Vector3(127.45f, 0f, 98.03f),
                new Vector3(127.45f, 0f, 99.11f),
                new Vector3(127.45f, 0f, 99.6f),
                new Vector3(127.55f, 0f, 97.517f),
                new Vector3(127.55f, 0f, 98.54f),
            };

            List<MovableUnit> playerUnits = new();
            // Spawn player units
            foreach (var pos in playerPositions)
            {
                System.Action<Unit> PreSpawnAction = (unit) =>
                {
                    MovableUnit movableUnit = unit as MovableUnit;
                    movableUnit.unitDataName = "military_units\\Rodelero";
                    movableUnit.playerId = 1;
                    movableUnit.transform.position = pos;
                    movableUnit.transform.eulerAngles = new Vector3(0, 0, 0);

                    CleanUp += () =>
                    {
                        UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit);
                    };
                };
                var playerUnit = UnitManager.Instance.GetMovableUnitFromPool(PreSpawnAction);
                playerUnits.Add(playerUnit);
            }

            MovableUnit playerShip = null;
            {
                // Spawn ship
                Vector3 pos = new Vector3(132.1f, 0f, 97.517f);

                System.Action<Unit> PreSpawnAction = (unit) =>
                {
                    MovableUnit movableUnit = unit as MovableUnit;
                    movableUnit.unitDataName = "ship_units\\TestShip";
                    movableUnit.playerId = 1;
                    movableUnit.transform.position = pos;
                    movableUnit.transform.eulerAngles = new Vector3(0, 0, 0);

                    CleanUp += () =>
                    {
                        UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit);
                    };
                };
                playerShip = UnitManager.Instance.GetMovableUnitFromPool(PreSpawnAction);
            }

            List<MovableUnit> enemyUnits = new();
            var enemyPositions = new List<Vector3> {
                new(128.49f, 0f, 110.85f),
                new(128.49f, 0f, 110.36f),
                new(128.49f, 0f, 108.767f),
                new(128.49f, 0f, 109.28f),
                new(128.49f, 0f, 109.79f),
            };

            // Spawn units
            foreach (var pos in enemyPositions)
            {
                System.Action<Unit> PreSpawnAction = (unit) =>
                {
                    MovableUnit movableUnit = unit as MovableUnit;
                    movableUnit.unitDataName = "military_units\\Rodelero";
                    movableUnit.playerId = 2;
                    movableUnit.transform.position = pos;
                    movableUnit.transform.eulerAngles = new Vector3(0, 0, 0);

                    CleanUp += () =>
                    {
                        UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit);
                    };
                };
                var npc = UnitManager.Instance.GetMovableUnitFromPool(PreSpawnAction);
                enemyUnits.Add(npc);
            }

            MovableUnit enemy_ship = null;
            {
                // Spawn ship
                Vector3 pos = new Vector3(132.1f, 0f, 109.48f);

                System.Action<Unit> PreSpawnAction = (unit) =>
                {
                    MovableUnit movableUnit = unit as MovableUnit;
                    movableUnit.unitDataName = "ship_units\\TestShip";
                    movableUnit.playerId = 2;
                    movableUnit.transform.position = pos;
                    movableUnit.transform.eulerAngles = new Vector3(0, 0, 0);

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
                Vector3 newCopiedPosition_0 = new Vector3(136.63f, 0f, 109.38f);

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
