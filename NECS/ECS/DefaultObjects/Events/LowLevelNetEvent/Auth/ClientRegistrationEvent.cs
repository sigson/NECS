using NECS.ECS.ECSCore;
using NECS.Harness.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.ECS.DefaultObjects.Events.LowLevelNetEvent.Auth
{
    [NetworkScore(400)]
    [System.Serializable]
    [TypeUid(22)]
    public class ClientRegistrationEvent : ECSEvent
    {
        public string Username = "";
        public string Password = "";
        public string Email = "";
        public string HardwareId = "";
        public string CaptchaResultHash = "";
        static public new long Id { get; set; } = 22;
        public override void Execute()
        {
            if (GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Server)
                AuthService.instance.RegistrationProcess(this);
        }

        public override bool CheckPacket()
        {
            if(!Username.Any(p => !char.IsLetterOrDigit(p)))
            {
                return false;
            }
            if (!Email.Any(p => !char.IsLetterOrDigit(p) || p == '@' || p == '.' || p == '_' || p == '-'))
            {
                return false;
            }
            if(Username.Length > 32 || Password.Length > 32 || Email.Length > 32)
                return false;
            return true;
        }
    }
}
