using CoreGameUnitAI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using static MovementComponent;
using static Utilities;

[System.Serializable]
public class InputCommand
{
    public ulong frame;      // The frame when this input should execute
    public int playerID;   // Who sent this input
    public string action;  // The type of action (e.g., "Move", "Attack")
}



public class MoveUnitCommand : InputCommand
{
    public const string commandName = "Move Unit Command";
    public ulong unitID;
    public Vector3 position;

    public void Execute()
    {
        Unit unit = UnitManager.Instance.GetUnit(unitID);
        if (unit)
        {
            MovableUnit movableUnit = (MovableUnit)unit;
            if (StatComponent.IsUnitAliveOrValid(movableUnit))
            {
                ulong newCrowdID = ++UnitManager.crowdIDCounter;
                movableUnit.ResetUnit(true);
                //movableUnit.movementComponent.StartPathfind(position);
                //movableUnit.SetAIModule(UnitAIModule.AIModule.BasicMovementAIModule, position, newCrowdID);
                movableUnit.aiController.SetAI(new MoveToPositionAI(movableUnit.aiController, position, newCrowdID));
                //movableUnit.movementComponent.crowdID = newCrowdID;
            }
        }
    }
}

public class MoveUnitsCommand : InputCommand
{
    public const string commandName = "Move Units Command";
    public List<ulong> unitIDs;
    public Vector3 position;
    public bool IsAttackMove = false;

    public void ArrangeUnits_New(List<Unit> units, Vector3 position, ulong crowdID)
    {
        if (units == null || units.Count == 0) return;

        ulong newCrowdID = ++UnitManager.crowdIDCounter;

        // 1. Build formation positions
        const float unitWidth = 0.14f;
        const float interUnitSpacing = 0.14f;
        float spacing = unitWidth + interUnitSpacing;
        int totalUnits = units.Count;
        int columns = Mathf.CeilToInt(Mathf.Sqrt(totalUnits));
        List<Vector3> formationOffsets = new List<Vector3>();

        Vector3 center = Vector3.zero;
        for (int i = 0; i < totalUnits; i++)
        {
            float row = Mathf.Floor(i / (float)columns);
            float col = i % columns;

            float unitsInRow = Mathf.Min(columns, totalUnits - row * columns);
            float totalRowWidth = (unitsInRow - 1) * spacing;
            float offsetX = -totalRowWidth / 2f;

            float x = col * spacing + offsetX;
            float z = row * spacing;

            formationOffsets.Add(new Vector3(x, 0, -z)); // negative z for forward
            center += units[i].transform.position;
        }

        center /= totalUnits;

        // 2. Build cost matrix
        float[,] costMatrix = new float[totalUnits, totalUnits];
        for (int i = 0; i < totalUnits; i++)
        {
            Vector3 unitPos = units[i].transform.position;
            for (int j = 0; j < totalUnits; j++)
            {
                Vector3 targetPos = position + formationOffsets[j];
                costMatrix[i, j] = Vector3.SqrMagnitude(unitPos - targetPos); // Use squared for performance
            }
        }

        // 3. Solve assignment
        List<int> assignment = HungarianAlgorithm.Solve(costMatrix).ToList();
        assignment.Reverse();

        NavMeshPath path = new NavMeshPath();
        Vector3 start = center;
        Vector3 end = position;

        MovableUnit movable = units[0] as MovableUnit;
        int areaMask = movable.movementComponent.GetAreaMask();
        System.Action groupMovementAction = () =>
        {
            if (GridGeneration.CalculatePath(start, end, areaMask, ref path))
            {
                for (int j = 0; j < totalUnits; j++)
                {
                    Unit unit = units[j];
                    MovableUnit movableUnit = (MovableUnit)unit;
                    if (StatComponent.IsUnitAliveOrValid(movableUnit))
                    {
                        if (movableUnit.movementComponent.GetPathfindingStatus() == PathfindingStatus.CalledForCancellingOfPathfinding)
                        {
                            movableUnit.movementComponent.SetPathfindingStatus(PathfindingStatus.NoPathfindingCalled);
                            continue;
                        }

                        List<Vector3> newPath = new List<Vector3>();
                        for (int i = 1; i < path.corners.Length; i++)
                        {
                            var prev = path.corners[i - 1];
                            var next = path.corners[i];
                            Quaternion rotation = Quaternion.LookRotation((prev - next).normalized, Vector3.up);
                            int assignedIndex = assignment[j];
                            Vector3 offset = rotation * formationOffsets[assignedIndex];
                            RaycastHit? hit = SelectionController.FindProperHit(position, areaMask);
                            var pos = next + offset;
                            pos = hit == null ? pos : hit.Value.point;
                            DebugExtension.DebugWireSphere(pos, Color.magenta, 0.1f, 2);
                            newPath.Add(next + offset);
                        }
                        if (IsAttackMove)
                        {
                            movableUnit.aiController.SetAI(new AttackMoveAI(movableUnit.aiController, newPath, crowdID), false, true);
                        }
                        else
                        {
                            movableUnit.aiController.SetAI(new MoveToPositionAI(movableUnit.aiController, newPath, crowdID), false, true);
                        }
                        //movableUnit.movementComponent.SetPositionData(newPath);
                    }
                }
            }
        };
        PathfindingManager.Instance.RequestPathfinding(movable.movementComponent, groupMovementAction, 0);
        return;

        // 4. Issue move commands
        Vector3 startPathfindingPostion = units[0].transform.position;
        for (int i = 0; i < totalUnits; i++)
        {
            Unit unit = units[i];
            MovableUnit movableUnit = (MovableUnit)unit;
            if (StatComponent.IsUnitAliveOrValid(movableUnit))
            {
                int assignedIndex = assignment[i];
                Vector3 offset = formationOffsets[assignedIndex];
                MoveToCommand(unit, position, newCrowdID, offset, center);
            }
        }
    }

