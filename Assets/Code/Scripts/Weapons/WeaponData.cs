using UnityEngine;
using UnityEngine.Serialization;

namespace Bosch.Weapons
{
    [CreateAssetMenu(menuName = "Scriptable Objects/Weapons/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        public Projectile projectilePrefab;
        [FormerlySerializedAs("speed")] public float projectileSpeed = 60.0f;
        [FormerlySerializedAs("damage")] public int projectileDamage = 1;
        public float projectileLifetime = 5.0f;
        public int projectilesPerShot;
        [Range(0.0f, 45.0f)] public float maxSprayAngle = 0.0f;
        public float fireRate = 600.0f;
        public float equipTime = 0.5f;
        public GameObject model;
    }
}