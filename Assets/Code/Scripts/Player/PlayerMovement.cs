using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Code.Scripts.Player
{
    [Serializable]
    public sealed class PlayerMovement
    {
        private const float Error = 0.01f;
        private const float MaxTranslationDistance = 0.2f;

        [SerializeField] private float maxSpeed = 15.0f;
        [SerializeField] private float accelerationTime = 0.1f;
        [SerializeField] [Range(0.0f, 1.0f)] private float airAccelerationPenalty = 0.2f;

        [FormerlySerializedAs("springSpeedScale")] [Space] [SerializeField]
        private float sprintSpeedScale = 2.0f;

        [SerializeField] private float sprintFovScale = 0.8f;

        [Space] [SerializeField] private float jumpHeight = 4.0f;
        [SerializeField] private float jumpGravity = 3.0f;
        [SerializeField] private float fallingGravity = 3.0f;

        [Space] [SerializeField] private float groundTestDistance = 1.0f;
        [SerializeField] private float groundTestSkin = 0.1f;
        [SerializeField] private float groundTestRadius = 0.4f;
        [SerializeField] private float groundTestMaxSlope = 46.0f;

        [Space] [SerializeField] private float hipWidth = 0.25f;
        [SerializeField] private float footSize = 0.075f;
        [SerializeField] private float stepDistance = 0.8f;

        private PlayerAvatar avatar;

        private InputAction
            moveAction,
            jumpAction,
            sprintAction;

        private MovementState state;

        private Vector3 frameAcceleration;

        private bool jumpLast;

        private int doubleJumpsLeft;

        private Collider[] colliders;

        private RaycastHit groundHit;
        private Vector3 groundVelocity;

        public Vector3 Position { get; private set; }
        public Vector3 Velocity { get; private set; }
        public bool Grounded { get; private set; }

        public Vector3 Gravity =>
            Physics.gravity * (Velocity.y > 0.0f && jumpAction.State() ? jumpGravity : fallingGravity);

        public Vector3 RelativeVelocity => Velocity - groundVelocity;

        public float MoveSpeed =>
            Mathf.Sqrt(RelativeVelocity.x * RelativeVelocity.x + RelativeVelocity.z * RelativeVelocity.z);

        public float MaxSpeed => maxSpeed;

        public void Initialize(PlayerAvatar avatar)
        {
            this.avatar = avatar;
            colliders = avatar.GetComponentsInChildren<Collider>();

            Utility.Input.InputAsset = avatar.InputAsset;
            moveAction = Utility.Input.BindFromAsset("Move");
            jumpAction = Utility.Input.BindFromAsset("Jump");
            sprintAction = Utility.Input.BindFromAsset("Sprint", OnSprint);
        }

        public void FixedUpdate()
        {
            frameAcceleration = Gravity;

            UpdateState();
            PerformChecks();

            FixedUpdateActions();

            PhysicsStuff();
            SetNextFrameFlags();
        }

        private void UpdateState()
        {
            switch (state)
            {
                case MovementState.Sprint:
                    if (moveAction.ReadValue<Vector2>().y < 0.01f)
                    {
                        state = MovementState.Default;
                    }

                    break;
                case MovementState.Default:
                default:
                    break;
            }
        }

        private void SetFootPosition(ref Vector3 footPosition, Vector3 target)
        {
            const float legLength = 1.0f;
            const float legSkin = 0.75f;
            target.y = Position.y;

            var ray = new Ray(target + Vector3.up * (legLength + legSkin * 0.5f), Vector3.down);

            if (!Physics.SphereCast(ray, footSize, out var hit, legLength + legSkin)) return;
            if (Mathf.Acos(hit.normal.y) > groundTestMaxSlope * Mathf.Deg2Rad) return;
            footPosition = hit.point;
        }

        private void PerformChecks()
        {
            CheckForGround();
        }

        private void FixedUpdateActions()
        {
            Move();
            JumpAction();
        }

        private void PhysicsStuff()
        {
            Integrate();
            Depenetrate();

            avatar.transform.position = Position;
        }

        private void SetNextFrameFlags()
        {
            jumpLast = jumpAction.State();
        }

        private void CheckForGround()
        {
            var ray = new Ray(Position + Vector3.up * groundTestDistance, Vector3.down);
            var results = Physics.SphereCastAll(ray, groundTestRadius, groundTestDistance + groundTestSkin)
                .OrderBy(e => e.distance);

            Grounded = false;
            groundVelocity = Vector3.zero;

            foreach (var result in results)
            {
                var distance = result.point.y - Position.y + groundTestSkin;

                if (result.transform.IsChildOf(avatar.transform)) continue;
                if (distance < 0.0f) continue;
                if (Mathf.Acos(result.normal.y) > groundTestMaxSlope * Mathf.Deg2Rad) continue;

                Grounded = true;

                if (result.rigidbody)
                {
                    groundVelocity = result.rigidbody.velocity;
                }

                if (Velocity.y <= Error) Position += Vector3.up * (distance - groundTestSkin);
                if (Velocity.y < 0.0f) Velocity = new Vector3(Velocity.x, 0.0f, Velocity.z);

                groundHit = result;
                break;
            }
        }

        private void Move()
        {
            var acceleration = 1.0f / accelerationTime;
            if (!Grounded) acceleration *= airAccelerationPenalty;

            var speed = maxSpeed;
            switch (state)
            {
                case MovementState.Sprint:
                    speed *= sprintSpeedScale;
                    break;
                case MovementState.Default:
                default:
                    break;
            }

            var input = moveAction.ReadValue<Vector2>();
            var target = avatar.transform.TransformDirection(input.x, 0.0f, input.y) * speed;
            var diff = (target - Velocity);

            diff.y = 0.0f;
            diff = Vector3.ClampMagnitude(diff, speed);

            var force = diff * acceleration;

            frameAcceleration += force;
            frameAcceleration += groundVelocity;
        }

        private void JumpAction()
        {
            if (!jumpAction.State()) return;
            if (jumpLast) return;

            if (!Grounded)
            {
                if (doubleJumpsLeft > 0) doubleJumpsLeft--;
                else return;
            }

            var jumpForce = Mathf.Sqrt(2.0f * -Physics.gravity.y * jumpGravity * jumpHeight);
            frameAcceleration += Vector3.up * jumpForce / Time.deltaTime;

            if (Velocity.y < 0.0f) frameAcceleration += Vector3.up * -Velocity.y / Time.deltaTime;
        }

        private void Depenetrate()
        {
            var others = GetBroadPhase();

            var hits = new List<Tuple<Vector3, float>>();

            foreach (var self in colliders)
            {
                foreach (var other in others)
                {
                    if (other.transform.IsChildOf(avatar.transform)) continue;

                    if (Physics.ComputePenetration(self, self.transform.position, self.transform.rotation, other,
                            other.transform.position, other.transform.rotation, out var direction, out var distance))
                    {
                        hits.Add(new Tuple<Vector3, float>(direction, distance));
                    }
                }
            }

            foreach (var hit in hits)
            {
                Position += hit.Item1 * hit.Item2;

                var dot = Vector3.Dot(hit.Item1, Velocity);
                if (dot < 0.0f) Velocity -= hit.Item1 * dot;
            }
        }

        private Collider[] GetBroadPhase()
        {
            var bounds = GetBounds();
            return Physics.OverlapBox(bounds.center, bounds.extents);
        }

        public Bounds GetBounds()
        {
            var bounds = colliders[0].bounds;
            foreach (var self in colliders)
            {
                bounds.Encapsulate(self.bounds);
            }

            bounds.Expand(Error);
            return bounds;
        }

        private void Integrate()
        {
            Position += Velocity * Time.deltaTime;
            Velocity += frameAcceleration * Time.deltaTime;
            avatar.transform.position = Position;
        }

        public void Update()
        {
            avatar.transform.position = Position;
            
            UpdateVFX();
        }
        
        private void UpdateVFX()
        {
            if (state == MovementState.Sprint) avatar.Camera.Zoom *= sprintFovScale;
        }

        private void OnSprint(bool v)
        {
            if (v)
            {
                state = MovementState.Sprint;
            }
            else if (state == MovementState.Sprint)
            {
                state = MovementState.Default;
            }
        }

        public enum MovementState
        {
            Default,
            Sprint,
        }
    }
}