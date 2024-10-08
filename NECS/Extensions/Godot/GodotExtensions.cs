﻿#if GODOT && !GODOT4_0_OR_GREATER
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
