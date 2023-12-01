using NECS.ECS.ECSCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.ECS.DefaultObjects.Events.ECSEvents
{
    [NetworkScore(0)]
    [System.Serializable]
    [TypeUid(17)]
    public class ClientDisconnectedEvent : ECSEvent
    {
        static public new long Id { get; set; } = 17;
        public override void Execute()
        {
            
        }
    }
}
