using NECS.ECS.ECSCore;
using NECS.Harness.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.ECS.DefaultObjects.Events.LowLevelNetEvent.ConfigEvent
{
    [LowLevelNetworkEvent]
    [NetworkScore(0)]
    [System.Serializable]
    [TypeUid(20)]
    public class ConfigCheckResultEvent : ECSEvent
    {
        public byte[] NewConfig = null;
        public long configHash;
        static public new long Id { get; set; } = 20;
        public override void Execute()
        {
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Server)
            {

            }
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client)
            {
                ConstantService.instance.loadedConfigFile = NewConfig == null ? new List<byte>() : this.NewConfig.ToList();
                ConstantService.instance.checkedConfigVersion = this.configHash;
                ConstantService.instance.SetupConfigs();
                ConstantService.instance.UnfreezeConstantService();
            }
        }
    }
}
