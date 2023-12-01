using NECS.ECS.ECSCore;
using NECS.Harness.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.ECS.DefaultObjects.Events.LowLevelNetEvent.ConfigEvent
{
    [NetworkScore(0)]
    [System.Serializable]
    [TypeUid(20)]
    public class ConfigCheckResultEvent : ECSEvent
    {
        public byte[] NewConfig = new byte[0];
        static public new long Id { get; set; } = 20;
        public override void Execute()
        {
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Server)
            {

            }
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client)
            {

            }
        }
    }
}