    public static List<List<Unit>> ClusterUnits(List<Unit> units, float clusterRadius)
    {
        List<List<Unit>> clusters = new();
        HashSet<Unit> visited = new();

        foreach (Unit unit in units)
        {
            if (visited.Contains(unit)) continue;

            List<Unit> cluster = new();
            Queue<Unit> queue = new();
            queue.Enqueue(unit);
            visited.Add(unit);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                cluster.Add(current);

                foreach (var other in units)
                {
                    if (!visited.Contains(other) &&
                        Vector3.Distance(current.transform.position, other.transform.position) < clusterRadius)
                    {
                        queue.Enqueue(other);
                        visited.Add(other);
                    }
                }
            }

            clusters.Add(cluster);
        }

        return clusters;
    }

    void MoveToCommand(Unit unit, Vector3 position, ulong newCrowdID, Vector3 offset = default, Vector3? startPosition = null)
    {
        MovableUnit movableUnit = (MovableUnit)unit;
        if (StatComponent.IsUnitAliveOrValid(movableUnit))
        {
            movableUnit.ResetUnit(true);
            if (startPosition == null)
            {
                startPosition = unit.transform.position;
            }
            Vector3 diff = startPosition.Value - position;
            float sqrMagnitude = diff.sqrMagnitude;
            const float distanceForFastRearrange = 5;
            const float distanceForFastRearrangeSqr = distanceForFastRearrange * distanceForFastRearrange;
            if (sqrMagnitude < distanceForFastRearrangeSqr)
            {
                startPosition = unit.transform.position;
            }

            if (movableUnit.IsShip())
            {
                movableUnit.shipData.SetDockedMode(false);
            }

            // TODO fix attack move
            //movableUnit.SetAIModule(IsAttackMove ? UnitAIModule.AIModule.AttackMoveAIModule : UnitAIModule.AIModule.BasicMovementAIModule,
            //    position, newCrowdID, offset, startPosition);
            //movableUnit.SetAIModule(UnitAIModule.AIModule.BasicMovementAIModule, position, newCrowdID);
            if (IsAttackMove)
            {
                movableUnit.aiController.SetAI(new AttackMoveAI(movableUnit.aiController, position, newCrowdID), false, true);
            }
            else
            {
                movableUnit.aiController.SetAI(new MoveToPositionAI(movableUnit.aiController, position, newCrowdID), false, true);
            }
        }
    }

    public void MoveUnitsToTargetInFormation(Vector3 destination, List<Unit> units)
    {
        List<List<Unit>> clusters = ClusterUnits(units, 5f); // 5 units apart same formation

        foreach (var cluster in clusters)
        {
            ulong newCrowdID = ++UnitManager.crowdIDCounter;
            if (cluster.Count == 1)
            {
                // Move alone
                MoveToCommand(cluster[0], destination, newCrowdID);
            }
            else
            {
                // Formation move
                ArrangeUnits_New(cluster, destination, newCrowdID);
                //ArrangeClusterFormation(destination, cluster);
            }
        }
    }
    public void Execute()
    {
        List<Unit> units = new List<Unit>();
        if (IsAttackMove)
        {
            //Debug.Log($"Doing Attack Move");
        }

        int unitCount = unitIDs.Count;
        for (int i = 0; i < unitCount; i++)
        {
            Unit unit = UnitManager.Instance.GetUnit(unitIDs[i]);
            if (unit && unit.GetType() == typeof(MovableUnit))
            {
                units.Add(unit);
            }
        }
        MoveUnitsToTargetInFormation(position, units);

        return;
    }
}

