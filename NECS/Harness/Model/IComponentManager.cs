using NECS.ECS.ECSCore;
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
    class IComponentManager : IECSObjectManager<ECSComponent>
    {
        public ECSComponent ManagerComponent
        {
            get
            {
                return this.ManagerECSObject;
            }
            set
            {
                this.ManagerECSObject = value;
            }
        }

        public long ManagerComponentId => this.ManagerECSObjectId;
    }
}
