using UnityEditor;
using UnityEngine;
using Unity.AI.Navigation;

public class NavMeshBaker
{
    [MenuItem("BloodDirective/Bake NavMesh")]
    public static void BakeNavMesh()
    {
        NavMeshSurface surface = Object.FindObjectOfType<NavMeshSurface>();
        if (surface == null)
        {
            Debug.LogError("No NavMeshSurface found in scene!");
            return;
        }
        surface.BuildNavMesh();
        Debug.Log("NavMesh baked successfully!");
    }
}
