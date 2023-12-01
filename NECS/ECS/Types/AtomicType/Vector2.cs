using NECS.ECS.ECSCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.ECS.Types.AtomicType
{
    [System.Serializable]
    [TypeUid(100)]
    public class Vector2S : BaseCustomType
    {
        public float x;
        public float y;
    }
}
