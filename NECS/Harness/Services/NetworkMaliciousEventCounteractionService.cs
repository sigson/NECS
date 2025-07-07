using NECS.Extensions;
using NECS.Extensions.ThreadingSync;
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
    public
#if GODOT4_0_OR_GREATER
    partial
#endif
    class NetworkMaliciousEventCounteractionService : IService
    {
        private static NetworkMaliciousEventCounteractionService cacheInstance;
        public static NetworkMaliciousEventCounteractionService instance
        {
            get
            {
                if (cacheInstance == null)
                    cacheInstance = SGT.Get<NetworkMaliciousEventCounteractionService>();
                return cacheInstance;
            }
        }
        public DictionaryWrapper<long, ScoreObject> maliciousScoringStorage = new DictionaryWrapper<long, ScoreObject>();
        public DictionaryWrapper<string, long> UnwantedSocketInfo = new DictionaryWrapper<string, long>();

        public int MaliciousScoreDecreaseIntervalInSec = 10;
        /// <summary>
        /// UTanks examples
        /// flamethower attack - 0.5 in sec, then for 10sec = 20 packets maximum (if not count reloading) + 2 for start and stop
        /// twins attack - 0.5 in sec + hits, then for 10 sec = 40 packets in maximum
        /// movement packets sends - 0.2 in sec, then for 10 sec = 100 packets maximum + 100 packets max from directive player controls command in maximum
        /// summary we have max 240 packets from player in maximum variant
        /// if i extract from calculation movement packets - i have 40 packets in maximum
        /// </summary>
        public int MaliciousScoreDecreaseValue = 100;
        public int MaxNetworkMaliciousScore = 1000;
        public int MaliciousIPTimeoutInSeconds = 300;
        private TimerEx maliciousTimerInstance = null;
        /// <summary>
        /// if any want to attack with Multiple-Session DoS Attack - this storage save all of sockets info and if count session from one resource will more then 1000 - all clients will be banned
        /// </summary>
        public DictionaryWrapper<string, int> SocketInfoDB = new DictionaryWrapper<string, int>();
        public override void InitializeProcess()
        {
            if(GlobalProgramState.instance.ProgramType == GlobalProgramState.ProgramTypeEnum.Offline)
            {
                return;
            }
            MaliciousScoreDecreaseIntervalInSec = ConstantService.instance.GetByConfigPath("baseconfig").GetObject<int>("NetworkMaliciousEventCounteraction/MaliciousScoreDecreaseIntervalInSec");
            MaliciousScoreDecreaseValue = ConstantService.instance.GetByConfigPath("baseconfig").GetObject<int>("NetworkMaliciousEventCounteraction/MaliciousScoreDecreaseValue");
            MaxNetworkMaliciousScore = ConstantService.instance.GetByConfigPath("baseconfig").GetObject<int>("NetworkMaliciousEventCounteraction/MaxNetworkMaliciousScore");
            MaliciousIPTimeoutInSeconds = ConstantService.instance.GetByConfigPath("baseconfig").GetObject<int>("NetworkMaliciousEventCounteraction/MaliciousIPTimeoutInSeconds");

            maliciousTimerInstance = new TimerEx()
            {
                AutoReset = true,
                Interval = MaliciousScoreDecreaseIntervalInSec * 1000
            };
            maliciousTimerInstance.Elapsed += (sender, e) => MaliciousScoreDecrease();
            maliciousTimerInstance.Start();
        }

        protected virtual void MaliciousScoreDecrease()
        {
            foreach (var socket in maliciousScoringStorage)
            {
                socket.Value.Score -= MaliciousScoreDecreaseValue;
                if (socket.Value.Score <= 0)
                    socket.Value.Score = 0;
            }
        }

        public override void OnDestroyReaction()
        {

        }

        public override void PostInitializeProcess()
        {

        }
        
        protected override Action<int>[] GetInitializationSteps()
        {
            return new Action<int>[]
            {
                (step) => {  },
                (step) => { InitializeProcess(); },
            };
        }

        protected override void SetupCallbacks(List<IService> allServices)
        {
            this.RegisterCallbackUnsafe(ECSService.instance.GetSGTId(), 1, (d) => { return true; }, () =>
            {
                //await for ecs initalization
            }, 0);
        }
    }

    public class ScoreObject
    {
        public SocketAdapter SocketAdapter { get => Lambda.LineFunction(() => NetworkingService.instance.SocketAdapters[this.SocketId]); }
        public long SocketId;
        private int score = 0;
        public int Score
        {
            get { return score; }
            set
            {
                score = value;
                if(score >= NetworkMaliciousEventCounteractionService.instance.MaxNetworkMaliciousScore)
                {
                    NetworkMaliciousEventCounteractionService.instance.UnwantedSocketInfo[SocketAdapter.Address] = DateTime.Now.AddSeconds(NetworkMaliciousEventCounteractionService.instance.MaliciousIPTimeoutInSeconds).Ticks;
                    SocketAdapter.DisconnectAsync();
                }
            }
        }
    }
}
