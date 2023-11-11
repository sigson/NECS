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
        public static GlobalProgramState instance => SGT.Get<GlobalProgramState>();

        public string ConfigDir {
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
                return Path.Combine(Application.dataPath, "Config");
#endif
#if NET
                return Path.Combine(Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName), "Config");
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
            ProgramType = ProgramTypeEnum.Server;
        }

        public override void OnDestroyReaction()
        {
            
        }

        public override void PostInitializeProcess()
        {
            
        }
    }
}
