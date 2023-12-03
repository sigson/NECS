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
    [TypeUid(102)]
    public class Vector3S : BaseCustomType
    {
        public float x = 0f;
        public float y = 0f;
        public float z = 0f;
        public Vector3S() { }

        public Vector3S(float x, float y, float z)
        {
            this.x = x; this.y = y; this.z = z;
        }

#if UNITY_5_3_OR_NEWER
        public Vector3S(UnityEngine.Vector3 vector)
        {
            this.x = vector.x; this.y = vector.y; this.z = vector.z;
        }
#endif
        public Vector3S(Vector3 vector)
        {
            this.x = vector.X; this.y = vector.Y; this.z = vector.Z;
        }

#if UNITY_5_3_OR_NEWER
        public UnityEngine.Vector3 GetNum()
        {
            return new UnityEngine.Vector3()
            {
                x = this.x,
                y = this.y,
                z = this.z
            };
        }
#endif

        public Vector3 NGetNum()
        {
            return new Vector3()
            {
                X = this.x,
                Y = this.y,
                Z = this.z
            };
        }

#if UNITY_5_3_OR_NEWER
        public UnityEngine.Vector3 ConvertToUnityVector3()
        {
            return new UnityEngine.Vector3(x, y, z);
        }

        public UnityEngine.Vector3 ConvertToUnityVector3Constant007Scaling()
        {
            return new UnityEngine.Vector3(x * 0.007f, y * 0.007f, z * 0.007f);
        }

        public static Vector3S ConvertToVector3SUnScaling(UnityEngine.Vector3 vector3N, float unscaler)
        {
            return new Vector3S(vector3N.x / unscaler, vector3N.y / unscaler, vector3N.z / unscaler);
        }

        public static Vector3S ConvertToVector3SScaling(UnityEngine.Vector3 vector3N, float scaler)
        {
            return new Vector3S(vector3N.x * scaler, vector3N.y * scaler, vector3N.z * scaler);
        }
#endif
    }
}
