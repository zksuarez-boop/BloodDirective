using UnityEngine;
using UnityEngine.InputSystem;
using BloodDirective.Combat;
using BloodDirective.Enemies;

namespace BloodDirective.Player
{
    /// <summary>
    /// Handles player input for click-to-move navigation and attack targeting.
    /// Uses Unity's new Input System — Mouse.current for button state and cursor position.
    /// Requires a <see cref="PlayerCharacter"/> and <see cref="CombatController"/> on the same GameObject.
    /// </summary>
    [RequireComponent(typeof(PlayerCharacter))]
    [RequireComponent(typeof(CombatController))]
    public class PlayerController : MonoBehaviour
    {
        // ── Serialized ────────────────────────────────────────────────────────

        [SerializeField] private LayerMask    _groundLayer;
        [SerializeField] private LayerMask    _enemyLayer;
        [SerializeField] private GameObject   _clickIndicatorPrefab;

        // ── Private ───────────────────────────────────────────────────────────

        private PlayerCharacter  _character;
        private CombatController _combat;
        private Camera           _mainCamera;
        private GameObject       _activeIndicator;

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Start()
        {
            _character  = GetComponent<PlayerCharacter>();
            _combat     = GetComponent<CombatController>();
            _mainCamera = Camera.main;
        }

        private void Update()
        {
            HandleLeftClick();
        }

        // ── Input Handlers ────────────────────────────────────────────────────

        /// <summary>
        /// Left-click: attack if cursor is over an enemy, otherwise move to ground.
        /// </summary>
        private void HandleLeftClick()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;

            Vector2 screenPos = Mouse.current.position.ReadValue();
            Ray ray = _mainCamera.ScreenPointToRay(screenPos);

            // Enemy hit — attack
            if (Physics.Raycast(ray, out RaycastHit enemyHit, Mathf.Infinity, _enemyLayer))
            {
                Debug.Log($"[PlayerController] Ray hit '{enemyHit.collider.gameObject.name}' on layer {enemyHit.collider.gameObject.layer}");
                if (enemyHit.collider.TryGetComponent<EnemyCharacter>(out var enemy))
                {
                    Debug.Log($"[PlayerController] SetTarget called on {enemy.gameObject.name}");
                    _combat.SetTarget(enemy);
                    return;
                }
                Debug.LogWarning($"[PlayerController] Hit object has no EnemyCharacter component");
            }
            else
            {
                Debug.Log($"[PlayerController] Enemy raycast missed (enemyLayer mask={_enemyLayer.value})");
            }

            // Ground hit — move
            if (Physics.Raycast(ray, out RaycastHit groundHit, Mathf.Infinity, _groundLayer))
            {
                _combat.ClearTarget();
                _character.MoveTo(groundHit.point);
                ShowClickIndicator(groundHit.point);
            }
        }

        // ── Click Indicator ───────────────────────────────────────────────────

        private void ShowClickIndicator(Vector3 position)
        {
            if (_clickIndicatorPrefab == null) return;

            if (_activeIndicator != null)
                Destroy(_activeIndicator);

            Vector3 spawnPos = position + Vector3.up * 0.05f;
            _activeIndicator = Instantiate(_clickIndicatorPrefab, spawnPos, Quaternion.identity);
            Destroy(_activeIndicator, 0.5f);
        }
    }
}
