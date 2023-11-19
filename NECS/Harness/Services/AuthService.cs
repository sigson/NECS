using NECS.ECS.ECSCore;
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
    public class AuthService : IService
    {
        private ConcurrentDictionary<SocketAdapter, ECSEntity> SocketToEntity = new ConcurrentDictionary<SocketAdapter, ECSEntity>();
        private ConcurrentDictionary<ECSEntity, SocketAdapter> EntityToSocket = new ConcurrentDictionary<ECSEntity, SocketAdapter>();
        public override void InitializeProcess()
        {
            
        }

        public override void OnDestroyReaction()
        {
            
        }

        public override void PostInitializeProcess()
        {
            
        }
    }
}
