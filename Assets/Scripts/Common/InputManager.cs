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

    private void Update()
    {
        networkAdapter.UpdateAdapter();
    }

    public void SendInputCommand(InputCommand command)
    {
        command.frame = DeterministicUpdateManager.Instance.tickCount + (ulong)networkAdapter.GetDelay();
        //QueueInput(command);
        networkAdapter.SendCommand(command);
    }

    public void QueueInput(InputCommand command)
    {
        if (!queuedCommands.ContainsKey(command.frame))
            queuedCommands[command.frame] = new List<InputCommand>();

        lastReceivedTick = command.frame;

        //Debug.Log($"Received and added new command for {command.frame}");
        queuedCommands[command.frame].Add(command); 
        
        if (DeterministicUpdateManager.Instance.IsPaused()) // Check if paused
        {
            DeterministicUpdateManager.Instance.Resume(); // Re-enable ticking
        }
    }

    private void ProcessCommandsForFrame(ulong frame)
    {
        ulong timeoutTicks = (ulong)networkAdapter.GetDelay();
        //Debug.LogWarning($"timeout delay: {timeoutTicks}, frame id: {frame} and last received tick: {lastReceivedTick}");
        if (!queuedCommands.TryGetValue(frame, out var commands))
        {
            if (frame - lastReceivedTick > timeoutTicks)
            {
                DeterministicUpdateManager.Instance.Pause(); // Disable ticking
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
        }
    }

    public void DeterministicUpdate(float deltaTime, ulong tickID)
    {
        ProcessCommandsForFrame(tickID);

        InputCommand keepAlive = new InputCommand
        {
            playerID = -1, // System command
            action = "KeepAlive"
        };
        SendInputCommand(keepAlive);
    }
}