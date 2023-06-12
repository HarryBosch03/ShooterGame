using System.Collections.Generic;
using UnityEngine;

namespace Bosch.EQS
{
    [System.Serializable]
    public class EqsGraph
    {
        public Bounds bounds = new(Vector3.zero, new Vector3(50.0f, 10.0f, 50.0f));
        public float voxelSize = 1.0f;
        public float sampleHeight = 3.0f;
        private Vector3 seedPosition = Vector3.zero;

        public void Build()
        {
            
        }

        public bool TryGetPoint(Vector3 p, out Vector3 result)
        {
            result = Vector3.zero;
            
            var ray = new Ray(p + Vector3.up * sampleHeight, Vector3.down);
            if (!Physics.SphereCast(ray, diskSize, out var hit, sampleHeight * 2.0f)) return false;

            result = hit.point;
            return true;
        }

        public Vector2Int WorldToGridSpace(Vector3 point)
        {
            var point2D = new Vector2(point.x, point.z);
            

            return Vector2Int.RoundToInt();
        }
    }
}