public class DockShipUnitCommand : InputCommand
{
    public const string commandName = "Dock Ship Unit Command";
    public List<ulong> unitIDs;
    public Vector3 position;

    public void ArrangeUnits_New(List<Unit> units, Vector3 position, ulong crowdID)
    {
        if (units == null || units.Count == 0) return;

        ulong newCrowdID = ++UnitManager.crowdIDCounter;

        // 1. Build formation positions
        const float unitWidth = 0.14f;
        const float interUnitSpacing = 0.14f;
        float spacing = unitWidth + interUnitSpacing;
        int totalUnits = units.Count;
        int columns = Mathf.CeilToInt(Mathf.Sqrt(totalUnits));
        List<Vector3> formationOffsets = new List<Vector3>();

        Vector3 center = Vector3.zero;
        for (int i = 0; i < totalUnits; i++)
        {
            float row = Mathf.Floor(i / (float)columns);
            float col = i % columns;

            float unitsInRow = Mathf.Min(columns, totalUnits - row * columns);
            float totalRowWidth = (unitsInRow - 1) * spacing;
            float offsetX = -totalRowWidth / 2f;

            float x = col * spacing + offsetX;
            float z = row * spacing;

            formationOffsets.Add(new Vector3(x, 0, -z)); // negative z for forward
            center += units[i].transform.position;
        }

        center /= totalUnits;

        // 2. Build cost matrix
        float[,] costMatrix = new float[totalUnits, totalUnits];
        for (int i = 0; i < totalUnits; i++)
        {
            Vector3 unitPos = units[i].transform.position;
            for (int j = 0; j < totalUnits; j++)
            {
                Vector3 targetPos = position + formationOffsets[j];
                costMatrix[i, j] = Vector3.SqrMagnitude(unitPos - targetPos); // Use squared for performance
            }
        }

        // 3. Solve assignment
        List<int> assignment = HungarianAlgorithm.Solve(costMatrix).ToList();
        assignment.Reverse();

        // 4. Issue move commands
        Vector3 startPathfindingPostion = units[0].transform.position;
        for (int i = 0; i < totalUnits; i++)
        {
            Unit unit = units[i];
            MovableUnit movableUnit = (MovableUnit)unit;
            if (StatComponent.IsUnitAliveOrValid(movableUnit))
            {
                int assignedIndex = assignment[i];
                Vector3 offset = formationOffsets[assignedIndex];
                MoveToCommand(unit, position, newCrowdID, offset, center);
            }
        }
    }

