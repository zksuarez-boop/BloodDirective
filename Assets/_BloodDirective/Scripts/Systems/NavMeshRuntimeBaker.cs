using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using BloodDirective.Player;

namespace BloodDirective.Systems
{
    /// <summary>
    /// Bakes the NavMesh at runtime before any NavMeshAgent tries to connect.
    /// Disables all agents in the scene, bakes, then re-enables them so they
    /// always attach to a valid NavMesh surface.
    /// Attach to the same GameObject as NavMeshSurface (the Ground plane).
    /// </summary>
    [DefaultExecutionOrder(-200)]
    [RequireComponent(typeof(NavMeshSurface))]
    public class NavMeshRuntimeBaker : MonoBehaviour
    {
        private void Awake()
        {
            // Step 1 — disable all agents so none try to attach before bake
            var agents = FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None);
            foreach (var a in agents)
                a.enabled = false;

            // Step 2 — bake
            var surface = GetComponent<NavMeshSurface>();
            surface.BuildNavMesh();
            Debug.Log("[NavMeshRuntimeBaker] NavMesh baked.");

            // Step 3 — re-enable agents so they attach to the fresh NavMesh
            foreach (var a in agents)
            {
                a.enabled = true;
                a.GetComponent<PlayerCharacter>()?.OnNavMeshReady();
            }

            Debug.Log($"[NavMeshRuntimeBaker] {agents.Length} agent(s) re-enabled on NavMesh.");
        }
    }
}
