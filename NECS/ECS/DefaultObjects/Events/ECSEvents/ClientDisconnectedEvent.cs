using NECS.ECS.ECSCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.ECS.DefaultObjects.Events.ECSEvents
{
    [NetworkScore(0)]
    [Serializable]
    [TypeUidAttribute(16)]
    public class ClientDisconnectedEvent : ECSEvent
    {
        static public new long Id { get; set; } = 16;
        public override void Execute()
        {
            
        }
    }
}
