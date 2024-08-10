using NECS.GameEngineAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.Harness.Model
{
public abstract
#if GODOT4_0_OR_GREATER
    partial
#endif
    class IManagable : ProxyBehaviour
    {
        public IEntityManager ownerManagerSpace = null;
        public long instanceId = Guid.NewGuid().GuidToLongR();
#if UNITY_5_3_OR_NEWER
        public List<UnityEngine.Object> ChildTemp = new List<UnityEngine.Object>();
#else
        public List<EngineApiObjectBehaviour> ChildTemp = new List<EngineApiObjectBehaviour>();
        #endif

        protected virtual void AwakeImpl()
        {
            if (ownerManagerSpace != null)
            {
                ownerManagerSpace.AddManagable(this);
            }
        }

        protected virtual void Awake()
        {
            AwakeImpl();
        }

        protected virtual void OnDestroyImpl()
        {
            if (ownerManagerSpace != null)
            {
                ownerManagerSpace.RemoveManagable(this);
            }
        }

        protected virtual void OnDestroy()
        {
            ChildTemp.ForEach(x => Destroy(x));
            OnDestroyImpl();
        }
    }
}
