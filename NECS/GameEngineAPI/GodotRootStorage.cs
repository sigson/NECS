#if GODOT
using System;
using Godot;
using NECS.GameEngineAPI;

public
#if GODOT4_0_OR_GREATER
partial
#endif
class GodotRootStorage: EngineApiObjectBehaviour
{
    public static Godot.Node globalRoot = null;
    public static object TreeLocker = new object();
}
#endif