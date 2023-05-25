using Bosch.AI.Enemies.Submodules;
using UnityEngine;

namespace Bosch.AI.Enemies.Core
{
    [SelectionBase]
    [DisallowMultipleComponent]
    public sealed class BipedalEnemy : Enemy
    {
        [SerializeField] private EnemyBipedalMovement movement;

        private void Awake()
        {
            movement.Initialize(this);
        }
    }
}
