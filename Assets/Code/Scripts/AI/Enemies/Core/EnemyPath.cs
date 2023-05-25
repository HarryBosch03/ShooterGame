using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Bosch.AI.Enemies.Core
{
    public class EnemyPath
    {
        public Queue<Vector3> corners;
        
        private EnemyPath() { }

        public EnemyPath(Vector3 from, Vector3 to, int areaMask = ~0)
        {
            var path = new NavMeshPath();
            CopyFromPath(NavMesh.CalculatePath(from, to, areaMask, path) ? path : null);
        }

        public EnemyPath CopyFromPath(NavMeshPath path)
        {
            if (path == null)
            {
                corners = new Queue<Vector3>();
                return this;
            }
            
            corners = new Queue<Vector3>(path.corners);
            return this;
        }

        public static implicit operator EnemyPath(NavMeshPath path)
        {
            return new EnemyPath().CopyFromPath(path);
        }
        
        public static implicit operator bool (EnemyPath path)
        {
            if (path == null) return false;
            if (path.corners.Count == 0) return false;
            
            return true;
        }

        public static bool IsValid(EnemyPath path, Vector3 target, float threshold = 1.0f)
        {
            if (!path) return false;

            foreach (var corner in path.corners)
            {
                if ((corner - target).magnitude > threshold) continue;
                return true;
            }

            return false;
        }
    }
}