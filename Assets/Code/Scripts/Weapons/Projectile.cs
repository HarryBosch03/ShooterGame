using UnityEngine;
using Random = UnityEngine.Random;

namespace Bosch.Weapons
{
    public class Projectile : MonoBehaviour
    {
        private int damage;
        private float lifetime;
        private float awakeTime;

        private GameObject hitEffect;

        private new Rigidbody rigidbody;

        private void Awake()
        {
            rigidbody = gameObject.GetOrAddComponent<Rigidbody>();

            hitEffect = transform.DeepFind("Hit Effect").gameObject;
            if (hitEffect) hitEffect.SetActive(false);

            awakeTime = Time.time;
        }

        private void FixedUpdate()
        {
            Collide();
        }

        private void Collide()
        {
            if (Time.time - awakeTime > lifetime)
            {
                Destroy(gameObject);
                return;
            }
            
            var ray = new Ray(rigidbody.position, rigidbody.velocity);
            var speed = rigidbody.velocity.magnitude;
            if (!Physics.Raycast(ray, out var hit, speed * Time.deltaTime * 1.01f)) return;

            if (hitEffect)
            {
                hitEffect.transform.SetParent(null);
                hitEffect.transform.position = hit.point;
                hitEffect.transform.rotation = Quaternion.LookRotation(Vector3.Reflect(ray.direction, hit.normal));
                hitEffect.SetActive(true);
                Destroy(hitEffect, 10.0f);
            }

            Destroy(gameObject);
        }

        public static void Spawn(WeaponData profile, Transform muzzle)
        {
            var angle = Random.value * 2.0f * Mathf.PI;
            var length = Random.value;
            var x = Mathf.Cos(angle) * length;
            var y = Mathf.Sin(angle) * length;
            var pitch = Mathf.Asin(x * Mathf.Sin(profile.maxSprayAngle * Mathf.Deg2Rad)) * Mathf.Rad2Deg;
            var yaw = Mathf.Asin(y * Mathf.Sin(profile.maxSprayAngle * Mathf.Deg2Rad)) * Mathf.Rad2Deg;

            var position = muzzle.position;
            var rotation = muzzle.rotation * Quaternion.Euler(yaw, pitch, yaw);
            var instance = Instantiate(profile.projectilePrefab, position, rotation);

            instance.rigidbody.mass = 0.0f;
            instance.rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            instance.rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

            instance.rigidbody.velocity = rotation * Vector3.forward * profile.projectileSpeed;
            instance.damage = profile.projectileDamage;
            instance.lifetime = profile.projectileLifetime;
        }
    }
}