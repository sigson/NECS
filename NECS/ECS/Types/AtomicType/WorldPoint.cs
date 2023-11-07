using NECS.ECS.ECSCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.ECS.Types.AtomicType
{
    [Serializable]
    [TypeUid(103)]
    public class WorldPoint : BaseCustomType
    {
        public Vector3S Position = new Vector3S();
        public Vector3S Rotation = new Vector3S();
    }
}
