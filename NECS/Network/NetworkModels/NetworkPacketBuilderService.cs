using NECS.Harness.Model;
using NECS.Harness.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core;

namespace NECS.Network.NetworkModels
{
    public class NetworkPacketBuilderService : IService
    {
        private static NetworkPacketBuilderService cacheInstance;
        public static NetworkPacketBuilderService instance
        {
            get
            {
                if (cacheInstance == null)
                    cacheInstance = SGT.Get<NetworkPacketBuilderService>();
                return cacheInstance;
            }
        }

        public ConcurrentDictionary<long, ConcurrentDictionary<long, byte[]>> SlicedReceivedStorage = new ConcurrentDictionary<long, ConcurrentDictionary<long, byte[]>>();
        public override void InitializeProcess()
        {
        }

        public (byte[], bool) UnpackNetworkPacket(byte[] packetBuffer)
        {
            var networkBufferSize = NetworkingService.instance.BufferSize;
            int headerSize = 8 * 3;
            //for (int i = 0; i < packetBuffer.Length / networkBufferSize; i++)
            //{

            //}
            var resultBuffer = new List<byte>();

            int readPos = 0;
            long guid = BitConverter.ToInt64(packetBuffer, readPos);
            readPos += 8;
            long packetSize = BitConverter.ToInt64(packetBuffer, readPos);
            readPos += 8;
            long packetNumber = BitConverter.ToInt64(packetBuffer, readPos);
            //readPos += 8;

            if (packetSize > networkBufferSize)
            {
                //lock(receiveLocker)
                {
                    ConcurrentDictionary<long, byte[]> bufPackets;
                    var getResult = SlicedReceivedStorage.TryGetValue(guid, out bufPackets);
                    if (getResult && packetSize - bufPackets.Count * networkBufferSize <= networkBufferSize)
                    {
                        bufPackets.TryAdd(packetNumber, packetBuffer);
                        
                        for (int i = 0; i < bufPackets.Count; i++)
                        {
                            //if (i == 0)
                            //    resultBuffer.AddRange(bufPackets[i]);
                            //else
                            //{
                                
                            //}
                            resultBuffer.AddRange(bufPackets[i].SubArray(headerSize, bufPackets[i].Length - headerSize));
                        }
                        SlicedReceivedStorage.TryRemove(guid, out _);
                    }
                    else
                    {
                        if (getResult)
                        {
                            bufPackets.TryAdd(packetNumber, packetBuffer);
                        }
                        else
                        {
                            bufPackets = new ConcurrentDictionary<long, byte[]>();
                            SlicedReceivedStorage.TryAdd(guid, bufPackets);
                            bufPackets.TryAdd(packetNumber, packetBuffer);
                        }
                    }
                    //if (SlicedReceivedStorage.TryGetValue(guid, out _))
                    //{
                    //    //try { socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, OnReceive, socket); }
                    //    //catch { return; }
                    //    return;
                    //}
                }
            }
            else
            {
                resultBuffer.AddRange(packetBuffer.SubArray(headerSize, packetBuffer.Length - headerSize));
            }
            if(resultBuffer.Count == 0)
            {
                return (resultBuffer.ToArray(), false);
            }
            else
            {
                return (resultBuffer.ToArray(), true);
            }
        }

        public byte[] SliceAndRepackForSendNetworkPacket(byte[] packetBuffer)
        {
            List<byte> buffer = new List<byte>(packetBuffer);
            List<byte> result = new List<byte>();
            long headerSize = 8L * 3L;
            var hashCode = Guid.NewGuid().GuidToLong();
            var networkBufferSize = NetworkingService.instance.BufferSize;

            if (networkBufferSize >= buffer.Count + headerSize)
            {
                //result.AddRange(BitConverter.GetBytes(hashCode));
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
                while (bufferSize + headerSize >= networkBufferSize)
                {
                    countOfHeaders += headerSize;
                    bufferSize -= networkBufferSize - (int)headerSize;
                }
                if (bufferSize > 0)
                    countOfHeaders += headerSize;
                while (networkBufferSize < (newBuffer.Count + countOfHeaders) - (position + packetPosition * headerSize))
                {
                    //result.AddRange(BitConverter.GetBytes(hashCode));
                    result.AddRange(BitConverter.GetBytes(guid));
                    result.AddRange(BitConverter.GetBytes((long)buffer.Count + countOfHeaders));
                    result.AddRange(BitConverter.GetBytes((long)packetPosition));
                    result.AddRange(buffer.GetRange(position, networkBufferSize - (int)headerSize));
                    position += networkBufferSize - (int)headerSize;
                    packetPosition++;
                }
                //result.AddRange(BitConverter.GetBytes(hashCode));
                result.AddRange(BitConverter.GetBytes(guid));
                result.AddRange(BitConverter.GetBytes((long)buffer.Count + countOfHeaders));
                result.AddRange(BitConverter.GetBytes((long)packetPosition));
                result.AddRange(buffer.GetRange(position, newBuffer.Count - position));
                //position += networkBufferSize - (int)headerSize;
                //packetPosition++;
            }
            //Logger.Log(result.Count);
            return result.ToArray();
        }

        public override void OnDestroyReaction()
        {
            
        }

        public override void PostInitializeProcess()
        {
            
        }
    }
}
