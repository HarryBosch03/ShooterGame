using Bosch.AI.Enemies.Core;
using UnityEngine;

namespace Bosch.AI.Enemies.Submodules
{
    [System.Serializable]
    public sealed class EnemyBipedalMovement
    {
        [SerializeField] private float moveSpeed;
        [SerializeField] private float accelerationTime;

        private BipedalEnemy enemy;
        private EnemyPath path;

        public Vector3 MovePosition { get; set; }

        public Transform Transform => enemy.transform;
        
        public void Initialize(BipedalEnemy enemy)
        {
            this.enemy = enemy;
        }

        public void FixedUpdate()
        {
            Move();
            
            ResetState();
        }

        private void Move()
        {
            UpdatePath();
        }

        private void UpdatePath()
        {
            if (EnemyPath.IsValid(path, MovePosition)) return;
            path = new EnemyPath(Transform.position, MovePosition);
        }

        private void ResetState()
        {
            
        }
    }
}