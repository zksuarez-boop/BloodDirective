using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

/// <summary>
/// One-click scene setup for Blood Directive.
/// Menu: BloodDirective/Setup Scene
/// Handles layers, colliders, NavMesh, camera target, and PlayerController layer masks.
/// Saves the scene automatically after baking so NavMesh data persists into Play mode.
/// </summary>
public static class SceneSetup
{
    private const string GroundLayerName = "Ground";
    private const string EnemyLayerName  = "Enemy";

    [MenuItem("BloodDirective/Setup Scene")]
    public static void SetupScene()
    {
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
            Debug.Log("[SceneSetup] Created Ground plane (50x50 units).");
        }

        if (plane.layer != groundLayer)
        {
            Undo.RecordObject(plane, "Set Ground Layer");
            plane.layer = groundLayer;
        }

        // Unity Plane primitives have a MeshCollider by default — add one if missing.
        if (plane.GetComponent<Collider>() == null)
            plane.AddComponent<MeshCollider>();

        // ── 3. Bake NavMesh ───────────────────────────────────────────────────
        // NavMeshSurface must be configured with Collect Objects: All Game Objects
        // so it picks up the plane regardless of which layer it's on.

        NavMeshSurface surface = Object.FindObjectOfType<NavMeshSurface>();
        if (surface == null)
        {
            var go = new GameObject("NavMeshSurface");
            surface = go.AddComponent<NavMeshSurface>();
            Undo.RegisterCreatedObjectUndo(go, "Create NavMeshSurface");
        }

        // Explicitly configure so the surface finds all geometry.
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry    = NavMeshCollectGeometry.PhysicsColliders;
        EditorUtility.SetDirty(surface);

        surface.BuildNavMesh();
        Debug.Log("[SceneSetup] NavMesh baked successfully.");

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
            Debug.Log("[SceneSetup] PlayerController layer masks assigned.");
        }
        else
        {
            Debug.LogWarning("[SceneSetup] No PlayerController found — add your player to the scene and run Setup Scene again.");
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
                }
            }
        }

        // ── 6. Save scene so NavMesh data persists into Play mode ─────────────
        // This is the critical step — without saving, the baked NavMesh data is
        // lost when entering Play mode and agents throw "no valid NavMesh" errors.

        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[SceneSetup] Scene saved. Hit Play — your player should now move.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Returns the layer index for the given name, creating it if needed.</summary>
    private static int EnsureLayer(string layerName)
    {
        int existing = LayerMask.NameToLayer(layerName);
        if (existing >= 0) return existing;

        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

        SerializedProperty layers = tagManager.FindProperty("layers");
        for (int i = 8; i < layers.arraySize; i++) // slots 0-7 are built-in
        {
            SerializedProperty slot = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(slot.stringValue))
            {
                slot.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                Debug.Log($"[SceneSetup] Created layer '{layerName}' at slot {i}.");
                return i;
            }
        }

        return -1;
    }

    /// <summary>Finds the ground plane by layer, then by name containing "ground" or "plane".</summary>
    private static GameObject FindGround()
    {
        int groundLayer = LayerMask.NameToLayer(GroundLayerName);

        foreach (GameObject go in Object.FindObjectsOfType<GameObject>())
        {
            if (go.layer == groundLayer && go.GetComponent<MeshRenderer>() != null)
                return go;
        }

        foreach (GameObject go in Object.FindObjectsOfType<GameObject>())
        {
            string n = go.name.ToLower();
            if ((n.Contains("ground") || n.Contains("plane")) && go.GetComponent<MeshRenderer>() != null)
                return go;
        }

        return null;
    }
}
