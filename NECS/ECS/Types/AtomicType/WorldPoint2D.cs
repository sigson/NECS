using NECS.ECS.ECSCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.ECS.Types.AtomicType
{
    [System.Serializable]
    [TypeUid(104)]
    public class WorldPoint2D : BaseCustomType
    {
        static new public long Id { get; set; } = 104;
        public Vector2S Position = new Vector2S();
        public Vector3S Rotation = new Vector3S();
    }
}
