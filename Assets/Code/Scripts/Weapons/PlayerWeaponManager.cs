using Bosch.Player;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Bosch.Weapons
{
    [System.Serializable]
    public class PlayerWeaponManager
    {
        [SerializeField] private WeaponData[] weaponSlots;
        [SerializeField] private int currentWeaponIndex;
        [SerializeField] private float propertyValueSmoothing = 0.1f;

        private float nextFireTime;
        private float cMovement;

        private PlayerAvatar avatar;
        private Transform weaponContainer;

        private GameObject model;
        private Transform muzzle;
        private Transform cameraBone;
        private Animator animator;

        private static readonly int Movement = Animator.StringToHash("movement");

        private WeaponData Profile => currentWeaponIndex != -1 ? weaponSlots[currentWeaponIndex] : null;

        public void Initialize(PlayerAvatar avatar)
        {
            this.avatar = avatar;

            weaponContainer = this.avatar.transform.DeepFind("Weapons");

            UpdateWeaponSelection();
        }

        public void ChangeWeapon(WeaponData data)
        {
            weaponSlots[currentWeaponIndex] = data;
            UpdateWeaponSelection();
        }

        public void ChangeWeapon(int index)
        {
            currentWeaponIndex = index;
            UpdateWeaponSelection();
        }

        [ContextMenu("UpdateWeaponSelection")]
        private void UpdateWeaponSelection()
        {
            if (model) Object.Destroy(model);

            if (!Profile) return;

            model = Object.Instantiate(Profile.model, weaponContainer);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            muzzle = model.transform.DeepFind("Muzzle");
            animator = model.GetComponentInChildren<Animator>();
            cameraBone = model.transform.DeepFind("Camera");

            nextFireTime = Time.time + Profile.equipTime;
        }

        public void Update()
        {
            CheckInputs();

            if (!Profile)
            {
                cMovement = 0.0f;
                return;
            }
            
            avatar.Camera.FrameRotation *= cameraBone.localRotation * Quaternion.Euler(-90.0f, 180.0f, 0.0f);

            var tMovement = avatar.Movement.CurrentMovement;
            if (propertyValueSmoothing > 0.0f)
                cMovement += (tMovement - cMovement) / propertyValueSmoothing * Time.deltaTime;
            else cMovement = tMovement;
            animator.SetFloat(Movement, cMovement);
        }

        private void CheckInputs()
        {
            avatar.Input.Shoot.CallIfDown(Shoot);
            avatar.Input.Holster.CallIfDown(Holster);

            for (var i = 0; i < weaponSlots.Length && i < avatar.Input.EquipWeapon.Count; i++)
            {
                var i2 = i;
                avatar.Input.EquipWeapon[i].CallIfDown(() => ChangeWeapon(i2));
            }
        }

        public void Shoot()
        {
            if (!Profile) return;
            if (Time.time < nextFireTime) return;

            for (var i = 0; i < Profile.projectilesPerShot; i++)
            {
                Projectile.Spawn(Profile, muzzle);
            }

            animator.Play("Shoot", 0, 0.0f);
            nextFireTime = Time.time + 60.0f / Profile.fireRate;
        }

        public void Holster() => ChangeWeapon(-1);
    }
}