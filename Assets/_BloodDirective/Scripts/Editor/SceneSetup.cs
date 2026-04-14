using UnityEditor;
using UnityEngine;
using Unity.AI.Navigation;

/// <summary>
/// One-click scene setup for Blood Directive.
/// Menu: BloodDirective/Setup Scene
/// Handles layers, colliders, NavMesh, camera target, and PlayerController layer masks.
/// </summary>
public static class SceneSetup
{
    private const string GroundLayerName = "Ground";
    private const string EnemyLayerName  = "Enemy";

    [MenuItem("BloodDirective/Setup Scene")]
    public static void SetupScene()
    {
        bool dirty = false;

        // ── 1. Ensure layers exist ────────────────────────────────────────────

        int groundLayer = EnsureLayer(GroundLayerName);
        int enemyLayer  = EnsureLayer(EnemyLayerName);

        if (groundLayer < 0 || enemyLayer < 0)
        {
            Debug.LogError("[SceneSetup] Could not create layers — all 32 user slots may be full.");
            return;
        }

        // ── 2. Find or create the ground plane ───────────────────────────────

        GameObject plane = FindGround();
        if (plane == null)
        {
            plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "Ground";
            plane.transform.localScale = new Vector3(5f, 1f, 5f);
            Undo.RegisterCreatedObjectUndo(plane, "Create Ground Plane");
            Debug.Log("[SceneSetup] Created Ground plane (10x10 units).");
            dirty = true;
        }

        if (plane.layer != groundLayer)
        {
            Undo.RecordObject(plane, "Set Ground Layer");
            plane.layer = groundLayer;
            dirty = true;
        }

        // Ensure it has a collider so raycasts can hit it.
        if (plane.GetComponent<Collider>() == null)
        {
            plane.AddComponent<MeshCollider>();
            dirty = true;
        }

        // ── 3. Bake NavMesh ───────────────────────────────────────────────────

        NavMeshSurface surface = Object.FindObjectOfType<NavMeshSurface>();
        if (surface == null)
        {
            surface = new GameObject("NavMeshSurface").AddComponent<NavMeshSurface>();
            Undo.RegisterCreatedObjectUndo(surface.gameObject, "Create NavMeshSurface");
            dirty = true;
        }

        surface.BuildNavMesh();
        EditorUtility.SetDirty(surface);
        Debug.Log("[SceneSetup] NavMesh baked.");

        // ── 4. Wire up PlayerController layer masks ───────────────────────────

        var controller = Object.FindObjectOfType<BloodDirective.Player.PlayerController>();
        if (controller != null)
        {
            SerializedObject so = new SerializedObject(controller);

            SerializedProperty groundProp = so.FindProperty("_groundLayer");
            SerializedProperty enemyProp  = so.FindProperty("_enemyLayer");

            if (groundProp != null) groundProp.intValue = 1 << groundLayer;
            if (enemyProp  != null) enemyProp.intValue  = 1 << enemyLayer;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);
            Debug.Log("[SceneSetup] PlayerController layer masks set.");
            dirty = true;
        }
        else
        {
            Debug.LogWarning("[SceneSetup] No PlayerController found in scene — add your player GameObject and run Setup Scene again.");
        }

        // ── 5. Wire CameraController target ──────────────────────────────────

        var cam = Object.FindObjectOfType<BloodDirective.Systems.CameraController>();
        if (cam != null)
        {
            var player = Object.FindObjectOfType<BloodDirective.Player.PlayerCharacter>();
            if (player != null)
            {
                SerializedObject so     = new SerializedObject(cam);
                SerializedProperty prop = so.FindProperty("_target");
                if (prop != null && prop.objectReferenceValue == null)
                {
                    prop.objectReferenceValue = player.transform;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(cam);
                    Debug.Log("[SceneSetup] CameraController target set to player.");
                    dirty = true;
                }
            }
        }

        // ── 6. Save ───────────────────────────────────────────────────────────

        if (dirty)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
            Debug.Log("[SceneSetup] Scene setup complete. Press Ctrl+S to save.");
        }
        else
        {
            Debug.Log("[SceneSetup] Everything already configured — nothing to do.");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Returns the layer index for the given name, creating it if needed.</summary>
    private static int EnsureLayer(string layerName)
    {
        int existing = LayerMask.NameToLayer(layerName);
        if (existing >= 0) return existing;

        // Write into TagManager asset.
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

        SerializedProperty layers = tagManager.FindProperty("layers");
        for (int i = 8; i < layers.arraySize; i++) // 0-7 are built-in
        {
            SerializedProperty slot = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(slot.stringValue))
            {
                slot.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                Debug.Log($"[SceneSetup] Created layer '{layerName}' at index {i}.");
                return i;
            }
        }

        return -1; // No free slot.
    }

    /// <summary>Finds the ground plane by layer name or by the name "Ground" or "Plane".</summary>
    private static GameObject FindGround()
    {
        int groundLayer = LayerMask.NameToLayer(GroundLayerName);

        foreach (GameObject go in Object.FindObjectsOfType<GameObject>())
        {
            if (go.layer == groundLayer) return go;
            string n = go.name.ToLower();
            if ((n.Contains("ground") || n.Contains("plane")) && go.GetComponent<MeshRenderer>() != null)
                return go;
        }

        return null;
    }
}
