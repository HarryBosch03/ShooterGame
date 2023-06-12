using UnityEngine;

namespace Bosch.Player
{
    [SelectionBase]
    [DisallowMultipleComponent]
    public sealed class ViewportCamera : MonoBehaviour
    {
        [SerializeField] private int framerate;

        private Camera cam;
        private float accumulator;

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void Start()
        {
            cam.enabled = false;
        }

        private void LateUpdate()
        {
            accumulator += Time.deltaTime;
            if (accumulator <= 1.0f / framerate) return;
            
            cam.Render();
            accumulator = -(accumulator % (1.0f / framerate));
        }
    }
}
