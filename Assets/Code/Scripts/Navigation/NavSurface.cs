using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bosch.Navigation
{
    [SelectionBase]
    [DisallowMultipleComponent]
    public sealed class NavSurface : MonoBehaviour
    {
        [SerializeField] private Bounds bounds;

        public void Bake(NavGroup group)
        {;
            var space = group.transform;
            var voxelSize = group.VoxelSize;
            var areas = group.Nuggets;
            
            var center = space.InverseTransformPoint(transform.position) + bounds.center;
            var extents = bounds.extents;
            
            var lc = Vector3Int.RoundToInt((center - extents) / voxelSize);
            var uc = Vector3Int.RoundToInt((center + extents) / voxelSize);

            for (var x = lc.x; x <= uc.x; x++)
            for (var z = lc.z; z <= uc.z; z++)
            {
                var i = new Vector2Int(x, z);
                var wp = space.TransformPoint(new Vector3(x, uc.y, z) * voxelSize);

                var ray = new Ray(wp, -space.up);
                if (!Physics.Raycast(ray, out var hit)) continue;

                var nugget = new NavNugget();
                nugget.position = i;
                nugget.height = space.InverseTransformPoint(hit.point).y;
                areas.Add(nugget);
            }
        }

        private void OnDrawGizmosSelected()
        {
            var group = GetComponentInParent<NavGroup>();
            var space = group.transform;
            var voxelSize = group.VoxelSize;
            
            var center = space.InverseTransformPoint(transform.position) + bounds.center;
            var extents = bounds.extents;

            Gizmos.color = new Color(1.0f, 0.5f, 0.0f, 1.0f);
            Gizmos.matrix = space.localToWorldMatrix;
            
            var lc = Vector3Int.RoundToInt((center - extents) / voxelSize);
            var uc = Vector3Int.RoundToInt((center + extents) / voxelSize);

            var drawBounds = new Bounds((Vector3)lc * voxelSize, Vector3.zero);
            drawBounds.Encapsulate((Vector3)uc * voxelSize);
            
            Gizmos.DrawWireCube(drawBounds.center, drawBounds.size);
        }
    }
}