using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

[GhostComponent]
public struct PlatformerPlayer : IComponentData
{
    [GhostField]
    public Entity ControlledCharacter;
    [GhostField]
    public Entity ControlledCamera;
}

[Serializable]
public struct PlatformerPlayerInputs : IInputComponentData
{
    public float2 Move;
    public float2 Look;
    public float CameraZoom;
    
    public bool SprintHeld;
    public bool RollHeld;
    public bool JumpHeld;
    
    public InputEvent JumpPressed;
    public InputEvent DashPressed;
    public InputEvent CrouchPressed;
    public InputEvent RopePressed;
    public InputEvent ClimbPressed;
    public InputEvent FlyNoCollisionsPressed;
}

[Serializable]
[GhostComponent(SendTypeOptimization = GhostSendType.OnlyPredictedClients)]
public struct PlatformerPlayerNetworkInput : IComponentData
{
    [GhostField()]
    public float2 LastProcessedCameraLookInput;
    [GhostField()]
    public float LastProcessedCameraZoomInput;
}