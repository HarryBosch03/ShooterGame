using UnityEngine;
using UnityEngine.InputSystem;

namespace Bosch.Player
{
    [SelectionBase]
    [DisallowMultipleComponent]
    public sealed class PlayerAvatar : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputAsset;

        [SerializeField] private PlayerMovement movement;
        [SerializeField] private new PlayerCamera camera;

        public InputActionAsset InputAsset => inputAsset;

        public PlayerMovement Movement => movement;
        public PlayerCamera Camera => camera;
        
        private void Awake()
        {
            inputAsset.Enable();

            movement.Initialize(this);
            camera.Initialize(this);
        }

        private void OnEnable()
        {
            camera.OnEnable();
        }

        private void OnDisable()
        {
            camera.OnDisable();
        }

        private void Update()
        {
            movement.Update();
            camera.Update();
        }

        private void FixedUpdate()
        {
            movement.FixedUpdate();
        }
    }
}
