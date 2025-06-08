using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NECS.ECS.ECSCore;

namespace NECS.ECS.Components.ECSComponentsGroup
{
    [System.Serializable]
    [TypeUid(8)]
    public class ClientComponentGroup : ECSComponentGroup
    {
        static public new long Id { get; set; } = 8;
    }
}
