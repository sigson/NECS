using NECS.Harness.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
#if UNITY
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
#if UNITY
                return Path.Combine(Application.persistentDataPath, "GameData");
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
#if UNITY
                return Path.Combine(Path.Combine(Application.persistentDataPath, "GameData"), "Config");
#endif
#if NET
                return Path.Combine(Path.Combine(Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName), "GameData"), "Config");
#endif
            }
        }

        public string PathSystemSeparator
        {
            get
            {
                return Path.DirectorySeparatorChar.ToString();
            }
        }
        public string PathSeparator = "/";
        public string PathAltSeparator = "\\";

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
