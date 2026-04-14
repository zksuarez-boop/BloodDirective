using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using BloodDirective.Player;
using BloodDirective.Combat;
using BloodDirective.Systems;
using BloodDirective.Data;
using BloodDirective.Enemies;
using BloodDirective.Stats;

/// <summary>
/// Automatically configures the scene every time you enter Play mode.
/// No menu clicks needed — just hit Play.
/// </summary>
[InitializeOnLoad]
public static class AutoSetup
{
    private const string EnemyAssetPath = "Assets/_BloodDirective/ScriptableObjects/Enemies/BasicEnemy.asset";

    static AutoSetup()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        // Create the BasicEnemy data asset on first compile if it doesn't exist
        EnsureEnemyAsset();
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
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

        // ── 6. Wire player layer masks ────────────────────────────────────────
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
        var cam = Object.FindFirstObjectByType<CameraController>();
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

        // ── 8. Spawn enemy if none in scene ───────────────────────────────────
        var existingEnemy = Object.FindFirstObjectByType<EnemyCharacter>();
        if (existingEnemy == null)
        {
            SpawnEnemy(ground, enemyLayer);
        }
        else
        {
            if (existingEnemy.gameObject.layer != enemyLayer)
                existingEnemy.gameObject.layer = enemyLayer;
            Debug.Log($"[AutoSetup] Found existing enemy: '{existingEnemy.gameObject.name}'");
        }

        // ── 9. Save ───────────────────────────────────────────────────────────
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[AutoSetup] Done — entering Play. Left-click to move, right-click enemy to attack.");
    }

    // ── Enemy ─────────────────────────────────────────────────────────────────

    private static void SpawnEnemy(GameObject ground, int enemyLayer)
    {
        var enemyData = AssetDatabase.LoadAssetAtPath<EnemyData>(EnemyAssetPath);
        if (enemyData == null)
        {
            Debug.LogWarning("[AutoSetup] BasicEnemy.asset missing — enemy not spawned.");
            return;
        }

        // Root at ground level, 6 units in front of player
        float surfaceY = ground.transform.position.y;
        var enemy = new GameObject("Enemy");
        Undo.RegisterCreatedObjectUndo(enemy, "AutoSetup: Create Enemy");
        enemy.layer = enemyLayer;
        enemy.transform.position = new Vector3(6f, surfaceY + 0.01f, 0f);

        // Red capsule (visual only — no collider needed, raycast hits parent)
        var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.name = "Mesh";
        capsule.layer = enemyLayer;
        capsule.transform.SetParent(enemy.transform);
        capsule.transform.localPosition = new Vector3(0f, 1f, 0f);
        capsule.transform.localRotation = Quaternion.identity;
        capsule.transform.localScale    = Vector3.one;
        Object.DestroyImmediate(capsule.GetComponent<CapsuleCollider>());

        // Red material
        var renderer = capsule.GetComponent<Renderer>();
        if (renderer != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.75f, 0.1f, 0.1f);
            renderer.sharedMaterial = mat;
        }

        // Capsule collider on root so raycasts hit the enemy (not the visual mesh)
        var col = enemy.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, 1f, 0f);
        col.radius = 0.5f;
        col.height = 2f;

        // EnemyCharacter — assign the data asset
        var ec   = enemy.AddComponent<EnemyCharacter>();
        var ecSo = new SerializedObject(ec);
        ecSo.FindProperty("_enemyData").objectReferenceValue = enemyData;
        ecSo.ApplyModifiedProperties();
        EditorUtility.SetDirty(ec);

        // Floating health bar
        enemy.AddComponent<EnemyHealthBar>();

        // Snap using ground-only mask so the ray doesn't hit the enemy's own CapsuleCollider
        int groundLayer = LayerMask.NameToLayer("Ground");
        SnapToGround(enemy, 1 << groundLayer);

        Debug.Log("[AutoSetup] Enemy spawned. Right-click it to attack.");
    }

    private static void EnsureEnemyAsset()
    {
        if (AssetDatabase.LoadAssetAtPath<EnemyData>(EnemyAssetPath) != null) return;

        var asset = ScriptableObject.CreateInstance<EnemyData>();
        // SerializedObject lets us write private [SerializeField] fields
        var so = new SerializedObject(asset);
        so.FindProperty("_enemyName")        .stringValue = "Grey Drone";
        so.FindProperty("_maxHealth")        .floatValue  = 50f;
        so.FindProperty("_baseAttackDamage") .floatValue  = 8f;
        so.FindProperty("_baseAttackSpeed")  .floatValue  = 1f;
        so.FindProperty("_baseAttackRange")  .floatValue  = 1.5f;
        so.FindProperty("_baseWeaponDamage") .floatValue  = 10f;
        so.FindProperty("_damageType")       .intValue    = (int)DamageType.Solid;
        so.FindProperty("_xpReward")         .floatValue  = 25f;
        so.FindProperty("_moveSpeed")        .floatValue  = 3f;
        so.ApplyModifiedProperties();

        AssetDatabase.CreateAsset(asset, EnemyAssetPath);
        AssetDatabase.SaveAssets();
        Debug.Log("[AutoSetup] Created BasicEnemy.asset (Grey Drone, 50 HP, 25 XP).");
    }

    // ── Player ────────────────────────────────────────────────────────────────

    private static GameObject BuildPlayer(GameObject ground, int groundLayer, int enemyLayer)
    {
        float surfaceY = ground.transform.position.y;

        var player = new GameObject("Player");
        Undo.RegisterCreatedObjectUndo(player, "AutoSetup: Create Player");
        player.transform.position = new Vector3(0f, surfaceY, 0f);

        var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.name = "Mesh";
        capsule.transform.SetParent(player.transform);
        capsule.transform.localPosition = new Vector3(0f, 1f, 0f);
        capsule.transform.localRotation = Quaternion.identity;
        capsule.transform.localScale    = Vector3.one;
        Object.DestroyImmediate(capsule.GetComponent<CapsuleCollider>());

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
            Debug.LogWarning("[AutoSetup] GreenBeret.asset not found — assign CharacterData manually in Inspector.");
        }

        var ctrlSo = new SerializedObject(controller);
        ctrlSo.FindProperty("_groundLayer").intValue = 1 << groundLayer;
        ctrlSo.FindProperty("_enemyLayer").intValue  = 1 << enemyLayer;
        ctrlSo.ApplyModifiedProperties();

        SnapToGround(player, 1 << groundLayer);

        Debug.Log("[AutoSetup] Player created.");
        return player;
    }

    // ── Shared Helpers ────────────────────────────────────────────────────────

    private static void SnapToGround(GameObject go, int groundLayerMask)
    {
        Vector3 above = new Vector3(go.transform.position.x, 10f, go.transform.position.z);
        if (Physics.Raycast(above, Vector3.down, out RaycastHit hit, 20f, groundLayerMask))
        {
            Undo.RecordObject(go.transform, "AutoSetup: Snap to Ground");
            var pos = hit.point;
            pos.y += 0.01f;
            go.transform.position = pos;
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
