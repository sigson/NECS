using NECS.ECS.DefaultObjects.Events.ECSEvents;
using NECS.ECS.ECSCore;
using NECS.Harness.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.ECS.DefaultObjects.Events.LowLevelNetEvent.Auth
{
    [NetworkScore(0)]
    [System.Serializable]
    [TypeUid(26)]
    public class AuthActionFailedEvent : ECSEvent
    {
        public int EventId = 0;
        public string Reason = "";
        [System.NonSerialized]
        [Newtonsoft.Json.JsonIgnore]
        public static Action<AuthActionFailedEvent> action = (errorEvent) => { };
        static public new long Id { get; set; } = 26;
        public override void Execute()
        {
            if(GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Client)
            {
                action(this);
            }
        }
    }
}
