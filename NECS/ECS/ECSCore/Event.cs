using Newtonsoft.Json;
using System.Collections.Concurrent;
using NECS.Extensions;
using NECS.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using NECS.Harness.Services;
using NECS.Network.NetworkModels;


namespace NECS.ECS.ECSCore
{
    [NetworkScore(0)]
    [System.Serializable]
    [TypeUid(4)]
    public abstract class ECSEvent : IECSObject
    {
        static new public long Id { get; set; } = 4;
        public long EntityOwnerId;
        [Newtonsoft.Json.JsonIgnore]
        public long SocketSourceId {
            get{
                if (this.SocketSource == null)
                {
                    return 0;
                }
                return this.SocketSource.Id;
            }
        }
        [System.NonSerialized]
        [Newtonsoft.Json.JsonIgnore]
        public SocketAdapter SocketSource;
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
            if(Defines.ECSNetworkTypeLogging)
            {
                NLogger.Log($"Prepared to send {this.GetType().Name}");
            }
            if (cachedGameDataEvent == null)
                SerializeEvent();
            return cachedGameDataEvent;
        }
    }
}
