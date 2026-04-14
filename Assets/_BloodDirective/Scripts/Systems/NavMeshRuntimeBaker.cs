using UnityEngine;
using Unity.AI.Navigation;

namespace BloodDirective.Systems
{
    /// <summary>
    /// Bakes the NavMesh at runtime during Awake, before any NavMeshAgent initializes.
    /// Attach to the same GameObject as NavMeshSurface (the Ground plane).
    /// DefaultExecutionOrder(-100) ensures this runs before all other scripts.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(NavMeshSurface))]
    public class NavMeshRuntimeBaker : MonoBehaviour
    {
        private void Awake()
        {
            var surface = GetComponent<NavMeshSurface>();
            surface.BuildNavMesh();
            Debug.Log("[NavMeshRuntimeBaker] NavMesh baked at runtime.");
        }
    }
}
