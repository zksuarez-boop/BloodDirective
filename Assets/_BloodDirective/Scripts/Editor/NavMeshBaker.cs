using UnityEditor;
using UnityEngine;
using Unity.AI.Navigation;

public class NavMeshBaker
{
    [MenuItem("BloodDirective/Bake NavMesh")]
    public static void BakeNavMesh()
    {
        NavMeshSurface surface = Object.FindFirstObjectByType<NavMeshSurface>();

        if (surface == null)
        {
            Debug.Log("[NavMeshBaker] No NavMeshSurface found — creating one automatically.");
            var go = new GameObject("NavMeshSurface");
            surface = go.AddComponent<NavMeshSurface>();
            Undo.RegisterCreatedObjectUndo(go, "Create NavMeshSurface");
        }

        surface.BuildNavMesh();
        EditorUtility.SetDirty(surface);
        Debug.Log("[NavMeshBaker] NavMesh baked successfully!");
    }
}
