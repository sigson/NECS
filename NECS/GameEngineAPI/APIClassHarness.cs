using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.GameEngineAPI
{
    public
#if GODOT4_0_OR_GREATER
    partial
#endif
    class EngineApiCompoent
#if UNITY_5_3_OR_NEWER
        : UnityEngine.Component
#endif
#if GODOT
        : Godot.Node
#endif
    {

    }
    public
#if GODOT4_0_OR_GREATER
    partial
#endif
     class EngineApiCollider3D
#if UNITY_5_3_OR_NEWER
        : UnityEngine.Collider
#endif
#if GODOT && !GODOT4_0_OR_GREATER
        : object
#endif
#if GODOT4_0_OR_GREATER
        : object
#endif
    {

    }

    public class EngineApiCollision3D
#if UNITY_5_3_OR_NEWER
        : UnityEngine.Collision
#endif
#if GODOT && !GODOT4_0_OR_GREATER
        : object
#endif
#if GODOT4_0_OR_GREATER
        : object
#endif
    {

    }

    public class EngineApiCollider2D
#if UNITY_5_3_OR_NEWER
        : UnityEngine.Collider2D
#endif
#if GODOT && !GODOT4_0_OR_GREATER
        : object
#endif
#if GODOT4_0_OR_GREATER
        : object
#endif
    {

    }

    public class EngineApiCollision2D
#if UNITY_5_3_OR_NEWER
        : UnityEngine.Collision2D
#endif
#if GODOT && !GODOT4_0_OR_GREATER
        : object
#endif
#if GODOT4_0_OR_GREATER
        : object
#endif
    {

    }
}
