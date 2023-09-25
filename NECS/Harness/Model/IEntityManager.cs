using NECS.ECS.ECSCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.Harness.Model
{
    public interface IEntityManager : IManager
    {
        public ECSComponent ManagerComponent
        {
            get; set;
        }

        public long ManagerComponentId { get; }
    }
}
