#if GODOT
using System;
using Godot;
using NECS.GameEngineAPI;

public interface GodotPhysicAgent
{
    Action<Node> OnCollisionEnter{get; set;}

    Action<Node> OnCollisionEnter2D{get; set;}

    Action<Node> OnCollisionExit{get; set;}

    Action<Node> OnCollisionExit2D{get; set;}

    Action<Node> OnCollisionStay{get; set;}

    Action<Node> OnCollisionStay2D{get; set;}

    Action<Node> OnTriggerEnter{get; set;}

    Action<Node> OnTriggerEnter2D{get; set;}

    Action<Node> OnTriggerExit{get; set;}

    Action<Node> OnTriggerExit2D{get; set;}

    Action<Node> OnTriggerStay{get; set;}

    Action<Node> OnTriggerStay2D{get; set;}
}
#endif
