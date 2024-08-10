#if GODOT4_0_OR_GREATER
using System;
using Godot;
using NECS.GameEngineAPI;

public partial class GodotRootNode: EngineApiObjectBehaviour
{
    public static Godot.Node globalRoot = null;
}
#endif