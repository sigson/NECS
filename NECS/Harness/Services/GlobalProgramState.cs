using NECS.Harness.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FileAdapter;
#if UNITY_5_3_OR_NEWER
using UnityEngine;
#endif

namespace NECS.Harness.Services
{
    public
#if GODOT4_0_OR_GREATER
    partial
#endif
    class GlobalProgramState : IService
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

        public string persistentDataPath;
        public string streamingAssetsPath;

        public string GameConfigDir {
            get
            {
#if UNITY_5_3_OR_NEWER
                return FSExtensions.Combine(FSExtensions.Combine(persistentDataPath, "GameData"), "GameConfig");
#endif
#if NET && !GODOT
                return FSExtensions.Combine(FSExtensions.Combine(Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName), "GameData"), "GameConfig");
#endif
#if GODOT
                return FSExtensions.Combine(FSExtensions.Combine(persistentDataPath, "GameData").Replace("\\", "/"), "GameConfig").Replace("\\", "/");
#endif
            }
        }

        public string GameDataDir
        {
            get
            {
#if UNITY_5_3_OR_NEWER
                return FSExtensions.Combine(streamingAssetsPath, "GameData");
#endif
#if NET && !GODOT
                return FSExtensions.Combine(Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName), "GameData");
#endif
#if GODOT
                return FSExtensions.Combine(streamingAssetsPath, "GameData").Replace("\\", "/");
#endif
            }
        }

        public string TechConfigDir
        {
            get
            {
#if UNITY_5_3_OR_NEWER
                return FSExtensions.Combine(FSExtensions.Combine(streamingAssetsPath, "GameData"), "Config");
#endif
#if NET && !GODOT
                return FSExtensions.Combine(FSExtensions.Combine(Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName), "GameData"), "Config");
#endif
#if GODOT
                return FSExtensions.Combine(FSExtensions.Combine(streamingAssetsPath, "GameData").Replace("\\", "/"), "Config").Replace("\\", "/");
#endif
            }
        }

        public string BaseConfigDefault = "{\"DataBase\":{\"DBPath\":\"Config/Users.db\",\"DBType\":\"sqlite\"},\"Networking\":{\"HostAddress\":\"127.0.0.1\",\"Port\":\"6666\",\"BufferSize\":\"950\",\"Protocol\":\"tcp\"},\"NetworkMaliciousEventCounteraction\":{\"MaliciousScoreDecreaseIntervalInSec\":\"10\",\"MaliciousScoreDecreaseValue\":\"100\",\"MaxNetworkMaliciousScore\":\"1000\",\"MaliciousIPTimeoutInSeconds\":\"300\"},\"ECS\":{\"TickTimeMS\":\"5\"}}";

        public string BaseLoginConfig = "{\"LoginData\":{\"login\":\"\",\"password\":\"\"}}";

        public string PathSystemSeparator
        {
            get
            {
                #if GODOT
                return "/";
                #else
                return Path.DirectorySeparatorChar.ToString();
                #endif
            }
        }

        private string _pathSeparator = "";
        public string PathSeparator {
            get{
                if(_pathSeparator == "")
                {
                    #if GODOT
                    return "/";
                    #else
                    return Path.DirectorySeparatorChar.ToString();
                    #endif
                }
                return _pathSeparator;
            }
            set{
                _pathSeparator = value;
            }
        }//"\\";
        private string _pathAltSeparator = "";
        public string PathAltSeparator
        {
            get{
                if(_pathAltSeparator == "")
                {
                    #if GODOT
                    return "\\";
                    #else
                    return Path.AltDirectorySeparatorChar.ToString();
                    #endif
                }
                return _pathAltSeparator;
            }
            set{
                _pathAltSeparator = value;
            }
        }//"/";

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
