using NECS.Network.NetworkModels;
using System.Text.Json.Serialization;

namespace NECS.ECS.ECSCore
{
    [NetworkScore(0)]
    [Serializable]
    [TypeUid(4)]
    public abstract class ECSEvent : IECSObject
    {
        static new public long Id { get; set; } = 4;
        public long EntityOwnerId;
        [NonSerialized]
        [JsonIgnore]
        public long SocketSourceId = 0;
        [NonSerialized]
        [JsonIgnore]
        public EventWatcher eventWatcher;
        [NonSerialized]
        [JsonIgnore]
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
