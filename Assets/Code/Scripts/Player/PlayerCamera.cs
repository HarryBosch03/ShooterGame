using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Code.Scripts.Player
{
    [Serializable]
    public sealed class PlayerCamera
    {
        [SerializeField] private float mouseSensitivity = 0.3f;

        private PlayerAvatar avatar;
        private Transform cameraContainer;
        
        private Camera cam;
        private Vector2 rotation;
        
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
            rotation += Mouse.current.delta.ReadValue() * mouseSensitivity;
            rotation.y = Mathf.Clamp(rotation.y, -90.0f, 90.0f);

            DriveCamera();
        }

        private void DriveCamera()
        {
            avatar.transform.rotation = Quaternion.Euler(0.0f, rotation.x, 0.0f);
            cameraContainer.rotation = Quaternion.Euler(-rotation.y, rotation.x, 0.0f);
            
            cam.transform.position = cameraContainer.transform.position;
            cam.transform.rotation = cameraContainer.transform.rotation;
        }
    }
}
