using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class WeaponSelectSetupHelper
{
    [MenuItem("Tools/Stone Hammer Saw/Setup Weapon Select in 3D Scene")]
    public static void SetupWeaponSelect()
    {
        // Ensure Resources folder exists
        string resourcesFolder = "Assets/Resources";
        if (!AssetDatabase.IsValidFolder(resourcesFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        // 1. Export SledgeHammer2 FBX as a Prefab in Resources
        string hammerFBXPath = "Assets/3d/Hammer/SledgeHammer2.fbx";
        string targetHammerPrefabPath = "Assets/Resources/SledgeHammer2.prefab";
        GameObject hammerFBX = AssetDatabase.LoadAssetAtPath<GameObject>(hammerFBXPath);
        if (hammerFBX != null)
        {
            GameObject hammerPrefab = PrefabUtility.SaveAsPrefabAsset(hammerFBX, targetHammerPrefabPath);
            if (hammerPrefab != null)
            {
                Debug.Log($"[WeaponSelectSetup] Successfully exported Hammer prefab to: {targetHammerPrefabPath}");
            }
        }
        else
        {
            Debug.LogError($"[WeaponSelectSetup] SledgeHammer2 FBX not found at: {hammerFBXPath}");
        }

        // 2. Export Weapon-Select-Panel from HomeScene as Prefab in Resources
        string homeScenePath = "Assets/Scenes/HomeScene.unity";
        string targetPrefabPath = "Assets/Resources/Weapon-Select-Panel.prefab";

        Debug.Log("[WeaponSelectSetup] Opening HomeScene to extract Weapon-Select-Panel...");
        Scene homeScene = EditorSceneManager.OpenScene(homeScenePath, OpenSceneMode.Single);
        
        GameObject weaponSelectPanelGo = GameObject.Find("Weapon-Select-Panel");
        if (weaponSelectPanelGo == null)
        {
            Debug.LogError("[WeaponSelectSetup] 'Weapon-Select-Panel' not found in HomeScene!");
            EditorUtility.DisplayDialog("Setup Error", "'Weapon-Select-Panel' GameObject could not be found in HomeScene.", "OK");
            return;
        }

        // Save GameObject as a prefab
        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAssetAndConnect(weaponSelectPanelGo, targetPrefabPath, InteractionMode.UserAction);
        if (prefabAsset == null)
        {
            Debug.LogError("[WeaponSelectSetup] Failed to export Weapon-Select-Panel prefab!");
            return;
        }
        Debug.Log($"[WeaponSelectSetup] Successfully saved panel prefab to: {targetPrefabPath}");

        // 3. Open Mob Squad 3d world scene
        string targetScenePath = "Assets/Scenes/Mob Squad 3d world scene.unity";
        Debug.Log("[WeaponSelectSetup] Opening Mob Squad 3D world scene...");
        Scene targetScene = EditorSceneManager.OpenScene(targetScenePath, OpenSceneMode.Single);

        // Find Canvas
        GameObject canvasGo = GameObject.Find("Canvas");
        if (canvasGo == null)
        {
            Debug.LogError("[WeaponSelectSetup] 'Canvas' GameObject not found in 3D scene!");
            EditorUtility.DisplayDialog("Setup Error", "'Canvas' GameObject could not be found in the target scene.", "OK");
            return;
        }

        // Look for any existing Weapon-Select-Panel in the Canvas to replace it
        Transform existingPanel = canvasGo.transform.Find("Weapon-Select-Panel");
        if (existingPanel != null)
        {
            Debug.Log("[WeaponSelectSetup] Removing existing Weapon-Select-Panel from Canvas...");
            Undo.DestroyObjectImmediate(existingPanel.gameObject);
        }

        // Instantiate Prefab under Canvas
        GameObject instantiatedPanel = PrefabUtility.InstantiatePrefab(prefabAsset, canvasGo.transform) as GameObject;
        if (instantiatedPanel == null)
        {
            Debug.LogError("[WeaponSelectSetup] Failed to instantiate prefab under Canvas!");
            return;
        }
        Undo.RegisterCreatedObjectUndo(instantiatedPanel, "Instantiate Weapon Selection Panel");
        instantiatedPanel.name = "Weapon-Select-Panel";
        
        // Reset RectTransform anchors to stretch
        RectTransform rectTransform = instantiatedPanel.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;
        }

        // Hide it by default
        instantiatedPanel.SetActive(false);

        // 4. Find the Box GameObject and configure ChestOpeningSequence
        GameObject boxGo = GameObject.Find("Box");
        if (boxGo == null)
        {
            Debug.LogWarning("[WeaponSelectSetup] 'Box' GameObject not found in 3D scene! Skipping auto-configuration.");
        }
        else
        {
            ChestOpeningSequence seq = boxGo.GetComponent<ChestOpeningSequence>();
            if (seq == null)
            {
                seq = boxGo.AddComponent<ChestOpeningSequence>();
                Undo.RegisterCreatedObjectUndo(seq, "Add ChestOpeningSequence component");
            }

            Undo.RecordObject(seq, "Configure ChestOpeningSequence references");

            // Setup References
            seq.chestBox = boxGo.transform;
            
            seq.chestLid = FindChildRecursive(boxGo.transform, "chest_top");
            if (seq.chestLid == null)
            {
                seq.chestLid = FindChildRecursiveNameContains(boxGo.transform, "lid");
                if (seq.chestLid == null) seq.chestLid = FindChildRecursiveNameContains(boxGo.transform, "top");
            }

            seq.spawnPoint = FindChildRecursive(boxGo.transform, "SpawnPoint");
            if (seq.spawnPoint == null)
            {
                GameObject newSpawnPoint = new GameObject("SpawnPoint");
                newSpawnPoint.transform.SetParent(boxGo.transform);
                newSpawnPoint.transform.localPosition = new Vector3(0f, 0.4f, 0f);
                seq.spawnPoint = newSpawnPoint.transform;
            }

            seq.weaponSelectPanel = instantiatedPanel;
            
            // Assign hammer prefab from SledgeHammer2 prefab in Resources
            GameObject hammerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(targetHammerPrefabPath);
            if (hammerPrefab != null)
            {
                seq.hammerPrefab = hammerPrefab;
            }

            // Find player hand transform
            GameObject playerGo = GameObject.Find("Pangopal_01");
            if (playerGo != null)
            {
                seq.playerHandTransform = FindChildRecursive(playerGo.transform, "CC_Base_R_Hand");
            }

            Debug.Log("[WeaponSelectSetup] Box ChestOpeningSequence component configured successfully!");
        }

        // Mark the scene dirty and save it
        EditorSceneManager.MarkSceneDirty(targetScene);
        EditorSceneManager.SaveScene(targetScene);
        
        Debug.Log("[WeaponSelectSetup] Setup complete! Saved changes in Mob Squad 3d world scene.");
        EditorUtility.DisplayDialog("Setup Success", "Successfully copied Weapon-Select-Panel and configured the Box opening sequence!", "OK");
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent.name == childName) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindChildRecursive(child, childName);
            if (result != null) return result;
        }
        return null;
    }

    private static Transform FindChildRecursiveNameContains(Transform parent, string search)
    {
        if (parent.name.ToLower().Contains(search.ToLower())) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindChildRecursiveNameContains(child, search);
            if (result != null) return result;
        }
        return null;
    }
}
