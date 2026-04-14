using System;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;
using BloodDirective.Enemies;

namespace BloodDirective.Systems
{
    /// <summary>
    /// Tracks the current enemy wave and respawns it after a delay when all enemies are dead.
    /// AutoSetup configures the spawn positions and enemy data before each Play session.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private EnemyData  _enemyData;
        [SerializeField] private Vector3[]  _spawnPositions;
        [SerializeField] private float      _respawnDelay = 4f;

        private EnemyCharacter[] _wave = Array.Empty<EnemyCharacter>();
        private int              _deadCount;

        // Called by AutoSetup in edit mode to configure this spawner
        public void Configure(EnemyData data, Vector3[] positions, float respawnDelay = 4f)
        {
            _enemyData      = data;
            _spawnPositions = positions;
            _respawnDelay   = respawnDelay;
        }

        private void Start()
        {
            // Adopt all enemies already placed in the scene by AutoSetup
            var existing = FindObjectsByType<EnemyCharacter>();
            _wave     = existing;
            _deadCount = 0;

            foreach (var e in _wave)
                Subscribe(e);
        }

        private void Subscribe(EnemyCharacter enemy)
        {
            enemy.OnDeath += OnEnemyDied;
        }

        private void OnEnemyDied()
        {
            _deadCount++;
            if (_deadCount >= _wave.Length)
                _ = RespawnAsync(destroyCancellationToken);
        }

        private async Awaitable RespawnAsync(CancellationToken ct)
        {
            try
            {
                await Awaitable.WaitForSecondsAsync(_respawnDelay, ct);

                if (_enemyData == null || _spawnPositions == null || _spawnPositions.Length == 0)
                    return;

                _deadCount = 0;
                _wave      = new EnemyCharacter[_spawnPositions.Length];

                for (int i = 0; i < _spawnPositions.Length; i++)
                {
                    var ec = BuildEnemy(_spawnPositions[i]);
                    _wave[i] = ec;
                    Subscribe(ec);
                }
            }
            catch (OperationCanceledException) { }
        }

        // ── Enemy factory (runtime) ───────────────────────────────────────────

        private EnemyCharacter BuildEnemy(Vector3 position)
        {
            // Snap to NavMesh surface
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                position = hit.position;

            var root  = new GameObject("Enemy");
            root.layer = LayerMask.NameToLayer("Enemy");
            root.transform.position = position;

            // Visual capsule (no collider — parent handles it)
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Mesh";
            capsule.layer = root.layer;
            capsule.transform.SetParent(root.transform);
            capsule.transform.localPosition = new Vector3(0f, 1f, 0f);
            capsule.transform.localRotation = Quaternion.identity;
            capsule.transform.localScale    = Vector3.one;
            Destroy(capsule.GetComponent<CapsuleCollider>());

            var mat   = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.75f, 0.1f, 0.1f);
            capsule.GetComponent<Renderer>().material = mat;

            // Collider on root
            var col    = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 1f, 0f);
            col.radius = 0.5f;
            col.height = 2f;

            // NavMeshAgent — NavMesh is already baked, enable immediately
            var agent          = root.AddComponent<NavMeshAgent>();
            agent.radius       = 0.4f;
            agent.height       = 2f;
            agent.speed        = _enemyData.MoveSpeed;
            agent.angularSpeed = 360f;
            agent.acceleration = 8f;

            // Game components
            var ec = root.AddComponent<EnemyCharacter>();
            ec.Initialize(_enemyData);

            root.AddComponent<EnemyHealthBar>();
            root.AddComponent<EnemyAI>();

            return ec;
        }
    }
}
