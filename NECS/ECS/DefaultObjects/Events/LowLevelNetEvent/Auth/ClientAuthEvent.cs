using NECS.ECS.ECSCore;
using NECS.Harness.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.ECS.DefaultObjects.Events.LowLevelNetEvent.Auth
{
    [NetworkScore(40)]
    [System.Serializable]
    [TypeUid(21)]
    public class ClientAuthEvent : ECSEvent
    {
        public string Username = "";
        public string Password = "";
        static public new long Id { get; set; } = 21;
        public override void Execute()
        {
            AuthService.instance.AuthProcess(this);
        }

        public override bool CheckPacket()
        {
            if (!Username.Any(p => !char.IsLetterOrDigit(p)))
            {
                return false;
            }
            if (Username.Length > 32 || Password.Length > 32)
                return false;
            return true;
        }
    }
}
