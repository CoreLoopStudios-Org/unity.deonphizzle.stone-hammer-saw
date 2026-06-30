using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class CleanUpVFX
{
    static CleanUpVFX()
    {
        // CleanAndFix();
    }

    [MenuItem("Tools/Clean Up VFX")]
    public static void CleanAndFixMenu()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Mob Squad 3d world scene.unity");
        CleanAndFix();
    }

    private static void CleanAndFix()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[CleanUpVFX] Editor is in Play Mode. Stopping Play Mode first...");
            EditorApplication.isPlaying = false;
            return;
        }

        Debug.LogWarning("[CleanUpVFX] Initiating clean and fix on scene: " + EditorSceneManager.GetActiveScene().name);

        // 1. Fix Canvas plane distance
        GameObject canvasObj = GameObject.Find("Canvas");
        if (canvasObj != null)
        {
            Canvas canvas = canvasObj.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.planeDistance = 1.0f;
                EditorUtility.SetDirty(canvas);
                Debug.LogWarning("[CleanUpVFX] Set Canvas planeDistance to 1.0f");
            }
        }

        // 2. Fix Particle System velocity over lifetime mode mismatch
        GameObject particlesObj = GameObject.Find("SpinParticles");
        if (particlesObj != null)
        {
            ParticleSystem ps = particlesObj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var vel = ps.velocityOverLifetime;
                // Set constant velocity downwards instead of random range to fix mode mismatch exception
                vel.y = new ParticleSystem.MinMaxCurve(-20f); 
                EditorUtility.SetDirty(ps);
                EditorUtility.SetDirty(particlesObj);
                Debug.LogWarning("[CleanUpVFX] Fixed SpinParticles velocity over lifetime mode mismatch");
            }
        }

        // 3. Ensure both SquidGameManager and MobSquadGameManager are on the manager GameObject
        GameObject managerObj = GameObject.Find("MobSquadGameManager");
        if (managerObj != null)
        {
            SquidGameManager squidMgr = managerObj.GetComponent<SquidGameManager>();
            if (squidMgr == null)
            {
                squidMgr = managerObj.AddComponent<SquidGameManager>();
                Debug.LogWarning("[CleanUpVFX] Added SquidGameManager component to MobSquadGameManager GameObject.");
            }

            if (squidMgr.dollMusic == null)
            {
                squidMgr.dollMusic = managerObj.GetComponent<AudioSource>();
                Debug.LogWarning("[CleanUpVFX] Auto-assigned dollMusic AudioSource to SquidGameManager.");
            }

            MobSquadGameManager mobSquadMgr = managerObj.GetComponent<MobSquadGameManager>();
            if (mobSquadMgr == null)
            {
                mobSquadMgr = managerObj.AddComponent<MobSquadGameManager>();
                Debug.LogWarning("[CleanUpVFX] Added MobSquadGameManager component to MobSquadGameManager GameObject.");
            }

            // Ensure NetworkObject is attached for Fusion simulation
            Fusion.NetworkObject netObj = managerObj.GetComponent<Fusion.NetworkObject>();
            if (netObj == null)
            {
                netObj = managerObj.AddComponent<Fusion.NetworkObject>();
                Debug.LogWarning("[CleanUpVFX] Added Fusion.NetworkObject component to MobSquadGameManager GameObject.");
            }
            EditorUtility.SetDirty(managerObj);

            // 3.5 Ensure Pangopal_01 prefab exists and is assigned
            string prefabPath = "Assets/Resources/Pangopal_01.prefab";
            GameObject pangopalSceneObj = GameObject.Find("Pangopal_01");
            if (pangopalSceneObj != null)
            {
                if (!System.IO.Directory.Exists("Assets/Resources"))
                {
                    System.IO.Directory.CreateDirectory("Assets/Resources");
                }

                GameObject prefabObj = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefabObj == null)
                {
                    GameObject tempObj = Object.Instantiate(pangopalSceneObj);
                    tempObj.name = "Pangopal_01";
                    tempObj.SetActive(true);

                    if (tempObj.GetComponent<Fusion.NetworkObject>() == null)
                    {
                        tempObj.AddComponent<Fusion.NetworkObject>();
                    }
                    if (tempObj.GetComponent<Fusion.NetworkTransform>() == null)
                    {
                        tempObj.AddComponent<Fusion.NetworkTransform>();
                    }

                    prefabObj = PrefabUtility.SaveAsPrefabAsset(tempObj, prefabPath);
                    Object.DestroyImmediate(tempObj);
                    Debug.LogWarning("[CleanUpVFX] Created Pangopal_01 prefab with NetworkObject component.");
                }

                if (mobSquadMgr != null)
                {
                    if (mobSquadMgr.playerPrefab == null || mobSquadMgr.playerPrefab == pangopalSceneObj)
                    {
                        mobSquadMgr.playerPrefab = prefabObj;
                        EditorUtility.SetDirty(mobSquadMgr);
                        Debug.LogWarning("[CleanUpVFX] Assigned Pangopal_01 prefab to playerPrefab.");
                    }
                    if (mobSquadMgr.npcPrefab == null || mobSquadMgr.npcPrefab == pangopalSceneObj)
                    {
                        mobSquadMgr.npcPrefab = prefabObj;
                        EditorUtility.SetDirty(mobSquadMgr);
                        Debug.LogWarning("[CleanUpVFX] Assigned Pangopal_01 prefab to npcPrefab.");
                    }
                }
            }
        }

        // 4. Log all children under Canvas to find the panel names
        GameObject canvasRoot = GameObject.Find("Canvas");
        if (canvasRoot != null)
        {
            foreach (Transform child in canvasRoot.transform)
            {
                Debug.LogWarning($"[CleanUpVFX] Canvas Child: {child.name} (Active: {child.gameObject.activeSelf})");
            }
        }

        // Save scene and assets
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.LogWarning("[CleanUpVFX] Saved changes successfully.");
    }
}
// Touch to recompile in Edit Mode
