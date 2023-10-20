using NECS.ECS.ECSCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.Harness.Model
{
    public abstract class IEntityManager : IECSObjectManager<ECSEntity>
    {
        public ECSEntity ManagerEntity
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

        public long ManagerEntityId => this.ManagerECSObjectId;
    }

}
