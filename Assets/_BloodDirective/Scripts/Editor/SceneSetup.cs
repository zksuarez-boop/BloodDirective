using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

/// <summary>
/// One-click scene setup for Blood Directive.
/// Menu: BloodDirective/Setup Scene
/// </summary>
public static class SceneSetup
{
    private const string GroundLayerName = "Ground";
    private const string EnemyLayerName  = "Enemy";

    [MenuItem("BloodDirective/Setup Scene")]
    public static void SetupScene()
    {
        // ── 1. Layers ─────────────────────────────────────────────────────────

        int groundLayer = EnsureLayer(GroundLayerName);
        int enemyLayer  = EnsureLayer(EnemyLayerName);

        if (groundLayer < 0 || enemyLayer < 0)
        {
            Debug.LogError("[SceneSetup] Could not create layers — all user slots may be full.");
            return;
        }

        // ── 2. Ground plane ───────────────────────────────────────────────────

        GameObject plane = FindGround();
        if (plane == null)
        {
            plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "Ground";
            plane.transform.position   = Vector3.zero;
            plane.transform.localScale = new Vector3(5f, 1f, 5f);
            Undo.RegisterCreatedObjectUndo(plane, "Create Ground Plane");
            Debug.Log("[SceneSetup] Created Ground plane.");
        }

        if (plane.layer != groundLayer)
        {
            Undo.RecordObject(plane, "Set Ground Layer");
            plane.layer = groundLayer;
        }

        if (plane.GetComponent<Collider>() == null)
            plane.AddComponent<MeshCollider>();

        // ── 3. NavMeshSurface — attach directly to the plane ─────────────────
        // Attaching to the plane and using Volume mode is the most reliable way
        // to guarantee the plane geometry gets included in the bake.

        NavMeshSurface surface = plane.GetComponent<NavMeshSurface>();
        if (surface == null)
            surface = plane.AddComponent<NavMeshSurface>();

        surface.collectObjects = CollectObjects.All;
        surface.useGeometry    = NavMeshCollectGeometry.PhysicsColliders;
        EditorUtility.SetDirty(surface);

        surface.BuildNavMesh();

        // Verify the bake produced walkable data near the plane.
        Vector3 planeCenter = plane.transform.position;
        if (NavMesh.SamplePosition(planeCenter, out _, 2f, NavMesh.AllAreas))
            Debug.Log("[SceneSetup] NavMesh baked and verified — walkable area confirmed.");
        else
            Debug.LogWarning("[SceneSetup] NavMesh baked but SamplePosition found nothing near the plane. The blue overlay should still be visible in Scene view.");

        // ── 4. Snap player onto the NavMesh surface ───────────────────────────
        // The most common cause of "no valid NavMesh" is the player spawning
        // too far above the surface for the agent to auto-snap.

        var playerCharacter = Object.FindObjectOfType<BloodDirective.Player.PlayerCharacter>();
        if (playerCharacter != null)
        {
            GameObject playerGO = playerCharacter.gameObject;
            Undo.RecordObject(playerGO.transform, "Snap Player to NavMesh");

            // Cast down from above to find the plane surface Y.
            Vector3 above = new Vector3(playerGO.transform.position.x, 10f, playerGO.transform.position.z);
            if (Physics.Raycast(above, Vector3.down, out RaycastHit hit, 20f))
            {
                Vector3 snapped = hit.point;
                snapped.y += 0.05f; // tiny lift so the agent sits on top
                playerGO.transform.position = snapped;
                Debug.Log($"[SceneSetup] Player snapped to surface at Y={snapped.y:F3}.");
            }
            else
            {
                // Fallback: just place them at plane height.
                Vector3 pos = playerGO.transform.position;
                pos.y = plane.transform.position.y + 0.05f;
                playerGO.transform.position = pos;
                Debug.Log("[SceneSetup] Player Y-position set to plane surface (fallback).");
            }

            EditorUtility.SetDirty(playerGO);
        }

        // ── 5. PlayerController layer masks ───────────────────────────────────

        var controller = Object.FindObjectOfType<BloodDirective.Player.PlayerController>();
        if (controller != null)
        {
            SerializedObject so = new SerializedObject(controller);

            so.FindProperty("_groundLayer").intValue = 1 << groundLayer;
            so.FindProperty("_enemyLayer").intValue  = 1 << enemyLayer;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);
            Debug.Log("[SceneSetup] PlayerController layer masks assigned.");
        }
        else
        {
            Debug.LogWarning("[SceneSetup] No PlayerController found — add your player to the scene and re-run Setup Scene.");
        }

        // ── 6. CameraController target ────────────────────────────────────────

        var cam = Object.FindObjectOfType<BloodDirective.Systems.CameraController>();
        if (cam != null && playerCharacter != null)
        {
            SerializedObject so     = new SerializedObject(cam);
            SerializedProperty prop = so.FindProperty("_target");
            if (prop.objectReferenceValue == null)
            {
                prop.objectReferenceValue = playerCharacter.transform;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(cam);
                Debug.Log("[SceneSetup] CameraController target set to player.");
            }
        }

        // ── 7. Save — critical for NavMesh data to survive entering Play mode ─

        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[SceneSetup] Done — scene saved. Hit Play.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static int EnsureLayer(string layerName)
    {
        int existing = LayerMask.NameToLayer(layerName);
        if (existing >= 0) return existing;

        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

        SerializedProperty layers = tagManager.FindProperty("layers");
        for (int i = 8; i < layers.arraySize; i++)
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

    private static GameObject FindGround()
    {
        int groundLayer = LayerMask.NameToLayer(GroundLayerName);

        foreach (GameObject go in Object.FindObjectsOfType<GameObject>())
            if (go.layer == groundLayer && go.GetComponent<MeshRenderer>() != null)
                return go;

        foreach (GameObject go in Object.FindObjectsOfType<GameObject>())
        {
            string n = go.name.ToLower();
            if ((n.Contains("ground") || n.Contains("plane")) && go.GetComponent<MeshRenderer>() != null)
                return go;
        }

        return null;
    }
}
