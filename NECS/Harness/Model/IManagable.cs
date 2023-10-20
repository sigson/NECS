using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.Harness.Model
{
    public abstract class IManagable : ProxyBehaviour
    {
        public IEntityManager ownerManagerSpace = null;
        public long instanceId = Guid.NewGuid().GuidToLong();
        public List<IEngineApiObjectBehaviour> ChildTemp = new List<IEngineApiObjectBehaviour>();

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
