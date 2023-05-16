using Bosch.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Bosch.Player
{
    [SelectionBase]
    [DisallowMultipleComponent]
    public sealed class PlayerAvatar : MonoBehaviour
    {
        [SerializeField] private PlayerInput input;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private new PlayerCamera camera;
        [SerializeField] private PlayerWeaponManager weaponManager;

        public PlayerInput Input => input;

        public PlayerMovement Movement => movement;
        public PlayerCamera Camera => camera;
        
        private void Awake()
        {
            input.Initialize(this);

            movement.Initialize(this);
            camera.Initialize(this);
            weaponManager.Initialize(this);
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
            input.Update();
            
            movement.Update();
            weaponManager.Update();
            camera.Update();
        }

        private void FixedUpdate()
        {
            movement.FixedUpdate();
        }
    }
}
