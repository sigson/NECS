using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.ECS.ECSCore
{
    [Serializable]
    [TypeUid(6)]
    public class ECSComponentGroup : ECSComponent
    {
        public static new long Id { get; set; }
    }
}
