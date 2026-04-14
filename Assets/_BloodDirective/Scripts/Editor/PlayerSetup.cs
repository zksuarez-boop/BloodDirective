using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using BloodDirective.Player;
using BloodDirective.Combat;
using BloodDirective.Systems;

/// <summary>
/// Diagnoses and rebuilds the Player GameObject and NavMesh from scratch.
/// Menu: BloodDirective/Rebuild Player + NavMesh
/// </summary>
public static class PlayerSetup
{
    [MenuItem("BloodDirective/Rebuild Player + NavMesh")]
    public static void RebuildAll()
    {
        Debug.Log("======= [PlayerSetup] Starting diagnosis =======");

        // ── Step 1: Report NavMesh state ──────────────────────────────────────

        var surfaces = Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None);
        Debug.Log($"[PlayerSetup] NavMeshSurface components in scene: {surfaces.Length}");
        foreach (var s in surfaces)
            Debug.Log($"  - '{s.gameObject.name}' navMeshData={(s.navMeshData != null ? "EXISTS" : "NULL")}");

        bool navMeshValid = NavMesh.SamplePosition(Vector3.zero, out _, 100f, NavMesh.AllAreas);
        Debug.Log($"[PlayerSetup] NavMesh.SamplePosition result: {(navMeshValid ? "FOUND" : "NOTHING — no baked NavMesh in scene")}");

        // ── Step 2: Find or create ground plane ───────────────────────────────

        GameObject plane = FindOrCreatePlane();
        Debug.Log($"[PlayerSetup] Ground plane: '{plane.name}' at {plane.transform.position}, scale {plane.transform.localScale}");

        // ── Step 3: Rebuild NavMesh on the plane ──────────────────────────────

        // Remove any stale surfaces elsewhere
        foreach (var s in Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None))
            if (s.gameObject != plane)
            {
                Undo.DestroyObjectImmediate(s.gameObject.GetComponent<NavMeshSurface>() != null
                    ? s.gameObject : s.gameObject);
            }

        NavMeshSurface surface = plane.GetComponent<NavMeshSurface>();
        if (surface == null)
            surface = Undo.AddComponent<NavMeshSurface>(plane);

        surface.collectObjects = CollectObjects.All;
        surface.useGeometry    = NavMeshCollectGeometry.PhysicsColliders;
        surface.BuildNavMesh();
        EditorUtility.SetDirty(surface);

        bool bakeOk = NavMesh.SamplePosition(plane.transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas);
        Debug.Log($"[PlayerSetup] NavMesh after bake: {(bakeOk ? $"OK — nearest point {hit.position}" : "FAILED — bake produced no data")}");

        // ── Step 4: Delete old Player, create fresh one ───────────────────────

        var existing = Object.FindFirstObjectByType<PlayerCharacter>();
        if (existing != null)
        {
            Debug.Log($"[PlayerSetup] Deleting existing player: '{existing.gameObject.name}'");
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        GameObject player = new GameObject("Player");
        Undo.RegisterCreatedObjectUndo(player, "Create Player");

        // Position flat on the plane surface
        Vector3 spawnPos = plane.transform.position + Vector3.up * 0.05f;
        player.transform.position = spawnPos;
        Debug.Log($"[PlayerSetup] Player created at {spawnPos}");

        // Required components — add in dependency order
        var agent = player.AddComponent<NavMeshAgent>();
        agent.radius       = 0.4f;
        agent.height       = 2f;
        agent.speed        = 5f;
        agent.angularSpeed = 360f;
        agent.acceleration = 16f;
        agent.stoppingDistance = 0.1f;
        agent.autoBraking  = true;

        player.AddComponent<PlayerCharacter>();
        player.AddComponent<CombatController>();
        player.AddComponent<PlayerController>();

        Debug.Log("[PlayerSetup] Added: NavMeshAgent, PlayerCharacter, CombatController, PlayerController");

        // Warn if no CharacterData assigned
        var pc = player.GetComponent<PlayerCharacter>();
        SerializedObject pcSo   = new SerializedObject(pc);
        SerializedProperty data = pcSo.FindProperty("_characterData");
        if (data.objectReferenceValue == null)
            Debug.LogWarning("[PlayerSetup] PlayerCharacter._characterData is not assigned. Assign GreenBeret.asset (or another CharacterData) in the Inspector before hitting Play.");

        // Set layer masks on PlayerController
        int groundLayer = LayerMask.NameToLayer("Ground");
        int enemyLayer  = LayerMask.NameToLayer("Enemy");
        if (groundLayer >= 0 && enemyLayer >= 0)
        {
            var ctrl = player.GetComponent<PlayerController>();
            SerializedObject ctrlSo = new SerializedObject(ctrl);
            ctrlSo.FindProperty("_groundLayer").intValue = 1 << groundLayer;
            ctrlSo.FindProperty("_enemyLayer").intValue  = 1 << enemyLayer;
            ctrlSo.ApplyModifiedProperties();
            Debug.Log("[PlayerSetup] PlayerController layer masks set.");
        }

        // ── Step 5: Wire camera ───────────────────────────────────────────────

        var cam = Object.FindFirstObjectByType<CameraController>();
        if (cam != null)
        {
            SerializedObject camSo  = new SerializedObject(cam);
            SerializedProperty tgt  = camSo.FindProperty("_target");
            tgt.objectReferenceValue = player.transform;
            camSo.ApplyModifiedProperties();
            EditorUtility.SetDirty(cam);
            Debug.Log("[PlayerSetup] CameraController target set to Player.");
        }
        else
        {
            Debug.LogWarning("[PlayerSetup] No CameraController found in scene.");
        }

        // ── Step 6: Save scene ────────────────────────────────────────────────

        EditorSceneManager.SaveOpenScenes();
        Debug.Log("======= [PlayerSetup] Done — scene saved. Assign CharacterData in Inspector, then hit Play. =======");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static GameObject FindOrCreatePlane()
    {
        // Check by name
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            string n = go.name.ToLower();
            if ((n == "ground" || n == "plane" || n.Contains("ground") || n.Contains("plane"))
                && go.GetComponent<MeshRenderer>() != null)
                return go;
        }

        // None found — create one
        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = "Ground";
        plane.transform.position   = Vector3.zero;
        plane.transform.localScale = new Vector3(5f, 1f, 5f);
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0) plane.layer = groundLayer;
        Undo.RegisterCreatedObjectUndo(plane, "Create Ground Plane");
        Debug.Log("[PlayerSetup] No ground found — created a new Plane.");
        return plane;
    }
}
