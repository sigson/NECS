﻿using NECS.ECS.ECSCore;
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
        [System.NonSerialized]
        [Newtonsoft.Json.JsonIgnore]
        public ECSEntity userEntity;
        public long userEntityId;
        public string Username;
        public bool userRelogin = false;
        [System.NonSerialized]
        [Newtonsoft.Json.JsonIgnore]
        public static Action<UserLoggedEvent> actionAfterLoggin = (loggedEvent) => { };
        public override void Execute()
        {
            actionAfterLoggin(this);
        }
    }
}
