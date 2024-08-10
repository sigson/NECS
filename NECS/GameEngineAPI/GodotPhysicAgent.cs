#if GODOT4_0_OR_GREATER
using System;
using Godot;
using NECS.GameEngineAPI;

public interface GodotPhysicAgent
{
    public Action<Node> OnCollisionEnter{get; set;}

    public Action<Node> OnCollisionEnter2D{get; set;}

    public Action<Node> OnCollisionExit{get; set;}

    public Action<Node> OnCollisionExit2D{get; set;}

    public Action<Node> OnCollisionStay{get; set;}

    public Action<Node> OnCollisionStay2D{get; set;}

    public Action<Node> OnTriggerEnter{get; set;}

    public Action<Node> OnTriggerEnter2D{get; set;}

    public Action<Node> OnTriggerExit{get; set;}

    public Action<Node> OnTriggerExit2D{get; set;}

    public Action<Node> OnTriggerStay{get; set;}

    public Action<Node> OnTriggerStay2D{get; set;}
}
#endif