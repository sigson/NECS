using Assets.ClientCore.CoreImpl.Network.NetworkEvents.GameData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NECS.Core.Logging;

namespace NECS.ECS.ECSCore
{
    [TypeUid(4)]
    public abstract class ECSEvent : IECSObject
    {
        static public long Id { get; set; }
        public long EntityOwnerId;
        [NonSerialized]
        public EventWatcher eventWatcher;
        public GameDataEvent? cachedGameDataEvent = null;
        public object? cachedRawEvent = null;
        public abstract void Execute();

        public string Serialization()
        {
            using (StringWriter writer = new StringWriter())
            {
                GlobalCachingSerialization.cachingSerializer.Serialize(writer, this);
                return writer.ToString();
            }
        }

        public virtual GameDataEvent PackToNetworkPacket()
        {
            return new GameDataEvent()
            {
                jsonData = this.Serialization(),
                typeId = this.GetId(),
                packetId = (int)Guid.NewGuid().GuidToLong()
            };
        }

        public virtual GameDataEvent CachePackToNetworkPacket()
        {
            if(cachedGameDataEvent == null)
            {
                cachedGameDataEvent = new GameDataEvent()
                {
                    jsonData = this.Serialization(),
                    typeId = this.GetId(),
                    packetId = (int)Guid.NewGuid().GuidToLong()
                };
            }
            return (GameDataEvent)cachedGameDataEvent;
        }

        public GameDataEvent GetGameDataEvent()
        {
            if (cachedGameDataEvent == null)
                return PackToNetworkPacket();
            else
                return (GameDataEvent)cachedGameDataEvent;
        }

        
    }
}
