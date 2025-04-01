using System.Collections.Generic;
using System;
using UnityEngine;

public class SinglePlayerAdapter : NetworkAdapter
{
    public int delay = 1;

    private Queue<InputCommand> localQueue = new Queue<InputCommand>();

    public override int GetDelay()
    {
        return delay;
    }

    public override void SendCommand(InputCommand command)
    {
        // In singleplayer, execute immediately (or simulate delay)
        localQueue.Enqueue(command);
        OnCommandReceived?.Invoke(localQueue.Dequeue());

        //if (command.action == "KeepAlive")
        //    Debug.Log("Added and sent command");
    }

    public override void UpdateAdapter()
    {

    }
}