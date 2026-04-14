using System;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;
using BloodDirective.Player;

namespace BloodDirective.Enemies
{
    /// <summary>
    /// Simple chase-and-attack AI for enemies.
    /// Idles until the player enters aggro range, then chases and attacks at the
    /// rate defined by EnemyData.BaseAttackSpeed.
    /// Requires NavMeshAgent and EnemyCharacter on the same GameObject.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(EnemyCharacter))]
    public class EnemyAI : MonoBehaviour
    {
        [SerializeField] private float _aggroRadius = 12f;

        private NavMeshAgent    _agent;
        private EnemyCharacter  _self;
        private PlayerCharacter _player;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _self  = GetComponent<EnemyCharacter>();
        }

        private void Start()
        {
            _player = FindFirstObjectByType<PlayerCharacter>();
            _self.OnDeath += StopAI;

            var cts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            _ = RunAIAsync(cts.Token);
        }

        private void StopAI()
        {
            if (_agent != null && _agent.isOnNavMesh)
                _agent.ResetPath();
        }

        // ── AI Loop ───────────────────────────────────────────────────────────

        private async Awaitable RunAIAsync(CancellationToken ct)
        {
            // Wait one frame so the NavMesh agent has fully initialised
            await Awaitable.NextFrameAsync(ct);

            try
            {
                while (true)
                {
                    ct.ThrowIfCancellationRequested();

                    if (_self.IsDead || _player == null || !_player.IsAlive)
                    {
                        await Awaitable.WaitForSecondsAsync(0.5f, ct);
                        continue;
                    }

                    float dist        = Vector3.Distance(transform.position, _player.transform.position);
                    float attackRange = _self.EnemyData?.BaseAttackRange ?? 1.5f;

                    if (dist > _aggroRadius)
                    {
                        // Out of range — idle, check every few frames
                        if (_agent.isOnNavMesh && _agent.hasPath)
                            _agent.ResetPath();
                        await Awaitable.NextFrameAsync(ct);
                        continue;
                    }

                    if (dist > attackRange)
                    {
                        // Chase
                        _agent.stoppingDistance = attackRange * 0.9f;
                        if (_agent.isOnNavMesh)
                            _agent.SetDestination(_player.transform.position);
                        await Awaitable.NextFrameAsync(ct);
                        continue;
                    }

                    // In attack range — stop and swing
                    if (_agent.isOnNavMesh) _agent.ResetPath();

                    Attack();

                    float interval = 1f / Mathf.Max(0.1f, _self.EnemyData?.BaseAttackSpeed ?? 1f);
                    await Awaitable.WaitForSecondsAsync(interval, ct);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                Debug.LogError($"[EnemyAI] Loop error: {e}");
            }
        }

        private void Attack()
        {
            if (_player == null || !_player.IsAlive) return;

            float damage = _self.EnemyData?.BaseAttackDamage ?? 8f;
            var   type   = _self.EnemyData?.DamageType ?? BloodDirective.Stats.DamageType.Solid;
            _player.TakeDamage(damage, type);
        }
    }
}
