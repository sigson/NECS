using NECS.ECS.ECSCore;
using NECS.Harness.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.ECS.DefaultObjects.Events.LowLevelNetEvent.ConfigEvent
{
    [NetworkScore(100)]
    [System.Serializable]
    [TypeUid(19)]
    public class ConfigCheckEvent : ECSEvent
    {
        static public new long Id { get; set; } = 19;
        public long configHash;
        public override void Execute()
        {
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Server)
            {
                byte[] newconfig = null;
                if(configHash != ConstantService.instance.hashConfigFilesZip)
                {
                    newconfig = ConstantService.instance.ConfigFilesZip.ToArray();
                }
                NetworkingService.instance.Send(this.SocketSource, new ConfigCheckResultEvent()
                {
                    NewConfig = newconfig,
                    configHash = ConstantService.instance.hashConfigFilesZip
                }.GetNetworkPacket());
            }
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client)
            {
                NetworkingService.instance.Send(NetworkingService.instance.ClientSocket, this.GetNetworkPacket());
            }
        }
    }
}
