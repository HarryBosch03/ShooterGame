using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Code.Scripts.Player
{
    [Serializable]
    public sealed class PlayerMovement
    {
        private const float Error = 0.01f;
        private const float MaxTranslationDistance = 0.2f;

        [SerializeField] private float maxSpeed = 15.0f;
        [SerializeField] private float accelerationTime = 0.1f;
        [SerializeField][Range(0.0f, 1.0f)] private float airAccelerationPenalty = 0.2f;

        [Space]
        [SerializeField] private float jumpHeight = 4.0f;
        [SerializeField] private float jumpGravity = 3.0f;
        [SerializeField] private float fallingGravity = 3.0f;

        [Space]
        [SerializeField] private float groundTestDistance = 1.0f;
        [SerializeField] private float groundTestSkin = 0.1f;
        [SerializeField] private float groundTestRadius = 0.4f;
        [SerializeField] private float groundTestMaxSlope = 46.0f;

        private PlayerAvatar avatar;

        private InputAction
            moveAction,
            jumpAction,
            slamAction;

        private Vector3 position;
        private Vector3 velocity;
        private Vector3 frameAcceleration;

        private bool jumpLast, slammed;

        private int doubleJumpsLeft;

        private Collider[] colliders;

        private bool grounded;
        private RaycastHit groundHit;
        private Vector3 groundVelocity;

        public Vector3 Gravity => Physics.gravity * (velocity.y > 0.0f && jumpAction.State() ? jumpGravity : fallingGravity);

        public Vector3 RelativeVelocity => velocity - groundVelocity;
        public float MoveSpeed => Mathf.Sqrt(RelativeVelocity.x * RelativeVelocity.x + RelativeVelocity.z * RelativeVelocity.z) / maxSpeed;

        public void Initialize(PlayerAvatar avatar)
        {
            this.avatar = avatar;
            colliders = avatar.GetComponentsInChildren<Collider>();

            Utility.Input.InputAsset = avatar.InputAsset;
            moveAction = Utility.Input.BindFromAsset("Move");
            jumpAction = Utility.Input.BindFromAsset("Jump");
            slamAction = Utility.Input.BindFromAsset("Slam");
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

        private void FixedUpdateActions ()
        {
            Move();
            JumpAction();
        }

        private void PhysicsStuff()
        {
            Integrate();
            Depenetrate();

            avatar.transform.position = position;
        }

        private void SetNextFrameFlags()
        {
            jumpLast = jumpAction.State();
        }

        private void CheckForGround()
        {
            var ray = new Ray(position + Vector3.up * groundTestDistance, Vector3.down);
            var results = Physics.SphereCastAll(ray, groundTestRadius, groundTestDistance + groundTestSkin).OrderBy(e => e.distance);

            grounded = false;
            groundVelocity = Vector3.zero;

            foreach (var result in results)
            {
                var distance = result.point.y - position.y + groundTestSkin;

                if (result.transform.IsChildOf(avatar.transform)) continue;
                if (distance < 0.0f) continue;
                if (Mathf.Acos(result.normal.y) > groundTestMaxSlope * Mathf.Deg2Rad) continue;

                grounded = true;

                if (result.rigidbody)
                {
                    groundVelocity = result.rigidbody.velocity;
                }

                if (velocity.y <= Error) position += Vector3.up * (distance - groundTestSkin);
                if (velocity.y < 0.0f) velocity.y = 0.0f;

                groundHit = result;
                break;
            }
        }

        private void Move()
        {
            var acceleration = 1.0f / accelerationTime;
            if (!grounded) acceleration *= airAccelerationPenalty;

            var input = moveAction.ReadValue<Vector2>();
            var target = avatar.transform.TransformDirection(input.x, 0.0f, input.y) * maxSpeed;
            var diff = (target - velocity);

            diff.y = 0.0f;
            diff = Vector3.ClampMagnitude(diff, maxSpeed);

            var force = diff * acceleration;

            frameAcceleration += force;
            frameAcceleration += groundVelocity;
        }

        private void JumpAction()
        {
            if (!jumpAction.State()) return;
            if (jumpLast) return;
        
            if (!grounded)
            {
                if (doubleJumpsLeft > 0) doubleJumpsLeft--;
                else return;
            }

            var jumpForce = Mathf.Sqrt(2.0f * -Physics.gravity.y * jumpGravity * jumpHeight);
            frameAcceleration += Vector3.up * jumpForce / Time.deltaTime;

            if (velocity.y < 0.0f) frameAcceleration += Vector3.up * -velocity.y / Time.deltaTime;
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

                    if (Physics.ComputePenetration(self, self.transform.position, self.transform.rotation, other, other.transform.position, other.transform.rotation, out var direction, out var distance))
                    {
                        hits.Add(new Tuple<Vector3, float>(direction, distance));
                    }
                }
            }

            foreach (var hit in hits)
            {
                position += hit.Item1 * hit.Item2;

                var dot = Vector3.Dot(hit.Item1, velocity);
                if (dot < 0.0f) velocity -= hit.Item1 * dot;
            }
        }

        private Collider[] GetBroadPhase()
        {
            var bounds = GetBounds();
            return Physics.OverlapBox(bounds.center, bounds.extents);
        }

        public Bounds GetBounds ()
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
            position += velocity * Time.deltaTime;
            velocity += frameAcceleration * Time.deltaTime;
            avatar.transform.position = position;
        }

        public void Update()
        {
            avatar.transform.position = position;
        }
    }
}
