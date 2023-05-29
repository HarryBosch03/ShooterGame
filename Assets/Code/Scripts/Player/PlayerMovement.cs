using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace Bosch.Player
{
    [Serializable]
    public sealed class PlayerMovement
    {
        private const float Error = 0.01f;
        private const int JumpSpringIgnoreFrames = 3;

        [SerializeField] private float maxSpeed = 15.0f;
        [SerializeField] private float accelerationTime = 0.1f;
        [SerializeField] [Range(0.0f, 1.0f)] private float airAccelerationPenalty = 0.2f;

        [Space]
        [SerializeField] private float jumpHeight = 4.0f;
        [SerializeField] [Range(0.0f, 1.0f)]private float leap = 1.0f;
        [SerializeField] private int airJumps = 1;
        [SerializeField] private float jumpGravity = 3.0f;
        [SerializeField] private float fallingGravity = 3.0f;
        
        [Space] 
        [SerializeField] private float groundSpring;
        [SerializeField] private float groundDamper;

        [Space]
        [SerializeField] private float groundTestDistance = 1.0f;
        [SerializeField] private float groundTestSkin = 0.1f;
        [SerializeField] private float groundTestRadius = 0.4f;
        [SerializeField] private float groundTestMaxSlope = 46.0f;

        [Space]
        [SerializeField] private float wallTestDistance = 0.8f;
        [SerializeField] private int wallTestIterations = 64;
        [SerializeField] [Range(0.0f, 1.0f)] private float wallFriction = 0.5f;
        [SerializeField] private float wallJumpHorizontalForce = 10.0f;
        [SerializeField] private float wallJumpHeightPenalty = 0.8f;

        [Space] [SerializeField] private float groundSlamSpeed = 30.0f;

        [Space] [SerializeField] [Range(0.0f, 1.0f)]
        private float slidePenalty = 0.3f;

        private int frame;
        private int lastJumpFrame;

        private int airJumpsLeft;
        private bool isGroundSlamming;

        private Collider[] colliders;

        private RaycastHit groundHit;
        private Vector3 groundVelocity;

        private List<Action> deferredCalls = new();

        public PlayerAvatar Avatar { get; private set; }
        public Rigidbody Target { get; private set; }

        public PlayerInput.InputAxis2D MoveAction => Avatar.Input.Move;
        public PlayerInput.InputWrapper JumpAction => Avatar.Input.Jump;
        public PlayerInput.InputWrapper SlamAction => Avatar.Input.Slam;
        public PlayerInput.InputWrapper SlideAction => Avatar.Input.Slide;

        public Vector3 WorldDirection
        {
            get
            {
                var input = MoveAction.Value;
                return Avatar.transform.TransformDirection(input.x, 0.0f, input.y);
            }
        }

        public float GroundMovement => !Grounded ? 0.0f : (Velocity - Vector3.up * Vector3.Dot(Velocity, Vector3.up)).magnitude / maxSpeed;

        public Vector3 Position => Target.position;

        public Vector3 Velocity => Target.velocity;
        public bool Grounded { get; private set; }
        public float GroundDistance { get; private set; }

        public bool OnWall { get; private set; }
        public RaycastHit WallHit { get; private set; }

        public Vector3 Gravity =>
            Physics.gravity * (Velocity.y > 0.0f && JumpAction.Pressed ? jumpGravity : fallingGravity);

        public Vector3 RelativeVelocity => Velocity - groundVelocity;

        public float MoveSpeed =>
            Mathf.Sqrt(RelativeVelocity.x * RelativeVelocity.x + RelativeVelocity.z * RelativeVelocity.z);

        public float MaxSpeed => maxSpeed;

        public bool Sliding { get; private set; }

        public void Initialize(PlayerAvatar avatar)
        {
            Target = avatar.gameObject.GetOrAddComponent<Rigidbody>();
            Target.constraints = RigidbodyConstraints.FreezeRotation;
            Target.interpolation = RigidbodyInterpolation.Interpolate;
            Target.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            Avatar = avatar;
            colliders = avatar.GetComponentsInChildren<Collider>();
        }

        public void FixedUpdate()
        {
            ResetState();
            PerformChecks();
            FixedUpdateActions();

            frame++;
        }

        private void ResetState()
        {
            Sliding = false;
        }

        private void PerformChecks()
        {
            CheckForGround();
            CheckForWalls();
        }

        private void CheckForWalls()
        {
            OnWall = false;

            if (Grounded) return;
            if (isGroundSlamming) return;

            var hits = new List<RaycastHit>();
            for (var i = 0; i < wallTestIterations * 2 + 1; i++)
            {
                var a = i / (float)wallTestIterations * Mathf.PI * 2.0f;
                var d = new Vector3(Mathf.Cos(a), 0.0f, Mathf.Sin(a));
                var ray = new Ray(Position, d);

                if (!Physics.Raycast(ray, out var hit, wallTestDistance)) continue;

                hits.Add(hit);
            }

            if (hits.Count == 0) return;

            var best = hits.Best(e => 1.0f / e.distance);
            OnWall = true;
            WallHit = best;

            if (Velocity.y < 0.0f) Target.AddForce(Vector3.up * -Velocity.y / Time.deltaTime * wallFriction, ForceMode.Acceleration);
        }

        private void FixedUpdateActions()
        {
            Move();
            
            foreach (var call in deferredCalls) call();
            deferredCalls.Clear();
            
            ApplySpringForce();
            Target.AddForce(Gravity, ForceMode.Acceleration);
        }

        private void ApplySpringForce()
        {
            if (!Grounded) return;
            if (frame - lastJumpFrame < JumpSpringIgnoreFrames) return;
            
            var contraction = GroundDistance / groundTestDistance;
            var force = Vector3.up * (contraction * groundSpring - Target.velocity.y * groundDamper);
            
            Target.AddForce(force, ForceMode.Acceleration);
        }

        private void StartSlam()
        {
            if (isGroundSlamming) return;
            if (Grounded) return;

            isGroundSlamming = true;
        }

        private void CheckForGround()
        {
            var skin = Grounded ? groundTestSkin : 0.0f;

            var ray = new Ray(Position + Vector3.up * groundTestDistance, Vector3.down);
            var results = Physics.SphereCastAll(ray, groundTestRadius, groundTestDistance + skin)
                .OrderBy(e => e.distance);

            foreach (var result in results)
            {
                var distance = result.point.y - Position.y + skin;

                if (result.transform.IsChildOf(Avatar.transform)) continue;
                if (distance < 0.0f) continue;
                if (Mathf.Acos(result.normal.y) > groundTestMaxSlope * Mathf.Deg2Rad) continue;

                Grounded = true;
                GroundDistance = distance;
                airJumpsLeft = airJumps;

                if (result.rigidbody)
                {
                    groundVelocity = result.rigidbody ? result.rigidbody.velocity : Vector3.zero;
                }

                groundHit = result;
                return;
            }

            Grounded = false;
        }

        private void Move()
        {
            if (isGroundSlamming)
            {
                Slam();
                return;
            }

            if (!Grounded)
            {
                AirStrafe(airAccelerationPenalty);
                return;
            }

            if (SlideAction.Pressed)
            {
                AirStrafe(slidePenalty);
                return;
            }

            var acceleration = 1.0f / accelerationTime;

            var target = WorldDirection * maxSpeed;
            var diff = (target - Velocity);

            diff.y = 0.0f;
            diff = Vector3.ClampMagnitude(diff, maxSpeed);

            var force = diff * acceleration;

            Target.AddForce(force, ForceMode.Acceleration);
            Target.AddForce(groundVelocity, ForceMode.Acceleration);
        }

        private void AirStrafe(float penalty)
        {
            var force = WorldDirection * maxSpeed / accelerationTime * penalty;
            var dot = Vector3.Dot(WorldDirection.normalized, Velocity);
            force *= Mathf.Clamp01(1.0f - dot / maxSpeed);
            Target.AddForce(force, ForceMode.Acceleration);
            Sliding = true;
        }

        private void Slam()
        {
            if (Grounded)
            {
                isGroundSlamming = false;
                Avatar.Camera.ShakeCamera(1.0f);
                return;
            }

            Target.AddForce(Vector3.down * groundSlamSpeed - Target.velocity, ForceMode.VelocityChange);
        }

        private void Jump()
        {
            if (OnWall)
            {
                WallJump();
                return;
            }

            if (!Grounded)
            {
                if (airJumpsLeft > 0) airJumpsLeft--;
                else return;
            }

            var verticalForce = Mathf.Sqrt(2.0f * -Physics.gravity.y * jumpGravity * jumpHeight);
            var force = new Vector3(WorldDirection.x * maxSpeed, verticalForce, WorldDirection.z * maxSpeed);

            var contraction = Mathf.Min(1.0f, maxSpeed / new Vector2(Target.velocity.x, Target.velocity.z).magnitude);
            force.x -= Target.velocity.x * contraction;
            force.z -= Target.velocity.z * contraction;
            
            force.y -= Target.velocity.y;
            
            force.x *= leap;
            force.z *= leap;
            
            Target.AddForce(force, ForceMode.VelocityChange);
            lastJumpFrame = frame;
        }

        private void WallJump()
        {
            var direction = WorldDirection;
            if (direction.magnitude < 0.2f)
            {
                direction = WallHit.normal;
            }
            else
            {
                direction = Vector3.Reflect(direction, WallHit.normal);
                direction.Normalize();
            }

            var jumpForce = Mathf.Sqrt(2.0f * -Physics.gravity.y * jumpGravity * jumpHeight);
            var impulse = direction * wallJumpHorizontalForce + Vector3.up * jumpForce * wallJumpHeightPenalty;

            Target.AddForce(impulse, ForceMode.VelocityChange);
            if (Velocity.y < 0.0f) Target.AddForce(Vector3.up * -Velocity.y, ForceMode.VelocityChange);
        }
        
        public void Update()
        {
            Avatar.transform.position = Position;

            JumpAction.CallIfDown(DefferCall(Jump));
            SlamAction.CallIfDown(DefferCall(StartSlam));
        }

        public Action DefferCall(Action callback)
        {
            return () =>
            {
                if (deferredCalls.Contains(callback)) return;
                deferredCalls.Add(callback);
            };
        }
    }
}