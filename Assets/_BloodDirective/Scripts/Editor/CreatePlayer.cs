using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using BloodDirective.Player;
using BloodDirective.Combat;
using BloodDirective.Systems;
using BloodDirective.Data;

/// <summary>
/// Creates a fully configured Player GameObject and bakes the NavMesh.
/// Menu: BloodDirective/Create Player
/// </summary>
public static class CreatePlayer
{
    [MenuItem("BloodDirective/Create Player")]
    public static void Run()
    {
        Debug.Log("=== [CreatePlayer] Starting ===");

        // ── 1. Find the Ground plane ──────────────────────────────────────────

        GameObject ground = FindGround();
        if (ground == null)
        {
            Debug.LogError("[CreatePlayer] Could not find a Ground plane in the scene. Make sure a Plane object exists.");
            return;
        }
        Debug.Log($"[CreatePlayer] Found ground: '{ground.name}' at {ground.transform.position}");

        // Ensure it has a collider (required for both raycasts and NavMesh bake)
        if (ground.GetComponent<Collider>() == null)
        {
            ground.AddComponent<MeshCollider>();
            Debug.Log("[CreatePlayer] Added MeshCollider to ground.");
        }

        // Ensure it's on the Ground layer
        int groundLayer = EnsureLayer("Ground");
        int enemyLayer  = EnsureLayer("Enemy");
        if (ground.layer != groundLayer)
        {
            ground.layer = groundLayer;
            EditorUtility.SetDirty(ground);
            Debug.Log("[CreatePlayer] Assigned Ground layer to plane.");
        }

        // ── 2. Bake NavMesh ───────────────────────────────────────────────────

        NavMeshSurface surface = ground.GetComponent<NavMeshSurface>();
        if (surface == null)
            surface = ground.AddComponent<NavMeshSurface>();

        surface.collectObjects = CollectObjects.All;
        surface.useGeometry    = NavMeshCollectGeometry.PhysicsColliders;
        surface.BuildNavMesh();

        // Mark dirty and force-save so data persists into Play mode
        EditorUtility.SetDirty(surface);
        if (surface.navMeshData != null)
            EditorUtility.SetDirty(surface.navMeshData);

        AssetDatabase.SaveAssets();

        bool bakeOk = NavMesh.SamplePosition(ground.transform.position, out NavMeshHit hit, 10f, NavMesh.AllAreas);
        Debug.Log(bakeOk
            ? $"[CreatePlayer] NavMesh bake OK — sample point: {hit.position}"
            : "[CreatePlayer] WARNING: NavMesh.SamplePosition found nothing after bake.");

        // ── 3. Delete any existing player ────────────────────────────────────

        var existing = Object.FindObjectOfType<PlayerCharacter>();
        if (existing != null)
        {
            Debug.Log($"[CreatePlayer] Removing existing player: '{existing.gameObject.name}'");
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        // ── 4. Create Player root at the NavMesh surface ──────────────────────

        // Snap Y to the actual baked surface so the agent connects immediately
        float surfaceY = bakeOk ? hit.position.y : ground.transform.position.y;

        GameObject player = new GameObject("Player");
        Undo.RegisterCreatedObjectUndo(player, "Create Player");
        player.transform.position = new Vector3(0f, surfaceY, 0f);

        // Capsule mesh child — visual only, no collider needed on player for now
        GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.name = "Mesh";
        capsule.transform.SetParent(player.transform);
        capsule.transform.localPosition = new Vector3(0f, 1f, 0f); // capsule pivot is center, lift 1 unit so feet are at Y=0
        capsule.transform.localRotation = Quaternion.identity;
        capsule.transform.localScale    = Vector3.one;
        Object.DestroyImmediate(capsule.GetComponent<CapsuleCollider>()); // agent handles movement, no physics collider needed

        // ── 5. NavMeshAgent ───────────────────────────────────────────────────

        var agent = player.AddComponent<NavMeshAgent>();
        agent.radius          = 0.4f;
        agent.height          = 2f;
        agent.baseOffset      = 0f;
        agent.speed           = 5f;
        agent.angularSpeed    = 360f;
        agent.acceleration    = 16f;
        agent.stoppingDistance = 0.1f;
        agent.autoBraking     = true;
        Debug.Log("[CreatePlayer] NavMeshAgent added.");

        // ── 6. Game scripts ───────────────────────────────────────────────────

        player.AddComponent<PlayerCharacter>();
        player.AddComponent<CombatController>();
        var controller = player.AddComponent<PlayerController>();

        // Assign GreenBeret CharacterData
        var greenBeret = AssetDatabase.LoadAssetAtPath<CharacterData>(
            "Assets/_BloodDirective/ScriptableObjects/Classes/GreenBeret.asset");

        if (greenBeret != null)
        {
            var pcSo = new SerializedObject(player.GetComponent<PlayerCharacter>());
            pcSo.FindProperty("_characterData").objectReferenceValue = greenBeret;
            pcSo.ApplyModifiedProperties();
            Debug.Log("[CreatePlayer] GreenBeret.asset assigned to PlayerCharacter.");
        }
        else
        {
            Debug.LogWarning("[CreatePlayer] GreenBeret.asset not found — assign CharacterData manually in Inspector.");
        }

        // Set layer masks on PlayerController
        var ctrlSo = new SerializedObject(controller);
        ctrlSo.FindProperty("_groundLayer").intValue = 1 << groundLayer;
        ctrlSo.FindProperty("_enemyLayer").intValue  = 1 << enemyLayer;
        ctrlSo.ApplyModifiedProperties();
        Debug.Log("[CreatePlayer] PlayerController layer masks set.");

        // ── 7. Wire camera ────────────────────────────────────────────────────

        var cam = Object.FindObjectOfType<CameraController>();
        if (cam != null)
        {
            var camSo = new SerializedObject(cam);
            camSo.FindProperty("_target").objectReferenceValue = player.transform;
            camSo.ApplyModifiedProperties();
            EditorUtility.SetDirty(cam);
            Debug.Log("[CreatePlayer] CameraController target set to Player.");
        }
        else
        {
            Debug.LogWarning("[CreatePlayer] No CameraController in scene — camera won't follow player.");
        }

        // ── 8. Select the new player and save ────────────────────────────────

        Selection.activeGameObject = player;
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("=== [CreatePlayer] Done — scene saved. Hit Play and left-click the plane to move! ===");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static GameObject FindGround()
    {
        // Priority: object named exactly "Ground" or "Plane", then anything with a mesh
        foreach (var go in Object.FindObjectsOfType<GameObject>())
        {
            string n = go.name.ToLower();
            if ((n == "ground" || n == "plane") && go.GetComponent<MeshRenderer>() != null)
                return go;
        }
        foreach (var go in Object.FindObjectsOfType<GameObject>())
        {
            string n = go.name.ToLower();
            if ((n.Contains("ground") || n.Contains("plane")) && go.GetComponent<MeshRenderer>() != null)
                return go;
        }
        return null;
    }

    private static int EnsureLayer(string name)
    {
        int idx = LayerMask.NameToLayer(name);
        if (idx >= 0) return idx;

        var tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("layers");
        for (int i = 8; i < layers.arraySize; i++)
        {
            var slot = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(slot.stringValue))
            {
                slot.stringValue = name;
                tagManager.ApplyModifiedProperties();
                Debug.Log($"[CreatePlayer] Created layer '{name}' at slot {i}.");
                return i;
            }
        }
        Debug.LogError($"[CreatePlayer] No free layer slots for '{name}'.");
        return 0;
    }
}
