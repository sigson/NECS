using NECS.ECS.ECSCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.ECS.DefaultObjects.Events.ECSEvents
{
    [NetworkScore(0)]
    [System.Serializable]
    [TypeUid(25)]
    public class UserLoggedEvent : ECSEvent
    {
        static public new long Id { get; set; } = 25;
        public ECSEntity userEntity;
        public bool userRelogin = false;
        [System.NonSerialized]
        [Newtonsoft.Json.JsonIgnore]
        public Action<ECSEntity> actionAfterLoggin = (entity) => { };
        public override void Execute()
        {

        }
    }
}
