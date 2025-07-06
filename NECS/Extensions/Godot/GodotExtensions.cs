#if NET || UNITY
public static class GodotExtensionsMock
{
    public static string FixPath(this string path)
    {
        return path;
    }
}
#endif

#if GODOT && !GODOT4_0_OR_GREATER
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class GodotExtensions
{
    public static string FixPath(this string path)
    {
        #if GODOT
        var result = path.Replace("\\", NECS.Harness.Services.GlobalProgramState.instance.PathSystemSeparator).Replace("/", NECS.Harness.Services.GlobalProgramState.instance.PathSystemSeparator);
        if(result.IndexOf("user://") == -1 && result.IndexOf("user:/") != -1)
        {
            result = result.Replace("user:/", "user://");
        }
        if(result.IndexOf("res://") == -1 && result.IndexOf("res:/") != -1)
        {
            result = result.Replace("res:/", "res://");
        }
        return result;
        #else
        return path;
        #endif
    }
    public static T MockGObject<T>(this Node node) where T : Godot.Node, new()
    {
        var parent = node.GetParent();
        if(parent == null)
        return null;
        var mocknode = new T();
        parent.AddChild(mocknode);
        var childs = parent.GetChildCount();
        parent.RemoveChild(node);
        mocknode.AddChild(node);
        return mocknode;
    }

    public static T GetChild<T>(this Node node) where T : Godot.Node, new()
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is T)
            {
                return (T)child;
            }
        }
        return null;
    }
    public static Vector3 MoveTowardDistance(this Vector3 from, Vector3 to, float distance_delta)
    {
        return from.MoveToward(to, distance_delta * from.DistanceTo(to));
    }

    public static Vector3 Set(this Vector3 original, float? X = null, float? Y = null, float? Z = null)
    {
        return new Vector3( X == null ? original.x : (float)X, Y == null ? original.y : (float)Y, Z == null ? original.z : (float)Z);
    }

    public static Vector3 Increase(this Vector3 original, float? X = null, float? Y = null, float? Z = null)
    {
        return new Vector3(X == null ? original.x : original.x + (float)X, Y == null ? original.y : original.y + (float)Y, Z == null ? original.z : original.z + (float)Z);
    }
}
#endif
#if GODOT4_0_OR_GREATER
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class GodotExtensions
{
    public static Vector3 MoveTowardDistance(this Vector3 from, Vector3 to, float distance_delta)
    {
        return from.MoveToward(to, distance_delta * from.DistanceTo(to));
    }

    public static Vector3 Set(this Vector3 original, float? X = null, float? Y = null, float? Z = null)
    {
        return new Vector3( X == null ? original.X : (float)X, Y == null ? original.Y : (float)Y, Z == null ? original.Z : (float)Z);
    }

    public static Vector3 Increase(this Vector3 original, float? X = null, float? Y = null, float? Z = null)
    {
        return new Vector3(X == null ? original.X : original.X + (float)X, Y == null ? original.Y : original.Y + (float)Y, Z == null ? original.Z : original.Z + (float)Z);
    }
}
#endif
#if GODOT && !GODOT4_0_OR_GREATER


#endif
