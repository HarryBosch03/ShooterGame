using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bosch.Navigation
{
    public class NavGroup : MonoBehaviour
    {
        [SerializeField] private float voxelSize;

        private List<NavSurface> areas;
        [SerializeField] [HideInInspector] private List<NavNugget> nuggets = new();

        public float VoxelSize => voxelSize;
        public List<NavNugget> Nuggets => nuggets;
        
        private void Awake()
        {
            areas = new List<NavSurface>(GetComponentsInChildren<NavSurface>());
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1.0f, 1.0f, 0.0f, 1.0f);
            Gizmos.matrix = transform.localToWorldMatrix;

            foreach (var nugget in nuggets)
            {
                var p = new Vector3(nugget.position.x * voxelSize, nugget.height, nugget.position.y * voxelSize);
                Gizmos.DrawWireCube(p, new Vector3(voxelSize, 0.0f, voxelSize));
            }
        }

        [ContextMenu("Bake")]
        public void Bake()
        {   
            areas = new List<NavSurface>(GetComponentsInChildren<NavSurface>());
            
            nuggets.Clear();
            foreach (var area in areas)
            {
                area.Bake(this);
            }
        }
    }
}