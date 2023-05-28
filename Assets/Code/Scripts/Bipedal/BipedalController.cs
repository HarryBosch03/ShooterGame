using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Bosch.Bipedal
{
    [SelectionBase]
    [DisallowMultipleComponent]
    public class BipedalController : MonoBehaviour
    {
        [SerializeField] private float maxGroundSpeed = 12.0f;
        [SerializeField] private float accelerationTime = 0.1f;
        
        [Space]
        [SerializeField] private float jumpHeight = 3.0f;
        [SerializeField] private float gravityScale = 2.0f;

        [Space] [SerializeField] private float groundCheckDistance;
        [Space] [SerializeField] private float groundSpring;
        [Space] [SerializeField] private float groundDamper;

        private new Rigidbody rigidbody;
        
        public Vector3 MoveDirection { get; set; }
        public bool Jump { get; set; }
        public Vector3 Gravity => Physics.gravity * gravityScale;
        
        public readonly Ground grounded = new();
       
        private void Awake()
        {
            rigidbody = gameObject.GetOrAddComponent<Rigidbody>();

            rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.useGravity = false;
        }

        private void FixedUpdate()
        {
            CheckForGround();
            
            Move();
            PerformJump();

            AddSpringForce();

            AddGravityForce();
        }

        private void AddGravityForce()
        {
            rigidbody.AddForce(Gravity, ForceMode.Acceleration);
        }

        private void AddSpringForce()
        {
            var contraction = 1.0f - grounded.hit.distance / groundCheckDistance;
            var force = Vector3.up * (contraction * groundSpring - rigidbody.velocity.y * groundDamper);
            
            rigidbody.AddForce(force, ForceMode.Acceleration);
        }

        private void CheckForGround()
        {
            var start = transform.position + Vector3.up * groundCheckDistance;
            var end = transform.position;
            if (Physics.Linecast(start, end, out var hit))
            {
                grounded.Set(hit);
            }
            else
            {
                grounded.Invalidate();
            }
        }
        
        private void Move()
        {
            var current = rigidbody.velocity;
            var target = MoveDirection * maxGroundSpeed;
            var diff = target - current;
            diff.y = 0.0f;
            var force = Vector3.ClampMagnitude(diff, maxGroundSpeed) / accelerationTime;
            
            rigidbody.AddForce(force, ForceMode.Acceleration);
        }
        
        private void PerformJump(bool force = false)
        {
            if (!grounded && !force) return;

            var impulse = Vector3.up * (rigidbody.velocity.y + 2.0f * Gravity.y * jumpHeight);
            rigidbody.AddForce(impulse, ForceMode.VelocityChange);
        }

        public class Ground
        {
            public RaycastHit hit;
            public GameObject gameObject;
            public Rigidbody rigidbody;
            public float time;
            public bool valid;
            
            public Ground Set(RaycastHit hit)
            {
                valid = true;
                this.hit = hit;
                
                time = Time.time;
                
                gameObject = hit.transform.gameObject;
                rigidbody = hit.rigidbody;
                
                return this;
            }

            public void Invalidate() => valid = false;

            public static implicit operator bool(Ground ground) => ground != null && ground.valid;
        }
    }
}
