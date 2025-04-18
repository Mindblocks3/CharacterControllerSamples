using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Physics.Systems;
using Unity.CharacterController;
using Unity.NetCode;

[UpdateInGroup(typeof(GhostInputSystemGroup), OrderFirst = true)]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class PlatformerPlayerInputsSystem : SystemBase
{
    private PlatformerInputActions.GameplayMapActions _defaultActionsMap;

    protected override void OnCreate()
    {
        PlatformerInputActions inputActions = new PlatformerInputActions();
        inputActions.Enable();
        inputActions.GameplayMap.Enable();
        _defaultActionsMap = inputActions.GameplayMap;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        RequireForUpdate<NetworkTime>();
        RequireForUpdate(SystemAPI.QueryBuilder().WithAll<PlatformerPlayer, PlatformerPlayerInputs>().Build());
    }

    protected override void OnUpdate()
    {
        foreach (var (playerInputs, player) in SystemAPI.Query<RefRW<PlatformerPlayerInputs>, RefRO<PlatformerPlayer>>().WithAll<GhostOwnerIsLocal>())
        {
            playerInputs.ValueRW.Move = Vector2.ClampMagnitude(_defaultActionsMap.Move.ReadValue<Vector2>(), 1f);
            InputDeltaUtilities.AddInputDelta(ref playerInputs.ValueRW.Look, _defaultActionsMap.LookDelta.ReadValue<Vector2>());
            // playerInputs.ValueRW.Look = _defaultActionsMap.LookDelta.ReadValue<Vector2>();
            // if (math.lengthsq(_defaultActionsMap.LookConst.ReadValue<Vector2>()) > math.lengthsq(_defaultActionsMap.LookDelta.ReadValue<Vector2>()))
            // {
            //     playerInputs.ValueRW.Look = _defaultActionsMap.LookConst.ReadValue<Vector2>() * SystemAPI.Time.DeltaTime;
            // }
            InputDeltaUtilities.AddInputDelta(ref playerInputs.ValueRW.CameraZoom, _defaultActionsMap.CameraZoom.ReadValue<float>());
            //playerInputs.ValueRW.CameraZoom = _defaultActionsMap.CameraZoom.ReadValue<float>();
            playerInputs.ValueRW.SprintHeld = _defaultActionsMap.Sprint.IsPressed();
            playerInputs.ValueRW.RollHeld = _defaultActionsMap.Roll.IsPressed();
            playerInputs.ValueRW.JumpHeld = _defaultActionsMap.Jump.IsPressed();

            playerInputs.ValueRW.JumpPressed = default;
            if (_defaultActionsMap.Jump.WasPressedThisFrame())
            {
                playerInputs.ValueRW.JumpPressed.Set();
            }
            playerInputs.ValueRW.DashPressed = default;
            if (_defaultActionsMap.Dash.WasPressedThisFrame())
            {
                playerInputs.ValueRW.DashPressed.Set();
            }
            playerInputs.ValueRW.CrouchPressed = default;
            if (_defaultActionsMap.Crouch.WasPressedThisFrame())
            {
                playerInputs.ValueRW.CrouchPressed.Set();
            }
            playerInputs.ValueRW.RopePressed = default;
            if (_defaultActionsMap.Rope.WasPressedThisFrame())
            {
                playerInputs.ValueRW.RopePressed.Set();
            }
            playerInputs.ValueRW.ClimbPressed = default;
            if (_defaultActionsMap.Climb.WasPressedThisFrame())
            {
                playerInputs.ValueRW.ClimbPressed.Set();
            }
            playerInputs.ValueRW.FlyNoCollisionsPressed = default;
            if (_defaultActionsMap.FlyNoCollisions.WasPressedThisFrame())
            {
                playerInputs.ValueRW.FlyNoCollisionsPressed.Set();
            }
        }
    }
}

