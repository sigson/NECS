using System;
using System.Text;
using System.Collections.Generic;
using NECS.Core.Logging;
using NECS.Harness.Services;

namespace NECS.Network.Simple.Net {
    public class NetBuffer : IDisposable {
        public List<byte> buffer { get; set; } = new List<byte>();
        public long hashCode {get; protected set; }
        public long id {get; set; }
        public long length {get; set; }
        public long packetPosition {get; set; }
        public byte[] ToByteArray() {
            List<byte> result = new List<byte>();
            long headerSize = 8L * 4L;
            if (NetworkingService.instance.bufferSize >= buffer.Count + headerSize)
            {
                result.AddRange(BitConverter.GetBytes(hashCode));
                result.AddRange(BitConverter.GetBytes(Guid.NewGuid().GuidToLong()));
                result.AddRange(BitConverter.GetBytes((long)buffer.Count + headerSize));
                result.AddRange(BitConverter.GetBytes((long)0));
                result.AddRange(buffer);
            }
            else
            {
                List<byte> newBuffer = new List<byte>(buffer);
                int position = 0;
                int packetPosition = 0;
                long guid = Guid.NewGuid().GuidToLong();
                long countOfHeaders = 0;
                var bufferSize = buffer.Count;
                while (bufferSize + headerSize >= NetworkingService.instance.bufferSize)
                {
                    countOfHeaders += headerSize;
                    bufferSize -= NetworkingService.instance.bufferSize - (int)headerSize;
                }
                if(bufferSize > 0)
                    countOfHeaders += headerSize;
                while (NetworkingService.instance.bufferSize < (newBuffer.Count + countOfHeaders) - (position + packetPosition * headerSize))
                {
                    result.AddRange(BitConverter.GetBytes(hashCode));
                    result.AddRange(BitConverter.GetBytes(guid));
                    result.AddRange(BitConverter.GetBytes((long)buffer.Count + countOfHeaders));
                    result.AddRange(BitConverter.GetBytes((long)packetPosition));
                    result.AddRange(buffer.GetRange(position, NetworkingService.instance.bufferSize - (int)headerSize));
                    position += NetworkingService.instance.bufferSize - (int)headerSize;
                    packetPosition++;
                }
                result.AddRange(BitConverter.GetBytes(hashCode));
                result.AddRange(BitConverter.GetBytes(guid));
                result.AddRange(BitConverter.GetBytes((long)buffer.Count + countOfHeaders));
                result.AddRange(BitConverter.GetBytes((long)packetPosition));
                result.AddRange(buffer.GetRange(position, newBuffer.Count - position));
                position += NetworkingService.instance.bufferSize - (int)headerSize;
                packetPosition++;

            }
            //Logger.Log(result.Count);
            return result.ToArray();
        }
        
        public override string ToString() {
            var sb = new StringBuilder("byte[] { ");
            foreach (var b in ToByteArray())
                sb.Append(b + ", ");
            sb.Append("}");
            return sb.ToString();
        }

        public void Dispose()
            => buffer = null;
    }
}