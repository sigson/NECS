using NECS.ECS.ECSCore;
using NECS.Harness.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.ECS.DefaultObjects.Events.LowLevelNetEvent
{
    [NetworkScore(0)]
    [System.Serializable]
    [TypeUid(9)]
    public class CommunicationStartEvent : ECSEvent
    {
        static public new long Id { get; set; } = 9;
        public static Action OnCommunicationStart = () => { };
        public int CommunicationCounter = 0;
        public override void Execute()
        {
            if(GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Server)
            {
                CommunicationCounter++;
                this.SocketSource.Send(this.GetNetworkPacket());
            }
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client)
            {
                if(CommunicationCounter > 0)
                {
                    OnCommunicationStart();
                }
            }
        }
    }
}
