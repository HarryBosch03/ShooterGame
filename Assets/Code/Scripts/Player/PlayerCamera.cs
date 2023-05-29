using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Bosch.Player
{
    [Serializable]
    public sealed class PlayerCamera
    {
        [SerializeField] private float mouseSensitivity = 0.3f;

        [Space] 
        [SerializeField] private float shakeAmplitude;
        [SerializeField] private float shakeDecay;
        
        [Space] 
        [SerializeField] private float baseFov = 90.0f;
        [SerializeField] private float fovSmoothTime = 0.2f;

        [Space] 
        [SerializeField] private float sway;

        [Space] 
        [SerializeField] private float slideDrop = 1.4f;
        
        [Space] 
        [SerializeField] private Vector3 bobAmplitude;
        [SerializeField] private float bobFrequency;
        [SerializeField] private float bobSmoothTime = 0.1f;

        private static event Action<Func<Vector3, float>> cameraShakeEvent;
        
        private PlayerAvatar avatar;
        private Transform cameraContainer;

        private Vector3 frameRotation;
        private Vector3 frameRotationTarget;
        private Vector3 frameRotationVelocity;

        private Camera cam;
        private Vector3 rotation;

        private float distance = 0.0f;
        private float fov, fovVelocity;
        private float cHeight, vHeight;
        
        private float shakeIntensity;

        public float Zoom { get; set; }
        public float HeightOffset { get; set; }
        public Vector3 FrameTranslation { get; set; }
        public Quaternion FrameRotation { get; set; }
        
        public void Initialize(PlayerAvatar avatar)
        {
            this.avatar = avatar;
            cameraContainer = avatar.DeepFind("Camera Container");

            cam = Camera.main;
        }

        public void OnEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;

            cameraShakeEvent += ShakeCamera;
        }

        public void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            
            cameraShakeEvent -= ShakeCamera;
        }

        public void Update()
        {
            rotation += (Vector3)Mouse.current.delta.ReadValue() * mouseSensitivity;
            rotation.y = Mathf.Clamp(rotation.y, -90.0f, 90.0f);

            ApplyZoom();
            ApplyBob();
            ApplySway();
            ApplyShake();
            
            DriveCamera();
        }

        private void ApplyShake()
        {
            var angle = Random.value * Mathf.PI * 2.0f;
            var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * shakeIntensity;

            FrameTranslation += cameraContainer.rotation * offset;
            shakeIntensity -= shakeIntensity * shakeDecay * Time.deltaTime;
        }

        private void ApplySway()
        {
            var velocity = avatar.Movement.Target.velocity;
            var d1 = new Vector3
            {
                x = 0.0f,
                y = -Vector3.Dot(avatar.transform.forward, velocity),
                z = -Vector3.Dot(avatar.transform.right, velocity),
            };

            frameRotationTarget += d1 * sway;
        }

        private void ApplyBob()
        {
            if (!avatar.Movement.Grounded) return;
            if (avatar.Movement.Sliding)
            {
                HeightOffset -= slideDrop;
                return;
            }
            
            var speed = avatar.Movement.MoveSpeed;
            distance += speed * Time.deltaTime;

            var shake = new Vector3(Mathf.Cos(distance * bobFrequency),
                -Mathf.Abs(Mathf.Sin(distance * bobFrequency)), 0.0f);
            shake.x *= bobAmplitude.x;
            shake.y *= bobAmplitude.y;
            shake *= bobAmplitude.z * speed / avatar.Movement.MaxSpeed;

            frameRotationTarget += shake;
        }

        private void ApplyZoom()
        {
            var baseFovRad = baseFov * Mathf.Deg2Rad;
            var tFovRad = 2.0f * Mathf.Atan(Mathf.Tan(0.5f * baseFovRad) / Zoom);
            var tFov = tFovRad * Mathf.Rad2Deg;
            fov = Mathf.SmoothDamp(fov, tFov, ref fovVelocity, fovSmoothTime);
            Zoom = 1.0f;
        }

        private void DriveCamera()
        {
            frameRotation.x = Mathf.SmoothDampAngle(frameRotation.x, frameRotationTarget.x, ref frameRotationVelocity.x,
                bobSmoothTime);
            frameRotation.y = Mathf.SmoothDampAngle(frameRotation.y, frameRotationTarget.y, ref frameRotationVelocity.y,
                bobSmoothTime);
            frameRotation.z = Mathf.SmoothDampAngle(frameRotation.z, frameRotationTarget.z, ref frameRotationVelocity.z,
                bobSmoothTime);
            
            var rotation = this.rotation + frameRotation;

            avatar.transform.rotation = Quaternion.Euler(0.0f, rotation.x, 0.0f);
            cameraContainer.rotation = FrameRotation.normalized * Quaternion.Euler(-rotation.y, rotation.x, rotation.z);

            cHeight = Mathf.SmoothDamp(cHeight, HeightOffset, ref vHeight, fovSmoothTime);
            cameraContainer.localPosition = FrameTranslation + new Vector3(0.0f, 1.8f + cHeight, 0.0f);

            cam.transform.position = cameraContainer.transform.position;
            cam.transform.rotation = cameraContainer.transform.rotation;

            cam.fieldOfView = fov;
            HeightOffset = 0.0f;

            frameRotationTarget = Vector3.zero;

            FrameTranslation = Vector3.zero;
            FrameRotation = Quaternion.identity;
        }

        public static void ShakeCameraAll(Func<Vector3, float> intensity) => cameraShakeEvent?.Invoke(intensity);
        public void ShakeCamera(Func<Vector3, float> intensity) => ShakeCamera(intensity(cameraContainer.position));
        public void ShakeCamera(float intensity)
        {
            shakeIntensity += intensity;
        }
    }
}