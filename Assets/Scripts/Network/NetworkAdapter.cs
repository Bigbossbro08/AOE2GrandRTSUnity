using System;
using UnityEngine;

public class NetworkAdapter : MonoBehaviour
{
    public Action<InputCommand> OnCommandReceived;

    public virtual int GetDelay()
    {
        //throw new NotImplementedException();
        return 1;
    }

    public virtual void SendCommand(InputCommand command)
    {
        //throw new NotImplementedException();
    }

    public virtual void UpdateAdapter()
    {
        //throw new NotImplementedException();
    }
}