/// <summary>
/// Apply inputs that need to be read at a variable rate
/// </summary>
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[UpdateAfter(typeof(PredictedFixedStepSimulationSystemGroup))]
[BurstCompile]
public partial struct PlatformerPlayerVariableStepControlSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<PlatformerPlayer, PlatformerPlayerNetworkInput, PlatformerPlayerInputs>().Build());
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (playerInputs, playerNetworkInput, player) in SystemAPI.Query<PlatformerPlayerInputs,RefRW<PlatformerPlayerNetworkInput>, PlatformerPlayer>().WithAll<Simulate>())
        {
            // Compute input deltas, compared to last known values
            float2 lookInputDelta = InputDeltaUtilities.GetInputDelta(
                playerInputs.Look, 
                playerNetworkInput.ValueRO.LastProcessedCameraLookInput);
            float zoomInputDelta = InputDeltaUtilities.GetInputDelta(
                playerInputs.CameraZoom, 
                playerNetworkInput.ValueRO.LastProcessedCameraZoomInput);
            playerNetworkInput.ValueRW.LastProcessedCameraLookInput = playerInputs.Look;
            playerNetworkInput.ValueRW.LastProcessedCameraZoomInput = playerInputs.CameraZoom;

            if (SystemAPI.HasComponent<OrbitCameraControl>(player.ControlledCamera))
            {
                OrbitCameraControl cameraControl = SystemAPI.GetComponent<OrbitCameraControl>(player.ControlledCamera);

                cameraControl.FollowedCharacterEntity = player.ControlledCharacter;
                cameraControl.LookDegreesDelta = lookInputDelta;
                cameraControl.ZoomDelta = zoomInputDelta;

                SystemAPI.SetComponent(player.ControlledCamera, cameraControl);
            }
        }
    }
}

/// <summary>
/// Apply inputs that need to be read at a fixed rate.
/// It is necessary to handle this as part of the fixed step group, in case your framerate is lower than the fixed step rate.
/// </summary>
[UpdateInGroup(typeof(PredictedFixedStepSimulationSystemGroup), OrderFirst = true)]
[BurstCompile]
public partial struct PlatformerPlayerFixedStepControlSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<PlatformerPlayer, PlatformerPlayerInputs>().Build());
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (playerInputs, player) in SystemAPI.Query<PlatformerPlayerInputs, PlatformerPlayer>()
                     .WithAll<Simulate>())
        {
            if (SystemAPI.HasComponent<PlatformerCharacterControl>(player.ControlledCharacter) && SystemAPI.HasComponent<PlatformerCharacterStateMachine>(player.ControlledCharacter))
            {
                var characterControl = SystemAPI.GetComponent<PlatformerCharacterControl>(player.ControlledCharacter);
                var stateMachine = SystemAPI.GetComponent<PlatformerCharacterStateMachine>(player.ControlledCharacter);
                float3 characterUp = MathUtilities.GetUpFromRotation(SystemAPI.GetComponent<LocalTransform>(player.ControlledCharacter).Rotation);

                // Get camera rotation data, since our movement is relative to it
                quaternion cameraRotation = quaternion.identity;
                if (SystemAPI.HasComponent<OrbitCamera>(player.ControlledCamera))
                {
                    // Camera rotation is calculated rather than gotten from transform, because this allows us to 
                    // reduce the size of the camera ghost state in a netcode prediction context.
                    // If not using netcode prediction, we could simply get rotation from transform here instead.
                    OrbitCamera orbitCamera = SystemAPI.GetComponent<OrbitCamera>(player.ControlledCamera);
                    cameraRotation = OrbitCameraUtilities.CalculateCameraRotation(characterUp, orbitCamera.PlanarForward, orbitCamera.PitchAngle);
                }
                
                stateMachine.GetMoveVectorFromPlayerInput(stateMachine.CurrentState, in playerInputs, cameraRotation, out characterControl.MoveVector);

                characterControl.JumpHeld = playerInputs.JumpHeld;
                characterControl.RollHeld = playerInputs.RollHeld;
                characterControl.SprintHeld = playerInputs.SprintHeld;

                characterControl.JumpPressed = playerInputs.JumpPressed.IsSet;
                characterControl.DashPressed = playerInputs.DashPressed.IsSet;
                characterControl.CrouchPressed = playerInputs.CrouchPressed.IsSet;
                characterControl.RopePressed = playerInputs.RopePressed.IsSet;
                characterControl.ClimbPressed = playerInputs.ClimbPressed.IsSet;
                characterControl.FlyNoCollisionsPressed = playerInputs.FlyNoCollisionsPressed.IsSet;

                SystemAPI.SetComponent(player.ControlledCharacter, characterControl);
            }
        }
    }
}