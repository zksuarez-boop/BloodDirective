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
/// Automatically configures the scene every time you enter Play mode.
/// No menu clicks needed — just hit Play.
/// </summary>
[InitializeOnLoad]
public static class AutoSetup
{
    static AutoSetup()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // Run just before Unity enters Play mode
        if (state != PlayModeStateChange.ExitingEditMode) return;

        Debug.Log("[AutoSetup] Configuring scene before Play...");

        // ── 1. Layers ─────────────────────────────────────────────────────────
        int groundLayer = EnsureLayer("Ground");
        int enemyLayer  = EnsureLayer("Enemy");

        // ── 2. Find or create ground plane ────────────────────────────────────
        GameObject ground = FindGround();
        if (ground == null)
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position   = Vector3.zero;
            ground.transform.localScale = new Vector3(5f, 1f, 5f);
            Undo.RegisterCreatedObjectUndo(ground, "AutoSetup: Create Ground");
            Debug.Log("[AutoSetup] Created Ground plane.");
        }

        if (ground.layer != groundLayer)
            ground.layer = groundLayer;

        if (ground.GetComponent<Collider>() == null)
            ground.AddComponent<MeshCollider>();

        // ── 3. NavMeshSurface on ground ───────────────────────────────────────
        var surface = ground.GetComponent<NavMeshSurface>();
        if (surface == null)
            surface = ground.AddComponent<NavMeshSurface>();

        surface.collectObjects = CollectObjects.All;
        surface.useGeometry    = NavMeshCollectGeometry.PhysicsColliders;
        EditorUtility.SetDirty(surface);

        // ── 4. NavMeshRuntimeBaker on ground ──────────────────────────────────
        if (ground.GetComponent<NavMeshRuntimeBaker>() == null)
            ground.AddComponent<NavMeshRuntimeBaker>();

        // ── 5. Find or create player ──────────────────────────────────────────
        var existingCharacter = Object.FindFirstObjectByType<PlayerCharacter>();
        GameObject player;

        if (existingCharacter != null)
        {
            player = existingCharacter.gameObject;
            Debug.Log($"[AutoSetup] Found existing player: '{player.name}'");
        }
        else
        {
            player = BuildPlayer(ground, groundLayer, enemyLayer);
        }

        // Ensure player is on the NavMesh surface
        SnapPlayerToGround(player, ground);

        // ── 6. Wire layer masks ───────────────────────────────────────────────
        var ctrl = player.GetComponent<PlayerController>();
        if (ctrl != null)
        {
            var so = new SerializedObject(ctrl);
            so.FindProperty("_groundLayer").intValue = 1 << groundLayer;
            so.FindProperty("_enemyLayer").intValue  = 1 << enemyLayer;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(ctrl);
        }

        // ── 7. Wire camera ────────────────────────────────────────────────────
        var cam = Object.FindFirstObjectByType<BloodDirective.Systems.CameraController>();
        if (cam != null)
        {
            var so   = new SerializedObject(cam);
            var prop = so.FindProperty("_target");
            if (prop.objectReferenceValue == null)
            {
                prop.objectReferenceValue = player.transform;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(cam);
            }
        }

        // ── 8. Save ───────────────────────────────────────────────────────────
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[AutoSetup] Done — entering Play.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static GameObject BuildPlayer(GameObject ground, int groundLayer, int enemyLayer)
    {
        float surfaceY = ground.transform.position.y;
        NavMesh.SamplePosition(ground.transform.position, out NavMeshHit hit, 10f, NavMesh.AllAreas);
        // surfaceY from hit is only valid if there's an existing bake; runtime baker handles the rest

        var player = new GameObject("Player");
        Undo.RegisterCreatedObjectUndo(player, "AutoSetup: Create Player");
        player.transform.position = new Vector3(0f, surfaceY, 0f);

        // Visual capsule (no collider — agent handles movement)
        var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.name = "Mesh";
        capsule.transform.SetParent(player.transform);
        capsule.transform.localPosition = new Vector3(0f, 1f, 0f);
        capsule.transform.localRotation = Quaternion.identity;
        capsule.transform.localScale    = Vector3.one;
        Object.DestroyImmediate(capsule.GetComponent<CapsuleCollider>());

        // NavMeshAgent — starts disabled; NavMeshRuntimeBaker re-enables after bake
        var agent             = player.AddComponent<NavMeshAgent>();
        agent.radius          = 0.4f;
        agent.height          = 2f;
        agent.baseOffset      = 0f;
        agent.speed           = 5f;
        agent.angularSpeed    = 360f;
        agent.acceleration    = 16f;
        agent.stoppingDistance = 0.1f;
        agent.autoBraking     = true;
        agent.enabled         = false;

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
            Debug.Log("[AutoSetup] GreenBeret.asset assigned.");
        }
        else
        {
            Debug.LogWarning("[AutoSetup] GreenBeret.asset not found at Assets/_BloodDirective/ScriptableObjects/Classes/GreenBeret.asset — assign CharacterData manually.");
        }

        // Layer masks
        var ctrlSo = new SerializedObject(controller);
        ctrlSo.FindProperty("_groundLayer").intValue = 1 << groundLayer;
        ctrlSo.FindProperty("_enemyLayer").intValue  = 1 << enemyLayer;
        ctrlSo.ApplyModifiedProperties();

        Debug.Log("[AutoSetup] Player created.");
        return player;
    }

    private static void SnapPlayerToGround(GameObject player, GameObject ground)
    {
        Vector3 above = new Vector3(player.transform.position.x, 10f, player.transform.position.z);
        if (Physics.Raycast(above, Vector3.down, out RaycastHit hit, 20f))
        {
            var pos = hit.point;
            pos.y += 0.01f;
            Undo.RecordObject(player.transform, "AutoSetup: Snap Player");
            player.transform.position = pos;
        }
    }

    private static GameObject FindGround()
    {
        foreach (var go in Object.FindObjectsByType<GameObject>())
        {
            string n = go.name.ToLower();
            if ((n == "ground" || n == "plane") && go.GetComponent<MeshRenderer>() != null)
                return go;
        }
        foreach (var go in Object.FindObjectsByType<GameObject>())
        {
            string n = go.name.ToLower();
            if ((n.Contains("ground") || n.Contains("plane")) && go.GetComponent<MeshRenderer>() != null)
                return go;
        }
        return null;
    }

    private static int EnsureLayer(string layerName)
    {
        int idx = LayerMask.NameToLayer(layerName);
        if (idx >= 0) return idx;

        var tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("layers");
        for (int i = 8; i < layers.arraySize; i++)
        {
            var slot = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(slot.stringValue))
            {
                slot.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                Debug.Log($"[AutoSetup] Created layer '{layerName}' at slot {i}.");
                return i;
            }
        }
        Debug.LogError($"[AutoSetup] No free layer slot for '{layerName}'.");
        return 0;
    }
}
