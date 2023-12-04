using NECS.Harness.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if UNITY_5_3_OR_NEWER
using UnityEngine;
#endif

namespace NECS.Harness.Services
{
    public class GlobalProgramState : IService
    {
        private static GlobalProgramState cacheInstance;
        public static GlobalProgramState instance
        {
            get
            {
                if (cacheInstance == null)
                    cacheInstance = SGT.Get<GlobalProgramState>();
                return cacheInstance;
            }
        }

        public string GameConfigDir {
            get
            {
#if UNITY_5_3_OR_NEWER
                return Path.Combine(Path.Combine(Application.persistentDataPath, "GameData"), "GameConfig");
#endif
#if NET
                return Path.Combine(Path.Combine(Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName), "GameData"), "GameConfig");
#endif
            }
        }

        public string GameDataDir
        {
            get
            {
#if UNITY_5_3_OR_NEWER
                return Path.Combine(Application.streamingAssetsPath, "GameData");
#endif
#if NET
                return Path.Combine(Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName), "GameData");
#endif
            }
        }

        public string TechConfigDir
        {
            get
            {
#if UNITY_5_3_OR_NEWER
                return Path.Combine(Path.Combine(Application.streamingAssetsPath, "GameData"), "Config");
#endif
#if NET
                return Path.Combine(Path.Combine(Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName), "GameData"), "Config");
#endif
            }
        }

        public string BaseConfigDefault = "{\"DataBase\":{\"DBPath\":\"Config/Users.db\",\"DBType\":\"sqlite\"},\"Networking\":{\"HostAddress\":\"127.0.0.1\",\"Port\":\"6666\",\"BufferSize\":\"1024\",\"Protocol\":\"tcp\"},\"NetworkMaliciousEventCounteraction\":{\"MaliciousScoreDecreaseIntervalInSec\":\"10\",\"MaliciousScoreDecreaseValue\":\"100\",\"MaxNetworkMaliciousScore\":\"1000\",\"MaliciousIPTimeoutInSeconds\":\"300\"},\"ECS\":{\"TickTimeMS\":\"5\"}}";

        public string BaseLoginConfig = "{\"LoginData\":{\"login\":\"\",\"password\":\"\"}}";

        public string PathSystemSeparator
        {
            get
            {
                return Path.DirectorySeparatorChar.ToString();
            }
        }
        public string PathSeparator = "\\";
        public string PathAltSeparator = "/";

        public ProgramTypeEnum ProgramType;
        public enum ProgramTypeEnum
        {
            Server,
            Client
        }

        public override void InitializeProcess()
        {
            //ProgramType = ProgramTypeEnum.Server;
        }

        public override void OnDestroyReaction()
        {
            
        }

        public override void PostInitializeProcess()
        {
            
        }
    }
}
