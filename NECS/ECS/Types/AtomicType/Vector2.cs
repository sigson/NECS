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
    [TypeUid(100)]
    public class Vector2S : BaseCustomType
    {
        static new public long Id { get; set; } = 100;
        public float x;
        public float y;

        public Vector2S() { }

        public Vector2S(float x, float y)
        {
            this.x = x; this.y = y;
        }

#if UNITY_5_3_OR_NEWER
        public Vector2S(UnityEngine.Vector2 vector)
        {
            this.x = vector.x; this.y = vector.y;
        }
#endif

#if GODOT && !GODOT4_0_OR_GREATER
        public Vector2S(Godot.Vector2 vector)
        {
            this.x = vector.x; this.y = vector.y;
        }
#endif

#if GODOT4_0_OR_GREATER
        public Vector2S(Godot.Vector2 vector)
        {
            this.x = vector.X; this.y = vector.Y;
        }
#endif
        public Vector2S(Vector2 vector)
        {
            this.x = vector.X; this.y = vector.Y;
        }

#if UNITY_5_3_OR_NEWER
        public UnityEngine.Vector2 GetNum()
        {
            return new UnityEngine.Vector2()
            {
                x = this.x,
                y = this.y
            };
        }
#endif

#if GODOT && !GODOT4_0_OR_GREATER
        public Godot.Vector2 GetNum()
        {
            return new Godot.Vector2()
            {
                x = this.x,
                y = this.y
            };
        }
#endif

#if GODOT4_0_OR_GREATER
        public Godot.Vector2 GetNum()
        {
            return new Godot.Vector2()
            {
                X = this.x,
                Y = this.y
            };
        }
#endif

        public Vector2 NGetNum()
        {
            return new Vector2()
            {
                X = this.x,
                Y = this.y
            };
        }

#if UNITY_5_3_OR_NEWER
        public UnityEngine.Vector2 ConvertToUnityVector2()
        {
            return new UnityEngine.Vector2(x, y);
        }
#endif

#if GODOT && !GODOT4_0_OR_GREATER
        public Godot.Vector2 ConvertToUnityVector2()
        {
            return new Godot.Vector2(x, y);
        }
#endif

#if GODOT4_0_OR_GREATER
        public Godot.Vector2 ConvertToUnityVector2()
        {
            return new Godot.Vector2(x, y);
        }
#endif
    }
}
