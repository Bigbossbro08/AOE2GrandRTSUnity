using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InputCommand
{
    public ulong frame;      // The frame when this input should execute
    public int playerID;   // Who sent this input
    public string action;  // The type of action (e.g., "Move", "Attack")
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
        NativeLogger.Log($"timeout delay: {timeoutTicks}, frame id: {frame} and last received tick: {lastReceivedTick}");
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
            case MoveShipUnitCommand.commandName:
                {
                    MoveShipUnitCommand moveShipUnitCommand = command as MoveShipUnitCommand;
                    moveShipUnitCommand.Execute();
                }
                break;
            case MoveShipToDockCommand.commandName:
                {
                    MoveShipToDockCommand moveShipToDockCommand = command as MoveShipToDockCommand;
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

        // KeepAlive so you don’t time out while paused
        var keepAlive = new InputCommand { playerID = -1, action = "KeepAlive" };
        SendInputCommand(keepAlive);
    }
}