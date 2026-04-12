using UnityEngine;

namespace BloodDirective.Systems
{
    /// <summary>
    /// Isometric camera controller that smoothly follows a target transform.
    /// Attach to the main camera. Assign the player transform as the target.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        // ── Serialized ────────────────────────────────────────────────────────

        [SerializeField] private Transform _target;
        [SerializeField] private float     _height      = 15f;
        [SerializeField] private float     _distance    = 12f;
        [SerializeField] private float     _angle       = 45f;
        [SerializeField] private float     _followSpeed = 8f;

        // ── Private ───────────────────────────────────────────────────────────

        private Vector3 _offset;

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Start()
        {
            _offset = new Vector3(0f, _height, -_distance * Mathf.Cos(_angle * Mathf.Deg2Rad));

            transform.rotation = Quaternion.Euler(_angle, 0f, 0f);

            if (_target != null)
                transform.position = _target.position + _offset;
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            transform.position = Vector3.Lerp(
                transform.position,
                _target.position + _offset,
                _followSpeed * Time.deltaTime);
        }
    }
}
