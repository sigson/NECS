using NECS.ECS.ECSCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.ECS.Types.AtomicType
{
    [System.Serializable]
    [TypeUid(103)]
    public class WorldPoint : BaseCustomType
    {
        static new public long Id { get; set; } = 103;
        public Vector3S Position = new Vector3S();
        public Vector3S Rotation = new Vector3S();
        public WorldPoint2D Get2D() => new WorldPoint2D(){Position = new Vector2S(Position.x, Position.y), Rotation = Rotation};
    }
}
