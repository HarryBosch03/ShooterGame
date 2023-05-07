using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Bosch.Player
{
    [Serializable]
    public sealed class PlayerMovement
    {
        private const float Error = 0.01f;

        [SerializeField] private float maxSpeed = 15.0f;
        [SerializeField] private float accelerationTime = 0.1f;
        [SerializeField] [Range(0.0f, 1.0f)] private float airAccelerationPenalty = 0.2f;

        [Space] 
        [SerializeField] private float jumpHeight = 4.0f;
        [SerializeField] private int airJumps = 1;
        [SerializeField] private float jumpGravity = 3.0f;
        [SerializeField] private float fallingGravity = 3.0f;

        [Space] [SerializeField] private float groundTestDistance = 1.0f;
        [SerializeField] private float groundTestSkin = 0.1f;
        [SerializeField] private float groundTestRadius = 0.4f;
        [SerializeField] private float groundTestMaxSlope = 46.0f;

        public PlayerAvatar Avatar { get; private set; }

        public InputAction MoveAction { get; private set; }
        public InputAction JumpAction { get; private set; }
        public InputAction SprintAction { get; private set; }
        public InputAction CrouchAction { get; private set; }

        private Vector3 frameAcceleration;

        private bool jumpLast;
        
        private int airJumpsLeft;

        private Collider[] colliders;

        private RaycastHit groundHit;
        private Vector3 groundVelocity;

        public Vector3 Position
        {
            get => Avatar.transform.position;
            private set => Avatar.transform.position = value;
        }

        public Vector3 Velocity { get; private set; }
        public bool Grounded { get; private set; }

        public Vector3 Gravity =>
            Physics.gravity * (Velocity.y > 0.0f && JumpAction.State() ? jumpGravity : fallingGravity);

        public Vector3 RelativeVelocity => Velocity - groundVelocity;

        public float MoveSpeed =>
            Mathf.Sqrt(RelativeVelocity.x * RelativeVelocity.x + RelativeVelocity.z * RelativeVelocity.z);

        public float MaxSpeed => maxSpeed;

        public void Initialize(PlayerAvatar avatar)
        {
            this.Avatar = avatar;
            colliders = avatar.GetComponentsInChildren<Collider>();

            Utility.Input.InputAsset = avatar.InputAsset;
            MoveAction = Utility.Input.BindFromAsset("Move");
            JumpAction = Utility.Input.BindFromAsset("Jump");
        }

        public void FixedUpdate()
        {
            frameAcceleration = Gravity;

            PerformChecks();

            FixedUpdateActions();

            PhysicsStuff();
            SetNextFrameFlags();
        }

        private void PerformChecks()
        {
            CheckForGround();
        }

        private void FixedUpdateActions()
        {
            Move();
            Jump();
        }

        private void PhysicsStuff()
        {
            Integrate();
            Depenetrate();
        }

        private void SetNextFrameFlags()
        {
            jumpLast = JumpAction.State();
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
                airJumpsLeft = airJumps;

                if (result.rigidbody)
                {
                    groundVelocity = result.rigidbody ? result.rigidbody.velocity : Vector3.zero;
                }

                if (Velocity.y <= Error) Position += Vector3.up * (distance - skin);
                if (Velocity.y < 0.0f) Velocity = new Vector3(Velocity.x, 0.0f, Velocity.z);

                groundHit = result;
                return;
            }

            Grounded = false;
        }

        private void Move()
        {
            var acceleration = 1.0f / accelerationTime;
            if (!Grounded) acceleration *= airAccelerationPenalty;

            var input = MoveAction.ReadValue<Vector2>();
            var target = Avatar.transform.TransformDirection(input.x, 0.0f, input.y) * maxSpeed;
            var diff = (target - Velocity);

            diff.y = 0.0f;
            diff = Vector3.ClampMagnitude(diff, maxSpeed);

            var force = diff * acceleration;

            frameAcceleration += force;
            frameAcceleration += groundVelocity;
        }

        private void Jump()
        {
            if (!JumpAction.State()) return;
            if (jumpLast) return;

            if (!Grounded)
            {
                if (airJumpsLeft > 0) airJumpsLeft--;
                else return;
            }

            var jumpForce = Mathf.Sqrt(2.0f * -Physics.gravity.y * jumpGravity * jumpHeight);
            frameAcceleration += Vector3.up * jumpForce / Time.deltaTime;

            if (Velocity.y < 0.0f) frameAcceleration += Vector3.up * -Velocity.y / Time.deltaTime;
        }

        private void Depenetrate()
        {
            var others = GetBroadPhase();

            foreach (var self in colliders)
            {
                foreach (var other in others)
                {
                    if (other.transform.IsChildOf(Avatar.transform)) continue;

                    if (!Physics.ComputePenetration(self, self.transform.position, self.transform.rotation, other,
                            other.transform.position, other.transform.rotation, out var direction,
                            out var distance)) continue;

                    Position += direction * distance;
                    var dot = Vector3.Dot(direction, Velocity);
                    if (dot < 0.0f) Velocity -= direction * dot;
                }
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
            Avatar.transform.position = Position;
        }

        public void Update()
        {
            Avatar.transform.position = Position;
        }
    }
}