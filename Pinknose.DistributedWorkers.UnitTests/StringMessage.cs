using Pinknose.DistributedWorkers.Messages;
using System;

[Serializable]
public class StringMessage : PayloadMessage<string>
{
    
    public StringMessage(string payload) : base(payload, false, false, false)
    {
    }

   // public override Guid MessageTypeGuid => new Guid("2DCC5ABE-7725-4DA7-89AA-4706A0703D43");

}