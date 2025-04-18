using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

[GhostComponent]
[Serializable]
public struct OrbitCamera : IComponentData
{
    public float RotationSpeed;
    public float MaxVAngle;
    public float MinVAngle;
    public bool RotateWithCharacterParent;

    public float MinDistance;
    public float MaxDistance;
    public float DistanceMovementSpeed;
    public float DistanceMovementSharpness;

    public float ObstructionRadius;
    public float ObstructionInnerSmoothingSharpness;
    public float ObstructionOuterSmoothingSharpness;
    public bool PreventFixedUpdateJitter;
    
    public float CameraTargetTransitionTime;
    
    [GhostField]
    public float TargetDistance;
    public float SmoothedTargetDistance;
    public float ObstructedDistance;
    [GhostField]
    public float PitchAngle;
    [GhostField]
    public float3 PlanarForward;
    
    public Entity ActiveCameraTarget;
    public Entity PreviousCameraTarget;
    public float CameraTargetTransitionStartTime;
    public RigidTransform CameraTargetTransform;
    public RigidTransform CameraTargetTransitionFromTransform;
    public bool PreviousCalculateUpFromGravity;
}

[Serializable]
public struct OrbitCameraControl : IComponentData
{
    public Entity FollowedCharacterEntity;
    public float2 LookDegreesDelta;
    public float ZoomDelta;
}

[Serializable]
public struct OrbitCameraIgnoredEntityBufferElement : IBufferElementData
{
    public Entity Entity;
}