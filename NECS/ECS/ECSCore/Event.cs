using NECS.Network.NetworkModels;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using NECS.Extensions;
using NECS.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;


namespace NECS.ECS.ECSCore
{
    [NetworkScore(0)]
    [System.Serializable]
    [TypeUid(4)]
    public abstract class ECSEvent : IECSObject
    {
        static new public long Id { get; set; } = 4;
        public long EntityOwnerId;
        [System.NonSerialized]
        [Newtonsoft.Json.JsonIgnore]
        public long SocketSourceId = 0;
        [System.NonSerialized]
        [Newtonsoft.Json.JsonIgnore]
        public EventWatcher eventWatcher;
        [System.NonSerialized]
        [Newtonsoft.Json.JsonIgnore]
        public byte[] cachedGameDataEvent = null;
        public abstract void Execute();

        public virtual bool CheckPacket()
        {
            return true;
        }

        /// <summary>
        /// example if in chat message has 200+ symbols - it add score to packet
        /// </summary>
        /// <returns></returns>
        public virtual int NetworkScoreBooster()
        {
            return 0;
        }

        protected virtual void SerializeEvent()
        {
            using (MemoryStream writer = new MemoryStream())
            {
                NetSerializer.Serializer.Default.Serialize(writer, this);
                cachedGameDataEvent = NetworkPacketBuilderService.instance.SliceAndRepackForSendNetworkPacket(writer.ToArray());
            }
        }

        public virtual byte[] GetNetworkPacket()
        {
            if (cachedGameDataEvent == null)
                SerializeEvent();
            return cachedGameDataEvent;
        }
    }
}
