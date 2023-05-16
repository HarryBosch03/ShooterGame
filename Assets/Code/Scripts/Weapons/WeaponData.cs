using UnityEngine;

namespace Bosch.Weapons
{
    [CreateAssetMenu(menuName = "Scriptable Objects/Weapons/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        public Projectile projectilePrefab;
        public float speed = 60.0f;
        public int damage = 1;
        public int projectilesPerShot;
        [Range(0.0f, 45.0f)] public float maxSprayAngle = 0.0f;
        public float fireRate = 600.0f;
        public float equipTime = 0.5f;
        public GameObject model;
    }
}