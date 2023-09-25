using NECS.Core.Logging;
using NECS.ECS.ECSCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.Harness.Model
{
    public interface IManager : IProxyBehaviour
    {
        public IECSObject ConnectPoint
        {
            get; set;
        }

        public abstract void AddManager();
        protected abstract void OnStartManager();
        protected abstract void OnAwakeManager();
        protected abstract void OnRemoveManager();
        protected abstract void OnActivateManager();
        protected abstract void OnDeactivateManager();

        public abstract void ActivateManager();
        public abstract void DeactivateManager();
        public abstract void RemoveManager();
    }
}
