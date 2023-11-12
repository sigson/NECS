using NECS.Harness.Model;
using NECS.Network.NetworkModels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.Harness.Services
{
    public class NetworkMaliciousEventCounteractionService : IService
    {
        public static NetworkMaliciousEventCounteractionService instance => SGT.Get<NetworkMaliciousEventCounteractionService>();
        public ConcurrentDictionary<long, ScoreObject> maliciousScoringStorage = new ConcurrentDictionary<long, ScoreObject>();
        public ConcurrentDictionary<string, long> UnwantedSocketInfo = new ConcurrentDictionary<string, long>();
        public override void InitializeProcess()
        {
            throw new NotImplementedException();
        }

        public override void OnDestroyReaction()
        {
            throw new NotImplementedException();
        }

        public override void PostInitializeProcess()
        {
            throw new NotImplementedException();
        }
    }

    public class ScoreObject
    {
        public SocketAdapter SocketAdapter { get => Lambda.LineFunction(() => NetworkingService.instance.SocketAdapters[this.SocketId]); }
        public long SocketId;
        private int score;
        public int Score
        {
            get { return score; }
            set
            {
                score = value;
                if(score >= NetworkingService.instance.MaxNetworkMaliciousScore)
                {
                    NetworkMaliciousEventCounteractionService.instance.UnwantedSocketInfo[SocketAdapter.Address] = DateTime.Now.AddSeconds(NetworkingService.instance.MaliciousIPTimeoutInSeconds).Ticks;
                    SocketAdapter.DisconnectAsync();
                }
            }
        }
    }
}
