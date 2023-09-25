using System;

namespace NECS.Network.Simple.Net {
    public interface INetSerializable {
        void Serialize(NetWriterBase buffer);
        void Deserialize(NetReaderBase buffer);
    }
}