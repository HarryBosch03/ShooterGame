using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Bosch.Scripts.Player
{
    [Serializable]
    public sealed class PlayerCamera
    {
        [SerializeField] private float mouseSensitivity = 0.3f;

        [Space] 
        [SerializeField] private float baseFov = 90.0f;
        [SerializeField] private float fovSmoothTime = 0.2f;

        [Space] 
        [SerializeField] private float sway;
        
        [Space] [SerializeField] private Vector3 shakeAmplitude;
        [Space] [SerializeField] private float shakeFrequency;
        [Space] [SerializeField] private float shakeSmoothTime = 0.1f;

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

        public float Zoom { get; set; }
        public float HeightOffset { get; set; }

        public void Initialize(PlayerAvatar avatar)
        {
            this.avatar = avatar;
            cameraContainer = avatar.DeepFind("Camera Container");

            cam = Camera.main;
        }

        public void OnEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        public void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
        }

        public void Update()
        {
            rotation += (Vector3)Mouse.current.delta.ReadValue() * mouseSensitivity;
            rotation.y = Mathf.Clamp(rotation.y, -90.0f, 90.0f);

            ZoomCamera();
            BobCamera();
            SwayCamera();

            DriveCamera();
        }

        private void SwayCamera()
        {
            var velocity = avatar.Movement.Velocity;
            var d1 = new Vector3
            {
                x = 0.0f,
                y = -Vector3.Dot(avatar.transform.forward, velocity),
                z = -Vector3.Dot(avatar.transform.right, velocity),
            };

            frameRotationTarget += d1 * sway;
        }

        private void BobCamera()
        {
            if (!avatar.Movement.Grounded) return;
            
            var speed = avatar.Movement.MoveSpeed;
            distance += speed * Time.deltaTime;

            var shake = new Vector3(Mathf.Cos(distance * shakeFrequency),
                -Mathf.Abs(Mathf.Sin(distance * shakeFrequency)), 0.0f);
            shake.x *= shakeAmplitude.x;
            shake.y *= shakeAmplitude.y;
            shake *= shakeAmplitude.z * speed / avatar.Movement.MaxSpeed;

            frameRotationTarget += shake;
        }

        private void ZoomCamera()
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
                shakeSmoothTime);
            frameRotation.y = Mathf.SmoothDampAngle(frameRotation.y, frameRotationTarget.y, ref frameRotationVelocity.y,
                shakeSmoothTime);
            frameRotation.z = Mathf.SmoothDampAngle(frameRotation.z, frameRotationTarget.z, ref frameRotationVelocity.z,
                shakeSmoothTime);
            
            var rotation = this.rotation + frameRotation;

            avatar.transform.rotation = Quaternion.Euler(0.0f, rotation.x, 0.0f);
            cameraContainer.rotation = Quaternion.Euler(-rotation.y, rotation.x, rotation.z);

            cHeight = Mathf.SmoothDamp(cHeight, HeightOffset, ref vHeight, fovSmoothTime);
            cameraContainer.localPosition = new Vector3(0.0f, 1.8f + cHeight, 0.0f);

            cam.transform.position = cameraContainer.transform.position;
            cam.transform.rotation = cameraContainer.transform.rotation;

            cam.fieldOfView = fov;
            HeightOffset = 0.0f;

            frameRotationTarget = Vector3.zero;
        }
    }
}