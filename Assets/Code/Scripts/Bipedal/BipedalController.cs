using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Bosch.Bipedal
{
    [SelectionBase]
    [DisallowMultipleComponent]
    public class BipedalController : MonoBehaviour
    {
        private const int JumpSpringIgnoreFrames = 3;
        
        [SerializeField] private float maxGroundSpeed = 12.0f;
        [SerializeField] private float accelerationTime = 0.1f;
        
        [Space]
        [SerializeField] private float jumpHeight = 3.0f;
        [SerializeField] private float gravityScale = 2.0f;

        [Space] 
        [SerializeField] private float groundCheckDistance;
        [SerializeField] private float groundSpring;
        [SerializeField] private float groundDamper;

        private int lastJumpFrame;
        private int frame;
        
        public Vector3 MoveDirection { get; set; }
        public bool Jump { get; set; }
        public virtual Vector3 Gravity => Physics.gravity * gravityScale;
        public Rigidbody Rigidbody { get; private set; }

        public Ground Grounded { get; } = new();

        public virtual bool CanJump => Grounded;

        public float MoveSpeed => Rigidbody.velocity.magnitude - Rigidbody.velocity.y;
        public float MaxSpeed => maxGroundSpeed;
        public float GroundMovement => !Grounded ? 0.0f : (Rigidbody.velocity - Vector3.up * Rigidbody.velocity.y).magnitude / maxGroundSpeed;

        private void Awake()
        {
            Rigidbody = gameObject.GetOrAddComponent<Rigidbody>();

            Rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            Rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            Rigidbody.useGravity = false;
        }

        private void FixedUpdate()
        {
            CheckForGround();
            
            Move();
            ApplyJumpForce();
            AddSpringForce();
            AddGravityForce();

            frame++;
        }

        private void AddGravityForce()
        {
            Rigidbody.AddForce(Gravity, ForceMode.Acceleration);
        }

        private void AddSpringForce()
        {
            if (!Grounded) return;
            if (frame - lastJumpFrame < JumpSpringIgnoreFrames) return;
            
            var contraction = 1.0f - Grounded.hit.distance / groundCheckDistance;
            var force = Vector3.up * (contraction * groundSpring - Rigidbody.velocity.y * groundDamper);
            
            Rigidbody.AddForce(force, ForceMode.Acceleration);
        }

        private void CheckForGround()
        {
            var start = transform.position + Vector3.up * groundCheckDistance;
            var end = transform.position;
            if (Physics.Linecast(start, end, out var hit))
            {
                Grounded.Set(hit);
            }
            else
            {
                Grounded.Invalidate();
            }
        }
        
        private void Move()
        {
            var current = Rigidbody.velocity;
            var target = MoveDirection * maxGroundSpeed;
            var diff = target - current;
            diff.y = 0.0f;
            var force = Vector3.ClampMagnitude(diff, maxGroundSpeed) / accelerationTime;
            
            Rigidbody.AddForce(force, ForceMode.Acceleration);
        }
        
        private void ApplyJumpForce()
        {
            if (!Jump) return;
            Jump = false;

            if (!CanJump) return;
            
            var yVel = Rigidbody.velocity.y;
            var impulse = Vector3.up * Mathf.Sqrt(yVel * yVel + 2.0f * -Gravity.y * jumpHeight);
            Rigidbody.AddForce(impulse, ForceMode.VelocityChange);
            Debug.Log("test");
            
            lastJumpFrame = frame;
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