    public void MoveUnitsToTargetInFormation(Vector3 destination, List<Unit> units)
    {
        List<List<Unit>> clusters = MoveUnitsCommand.ClusterUnits(units, 5f); // 5 units apart same formation

        foreach (var cluster in clusters)
        {
            ulong newCrowdID = ++UnitManager.crowdIDCounter;
            if (cluster.Count == 1)
            {
                // Move alone
                MoveToCommand(cluster[0], destination, newCrowdID);
            }
            else
            {
                // Formation move
                ArrangeUnits_New(cluster, destination, newCrowdID);
                //ArrangeClusterFormation(destination, cluster);
            }
        }
    }

    void MoveToCommand(Unit unit, Vector3 position, ulong newCrowdID, Vector3 offset = default, Vector3? startPosition = null)
    {
        MovableUnit movableUnit = (MovableUnit)unit;
        if (StatComponent.IsUnitAliveOrValid(movableUnit))
        {
            if (!movableUnit.IsShip()) return;
            movableUnit.ResetUnit(true);
            if (startPosition == null)
            {
                startPosition = unit.transform.position;
            }
            Vector3 diff = startPosition.Value - position;
            float sqrMagnitude = diff.sqrMagnitude;
            const float distanceForFastRearrange = 5;
            const float distanceForFastRearrangeSqr = distanceForFastRearrange * distanceForFastRearrange;
            if (sqrMagnitude < distanceForFastRearrangeSqr)
            {
                startPosition = unit.transform.position;
            }

            movableUnit.aiController.SetAI(new BoardToShoreAI(movableUnit.aiController, position, newCrowdID), false, true);
        }
    }

    public void Execute()
    {
        List<Unit> units = new List<Unit>();
        int unitCount = unitIDs.Count;
        for (int i = 0; i < unitCount; i++)
        {
            Unit unit = UnitManager.Instance.GetUnit(unitIDs[i]);
            if (unit && unit.GetType() == typeof(MovableUnit))
            {
                units.Add(unit);
            }
        }
        MoveUnitsToTargetInFormation(position, units);
    }
}

public class PauseGameCommand : InputCommand
{
    public const string commandName = "Pause Game Command";

    public void Execute()
    {
        if (!DeterministicUpdateManager.Instance.IsPaused())
        {
            DeterministicUpdateManager.Instance.Pause();
        }
    }
}

public class ResumeGameCommand : InputCommand
{
    public const string commandName = "Resume Game Command";

    public void Execute()
    {
        if (DeterministicUpdateManager.Instance.IsPaused())
        {
            DeterministicUpdateManager.Instance.Resume();
        }
    }
}

public class DeleteUnitsCommand : InputCommand
{
    public const string commandName = "Delete Units Command";
    public List<ulong> unitIDs;

    public void Execute()
    {
        foreach (ulong unitID in unitIDs)
        {
            Unit unit = UnitManager.Instance.GetUnit(unitID);
            MovableUnit movableUnit = (MovableUnit)unit;
            if (movableUnit && StatComponent.IsUnitAliveOrValid(movableUnit))
            {
                StatComponent.KillUnit(movableUnit);
            }
        }
    }
}

public class StopUnits : InputCommand
{
    public const string commandName = "Stop Units";
    public List<ulong> unitIDs;

    public void Execute()
    {
        foreach (ulong unitID in unitIDs) {
            Unit unit = UnitManager.Instance.GetUnit(unitID);
            if (unit)
            {
                if (unit.GetType() == typeof(MovableUnit))
                {
                    MovableUnit movableUnit = unit as MovableUnit;
                    if (StatComponent.IsUnitAliveOrValid(movableUnit))
                    {
                        movableUnit.movementComponent.Stop();
                    }
                }
                // shipUnit.StartPathfind(position);
            }
        }
    }
}

