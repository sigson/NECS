using NECS.ECS.ECSCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.ECS.DefaultObjects.Events.LowLevelNetEvent.ConfigEvent
{
    [NetworkScore(100)]
    [Serializable]
    [TypeUid(19)]
    public class ConfigCheckEvent : ECSEvent
    {
        static public new long Id { get; set; } = 19;
        public override void Execute()
        {

        }
    }
}
