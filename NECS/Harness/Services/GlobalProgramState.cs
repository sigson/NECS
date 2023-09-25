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
