using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.GameEngineAPI
{
    public class EngineApiCompoent
#if UNITY_5_3_OR_NEWER
        : UnityEngine.Component
#endif
    {

    }
    public class EngineApiCollider3D
#if UNITY_5_3_OR_NEWER
        : UnityEngine.Collider
#endif
    {

    }

    public class EngineApiCollision3D
#if UNITY_5_3_OR_NEWER
        : UnityEngine.Collision
#endif
    {

    }

    public class EngineApiCollider2D
#if UNITY_5_3_OR_NEWER
        : UnityEngine.Collider2D
#endif
    {

    }

    public class EngineApiCollision2D
#if UNITY_5_3_OR_NEWER
        : UnityEngine.Collision2D
#endif
    {

    }
}
