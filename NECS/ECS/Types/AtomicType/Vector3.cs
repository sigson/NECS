using NECS.ECS.ECSCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.ECS.Types.AtomicType
{
    [System.Serializable]
    [TypeUid(102)]
    public class Vector3S : BaseCustomType
    {
        public float x = 0f;
        public float y = 0f;
        public float z = 0f;
    }
}
