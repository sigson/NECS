using NECS.ECS.ECSCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NECS.ECS.Types.AtomicType
{
    [System.Serializable]
    [TypeUid(101)]
    public class QuaternionS : BaseCustomType
    {
        public float x = 0f;
        public float y = 0f;
        public float z = 0f;
        public float w = 0f;

        public QuaternionS() { }
        public QuaternionS(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

#if UNITY_5_3_OR_NEWER
        public QuaternionS(UnityEngine.Quaternion qa)
        {
            this.x = qa.x; this.y = qa.y; this.z = qa.z; this.w = qa.w;
        }
#endif
        public QuaternionS(Quaternion qa)
        {
            this.x = qa.X; this.y = qa.Y; this.z = qa.Z; this.w = qa.W;
        }

#if UNITY_5_3_OR_NEWER
        public UnityEngine.Quaternion GetNum()
        {
            return new UnityEngine.Quaternion()
            {
                x = this.x,
                y = this.y,
                z = this.z,
                w = this.w
            };
        }
#endif

        public Quaternion NGetNum()
        {
            return new Quaternion()
            {
                X = this.x,
                Y = this.y,
                Z = this.z,
                W = this.w
            };
        }
    }
}
