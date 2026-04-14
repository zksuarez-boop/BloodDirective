using System;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;
using BloodDirective.Player;
using BloodDirective.Enemies;
using BloodDirective.Stats;

namespace BloodDirective.Combat
{
    /// <summary>
    /// Manages the player's attack engagement: approach target → enter range → attack loop → death → XP.
    /// Sits on the same GameObject as PlayerCharacter and PlayerController.
    /// Uses async/await with Unity 6's Awaitable API for the attack loop.
    /// </summary>
    [RequireComponent(typeof(PlayerCharacter))]
    public class CombatController : MonoBehaviour
    {
        // ── Private ───────────────────────────────────────────────────────────

        private PlayerCharacter         _character;
        private NavMeshAgent            _agent;
        private EnemyCharacter          _currentTarget;
        private CancellationTokenSource _combatCts;

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            _character = GetComponent<PlayerCharacter>();
            _agent     = GetComponent<NavMeshAgent>();
        }

        private void OnDestroy()
        {
            // Ensure any running loop is cancelled when this component is destroyed.
            _combatCts?.Cancel();
            _combatCts?.Dispose();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Begins a new combat engagement against the given enemy.
        /// Cancels any active engagement first. Ignored if target is null or already dead.
        /// </summary>
        /// <param name="target">The enemy to attack.</param>
        public void SetTarget(EnemyCharacter target)
        {
            if (target == null || target.IsDead) return;

            // Cancel existing loop and unsubscribe from old target before reassigning.
            CancelCombat();

            if (_currentTarget != null)
                _currentTarget.OnDeath -= OnTargetDied;

            _currentTarget          = target;
            _currentTarget.OnDeath += OnTargetDied;

            // Create a token linked to both our manual source and the GameObject's lifetime.
            _combatCts = new CancellationTokenSource();
            var linked = CancellationTokenSource.CreateLinkedTokenSource(
                _combatCts.Token, destroyCancellationToken);

            _ = RunCombatLoopAsync(linked.Token);
        }

        /// <summary>
        /// Cancels the current attack engagement and stops NavMesh movement.
        /// Called by PlayerController when the player left-clicks to move.
        /// </summary>
        public void ClearTarget()
        {
            if (_currentTarget != null)
            {
                _currentTarget.OnDeath -= OnTargetDied;
                _currentTarget          = null;
            }

            CancelCombat();
        }

        // ── Private Helpers ───────────────────────────────────────────────────

        /// <summary>Cancels the CTS and resets the NavMesh path to stop movement.</summary>
        private void CancelCombat()
        {
            _combatCts?.Cancel();
            _combatCts?.Dispose();
            _combatCts = null;

            if (_agent != null && _agent.isActiveAndEnabled)
                _agent.ResetPath();
        }

        /// <summary>Invoked by the enemy's OnDeath event. Grants XP then clears the target.</summary>
        private void OnTargetDied()
        {
            if (_currentTarget != null)
                _character.GainXP(_currentTarget.EnemyData.XpReward);

            ClearTarget();
        }

        // ── Combat Loop ───────────────────────────────────────────────────────

        /// <summary>
        /// Async loop that drives the full attack engagement:
        ///   Phase 1 — approach until within attack range.
        ///   Phase 2 — attack at BaseAttackSpeed rate until target dies or engagement is cancelled.
        /// Exits cleanly on OperationCanceledException (normal cancellation path).
        /// </summary>
        private async Awaitable RunCombatLoopAsync(CancellationToken ct)
        {
            try
            {
                float attackRange = _character.CharacterData.BaseAttackRange;

                // ── Phase 1: Approach ─────────────────────────────────────────

                _agent.stoppingDistance = attackRange * 0.9f;

                while (_currentTarget != null &&
                       Vector3.Distance(transform.position, _currentTarget.transform.position) > attackRange)
                {
                    ct.ThrowIfCancellationRequested();
                    if (_agent.isOnNavMesh)
                        _agent.SetDestination(_currentTarget.transform.position);
                    await Awaitable.NextFrameAsync(ct);
                }

                // Stop NavMesh movement before entering attack phase.
                if (_agent.isActiveAndEnabled)
                    _agent.ResetPath();

                // ── Phase 2: Attack loop ───────────────────────────────────────

                float attackInterval = 1f / _character.CharacterData.BaseAttackSpeed;

                while (true)
                {
                    ct.ThrowIfCancellationRequested();

                    if (_currentTarget == null || _currentTarget.IsDead)
                        return;

                    ExecuteAttack();

                    await Awaitable.WaitForSecondsAsync(attackInterval, ct);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected — loop exits cleanly when ClearTarget or SetTarget(new) is called.
            }
        }

        /// <summary>
        /// Calculates and applies one attack's worth of damage to the current target.
        /// Uses StatSheet.MeleeDamage, CritChance, and CritDamage from the player's live stats.
        /// </summary>
        private void ExecuteAttack()
        {
            if (_currentTarget == null || _currentTarget.IsDead) return;

            float baseAmount  = _character.CharacterData.BaseWeaponDamage * _character.Stats.MeleeDamage;
            bool  isCrit      = UnityEngine.Random.value < _character.Stats.CritChance;
            float finalDamage = isCrit ? baseAmount * _character.Stats.CritDamage : baseAmount;

            DamageType dmgType = _character.CharacterData.DefaultDamageType;
            _currentTarget.TakeDamage(finalDamage, dmgType);

            if (isCrit)
                Debug.Log($"[CombatController] CRIT! {finalDamage:F1} {dmgType} damage to " +
                          $"{_currentTarget.EnemyData.EnemyName}");
        }
    }
}