public class BoardToShipCommand : InputCommand
{
    public const string commandName = "Board To Ship Command";
    public List<ulong> unitIDs = new List<ulong>();
    public ulong targetID = 0;

    static bool IsValidShip(ulong id, out MovableUnit targetShip)
    {
        targetShip = null;
        Unit targetUnit = UnitManager.Instance.GetUnit(id);
        if (targetUnit.GetType() != typeof(MovableUnit)) { return false; }
        targetShip = targetUnit as MovableUnit;
        if (!StatComponent.IsUnitAliveOrValid(targetShip)) { return false; }
        if (!targetShip.IsShip()) return false;
        return true;
    }

    public void Execute()
    {
        if (unitIDs.Contains(targetID)) return;

        if (IsValidShip(targetID, out MovableUnit targetShip))
        {
            for (int i = 0; i < unitIDs.Count; i++)
            {
                ulong id = unitIDs[i];
                if (IsValidShip(id, out MovableUnit movableUnit))
                {
                    movableUnit.ResetUnit(true);
                    movableUnit.shipData.SetDockedMode(false);
                    ulong newCrowdID = ++UnitManager.crowdIDCounter;
                    movableUnit.aiController.SetAI(new BoardToShipAI(movableUnit.aiController, targetShip), false, true);
                }
            }
        }

    }
}

public class AttackUnitCommand : InputCommand
{
    public const string commandName = "Attack Unit Command";
    public ulong unitID;
    public ulong targetID;

    public void Execute()
    {
        if (unitID == targetID) return;
        Unit unit = UnitManager.Instance.GetUnit(unitID);
        Unit targetUnit = UnitManager.Instance.GetUnit(targetID);
        if (unit && targetUnit)
        {
            MovableUnit movableUnit = (MovableUnit)unit;
            if (StatComponent.IsUnitAliveOrValid(movableUnit))
            {
                MovableUnit movableTargetUnit = (MovableUnit)targetUnit;
                if (movableUnit.aiModule)
                {
                    ulong newCrowdID = ++UnitManager.crowdIDCounter;
                    movableUnit.aiController.SetAI(new AttackAI(movableUnit.aiController, movableTargetUnit, true), false, true);
                }
            }
        }
    }
}

public interface INetworkAdapter
{
    void SendCommand(InputCommand command);
    event Action<InputCommand> OnCommandReceived;
    void UpdateAdapter();
    int GetDelay();
}

public class InputManager : MonoBehaviour, IDeterministicUpdate
{
    public static InputManager Instance;
    public NetworkAdapter networkAdapter;
    private Dictionary<ulong, List<InputCommand>> queuedCommands = new Dictionary<ulong, List<InputCommand>>();
    ulong lastReceivedTick = 0;

