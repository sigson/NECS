﻿using NECS.ECS.ECSCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.ECS.DefaultObjects.Events.LowLevelNetEvent
{
    [LowLevelNetworkEvent]
    [NetworkScore(0)]
    [System.Serializable]
    [TypeUid(18)]
    public class SetEntityIdEvent : ECSEvent
    {
        static public new long Id { get; set; } = 18;
        public override void Execute()
        {

        }
    }
}
