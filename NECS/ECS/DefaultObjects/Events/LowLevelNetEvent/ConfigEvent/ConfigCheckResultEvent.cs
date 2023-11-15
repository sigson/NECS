using NECS.ECS.ECSCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.ECS.DefaultObjects.Events.LowLevelNetEvent.ConfigEvent
{
    [NetworkScore(0)]
    [Serializable]
    [TypeUid(20)]
    public class ConfigCheckResultEvent : ECSEvent
    {
        static public new long Id { get; set; } = 20;
        public override void Execute()
        {

        }
    }
}