    void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        //DeterministicUpdateManager.Instance.Register(this);
        networkAdapter.OnCommandReceived += QueueInput;
    }

    private void OnDisable()
    {
        //DeterministicUpdateManager.Instance.Unregister(this);
        networkAdapter.OnCommandReceived -= QueueInput;
    }
    
    /// <summary>
     /// Call this every frame, even when paused.
     /// </summary>
    public void NetworkTick()
    {
        networkAdapter.UpdateAdapter();
        // you could also send KeepAlive here if you like
    }

    //public void SendInputCommand(InputCommand command)
    //{
    //    command.frame = DeterministicUpdateManager.Instance.tickCount + (ulong)networkAdapter.GetDelay();
    //    //QueueInput(command);
    //    networkAdapter.SendCommand(command);
    //}

    public void QueueInput(InputCommand command)
    {
        if (!queuedCommands.ContainsKey(command.frame))
            queuedCommands[command.frame] = new List<InputCommand>();

        lastReceivedTick = command.frame;

        //Debug.Log($"Received and added new command for {command.frame}");
        queuedCommands[command.frame].Add(command); 
        
        if (DeterministicUpdateManager.Instance.IsPaused())
        {
            // Simulate minimal ticking just to process input
            // DeterministicUpdate(DeterministicUpdateManager.FixedStep, DeterministicUpdateManager.Instance.tickCount);
            DeterministicUpdateManager.Instance.Resume();
        }
    }

    private void ProcessCommandsForFrame(ulong frame)
    {
        ulong timeoutTicks = (ulong)networkAdapter.GetDelay();
        //NativeLogger.Log($"timeout delay: {timeoutTicks}, frame id: {frame} and last received tick: {lastReceivedTick}");
        if (!queuedCommands.TryGetValue(frame, out var commands))
        {
            // compute signed difference
            long diff = (long)frame - (long)lastReceivedTick;

            // only pause if lastReceivedTick was truly older and beyond timeout
            if (diff > (long)timeoutTicks)
            {
                DeterministicUpdateManager.Instance.Pause();
            }
            return;
        }

        foreach (var cmd in commands)
            ExecuteCommand(cmd);
        queuedCommands.Remove(frame);
    }

    private void ExecuteCommand(InputCommand command)
    {
        if (command.action != "KeepAlive")
            NativeLogger.Log($"Executing {command.action} at Frame {command.frame}");
        switch (command.action)
        {
            case MoveUnitCommand.commandName:
                {
                    MoveUnitCommand moveUnitCommand = command as MoveUnitCommand;
                    moveUnitCommand.Execute();
                }
                break;
            case DockShipUnitCommand.commandName:
                {
                    DockShipUnitCommand moveShipUnitCommand = command as DockShipUnitCommand;
                    moveShipUnitCommand.Execute();
                }
                break;
            case StopUnits.commandName:
                {
                    StopUnits moveShipToDockCommand = command as StopUnits;
                    moveShipToDockCommand.Execute();
                }
                break;
            case MoveUnitsCommand.commandName:
                {
                    MoveUnitsCommand moveUnitsCommand = command as MoveUnitsCommand;
                    moveUnitsCommand.Execute();
                }
                break;
            case AttackUnitCommand.commandName:
                {
                    AttackUnitCommand attackUnitCommand = command as AttackUnitCommand;
                    attackUnitCommand.Execute();
                }
                break;
            case BoardToShipCommand.commandName:
                {
                    BoardToShipCommand dockToShipCommand = command as BoardToShipCommand;
                    dockToShipCommand.Execute();
                }
                break;
            case DeleteUnitsCommand.commandName:
                {
                    DeleteUnitsCommand deleteUnitsCommand = command as DeleteUnitsCommand;
                    deleteUnitsCommand.Execute();
                }
                break;
            case PauseGameCommand.commandName:
                {
                    PauseGameCommand pauseGameCommand = command as PauseGameCommand;
                    pauseGameCommand.Execute();
                }
                break;
            case ResumeGameCommand.commandName:
                {
                    ResumeGameCommand resumeGameCommand = command as ResumeGameCommand;
                    resumeGameCommand.Execute();
                }
                break;
        }
    }

    public void SendInputCommand(InputCommand command)
    {
        if (command.action != "KeepAlive")
            NativeLogger.Log($"Command type recieved was: {command.action}");

        if (command.action == ResumeGameCommand.commandName)
        {
            // execute on the same frame
            command.frame = DeterministicUpdateManager.Instance.tickCount;
        }
        else
        {
            // normal gameplay commands still use delayed scheduling
            command.frame = DeterministicUpdateManager.Instance.tickCount
                          + (ulong)networkAdapter.GetDelay();
        }

        networkAdapter.SendCommand(command);
    }

    public void DeterministicUpdate(float deltaTime, ulong tickID)
    {
        ProcessCommandsForFrame(tickID);

        // KeepAlive so you don�t time out while paused
        var keepAlive = new InputCommand { playerID = -1, action = "KeepAlive" };
        SendInputCommand(keepAlive);
    }
}