using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;

public class ThirdPersonSetupHelper
{
    [MenuItem("Tools/Setup Third Person Controller")]
    public static void SetupController()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[ThirdPersonSetup] Cannot run setup while Unity is in Play Mode. Please exit Play Mode and try again.");
            EditorUtility.DisplayDialog("Warning", "Cannot run setup while Unity is in Play Mode!\n\nPlease exit Play Mode and try again.", "OK");
            return;
        }

        string fbxPath = "Assets/3d/characters/pongotest/Pangopal_01.Fbx";
        string controllerPath = "Assets/Animation/PangolinThirdPerson.controller";
        string scenePath = "Assets/Mov Squad 3d world scene.unity";

        Debug.Log("[ThirdPersonSetup] Starting automated setup...");

        // 1. Reimport Rig as Humanoid
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer != null)
        {
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.SaveAndReimport();
                Debug.Log("[ThirdPersonSetup] Character rig set to Humanoid and reimported.");
            }
            else
            {
                Debug.Log("[ThirdPersonSetup] Rig is already Humanoid.");
            }
        }
        else
        {
            Debug.LogError($"[ThirdPersonSetup] Fbx model not found at path: {fbxPath}");
            EditorUtility.DisplayDialog("Error", $"Could not find model FBX at {fbxPath}!", "OK");
            return;
        }

        // 2. Create/Retrieve Animator Controller with Blend Tree
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);

            // Add base state machine layer
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

            // Create a Blend Tree
            BlendTree blendTree;
            AnimatorState blendState = controller.CreateBlendTreeInController("Movement", out blendTree, 0);
            blendTree.blendParameter = "Speed";

            // Set motion fields to default values
            // The user will drag and drop standard humanoid Idle, Walk, and Run clips here in the editor.
            blendTree.AddChild(null, 0f);
            blendTree.AddChild(null, 1f);
            blendTree.AddChild(null, 2f);

            Debug.Log($"[ThirdPersonSetup] Created Animator Controller with Speed Blend Tree at: {controllerPath}");
        }
        else
        {
            Debug.Log($"[ThirdPersonSetup] Animator Controller already exists at: {controllerPath}");
        }

        // 3. Load target scene
        var currentScene = EditorSceneManager.GetActiveScene();
        if (currentScene.path != scenePath)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                currentScene = EditorSceneManager.OpenScene(scenePath);
                Debug.Log($"[ThirdPersonSetup] Loaded scene: {scenePath}");
            }
            else
            {
                Debug.LogWarning("[ThirdPersonSetup] Cancelled setup: Scene load refused by user.");
                return;
            }
        }

        // 4. Find Player Root GameObject
        GameObject playerRoot = GameObject.Find("Pangopal_01");
        if (playerRoot == null)
        {
            Debug.LogError("[ThirdPersonSetup] GameObject 'Pangopal_01' not found in active scene!");
            EditorUtility.DisplayDialog("Error", "GameObject 'Pangopal_01' root not found in the scene hierarchy!", "OK");
            return;
        }

        Undo.RecordObject(playerRoot, "Configure Player Root Components");

        // 5. Attach CharacterController component
        CharacterController charController = playerRoot.GetComponent<CharacterController>();
        if (charController == null)
        {
            charController = playerRoot.AddComponent<CharacterController>();
        }
        // General capsule sizes suitable for our character
        charController.center = new Vector3(0f, 0.9f, 0f);
        charController.height = 1.8f;
        charController.radius = 0.4f;

        // 6. Attach Animator and RuntimeAnimatorController
        Animator animator = playerRoot.GetComponent<Animator>();
        if (animator == null)
        {
            animator = playerRoot.AddComponent<Animator>();
        }
        animator.runtimeAnimatorController = controller;
        animator.avatar = AssetDatabase.LoadAssetAtPath<Avatar>("Assets/3d/characters/pongotest/Pangopal_01-Avatar.asset");

        // 7. Attach ThirdPersonCharacterController
        ThirdPersonCharacterController tpController = playerRoot.GetComponent<ThirdPersonCharacterController>();
        if (tpController == null)
        {
            tpController = playerRoot.AddComponent<ThirdPersonCharacterController>();
        }

        // 7.2 Attach ProceduralHumanoidAnimator to handle bone motions procedurally
        ProceduralHumanoidAnimator proceduralAnim = playerRoot.GetComponent<ProceduralHumanoidAnimator>();
        if (proceduralAnim == null)
        {
            proceduralAnim = playerRoot.AddComponent<ProceduralHumanoidAnimator>();
        }

        // 7.5 Ensure environment colliders are configured so character doesn't fall through
        GameObject backgroundObj = GameObject.Find("background");
        if (backgroundObj != null)
        {
            MeshCollider bgCollider = backgroundObj.GetComponent<MeshCollider>();
            if (bgCollider == null)
            {
                bgCollider = backgroundObj.AddComponent<MeshCollider>();
                Debug.Log("[ThirdPersonSetup] Added MeshCollider to background terrain GameObject.");
            }
        }
        else
        {
            Debug.LogWarning("[ThirdPersonSetup] GameObject 'background' not found! Make sure your terrain has a collider.");
        }

        GameObject groundObj = GameObject.Find("Ground");
        if (groundObj != null && !groundObj.activeSelf)
        {
            groundObj.SetActive(true);
            Debug.Log("[ThirdPersonSetup] Enabled 'Ground' GameObject as a secondary collision floor.");
        }

        // 8. Find and configure Main Camera
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camObj = GameObject.Find("Main Camera");
            if (camObj != null)
            {
                mainCam = camObj.GetComponent<Camera>();
            }
        }

        if (mainCam != null)
        {
            GameObject camObj = mainCam.gameObject;
            Undo.RecordObject(camObj, "Detach and Setup Camera");

            // Detach camera from character joints
            if (camObj.transform.parent != null)
            {
                camObj.transform.parent = null;
                Debug.Log("[ThirdPersonSetup] Detached Main Camera from bone hierarchy.");
            }

            // Attach ThirdPersonCameraController
            ThirdPersonCameraController cameraController = camObj.GetComponent<ThirdPersonCameraController>();
            if (cameraController == null)
            {
                cameraController = camObj.AddComponent<ThirdPersonCameraController>();
            }
            cameraController.target = playerRoot.transform;

            // Wire camera reference in character controller
            tpController.cameraTransform = camObj.transform;

            Debug.Log("[ThirdPersonSetup] Configured ThirdPersonCameraController and linked to target.");
        }
        else
        {
            Debug.LogWarning("[ThirdPersonSetup] Main Camera not found. Setup Camera script manually.");
        }

        // 9. Save scene
        EditorSceneManager.MarkSceneDirty(currentScene);
        EditorSceneManager.SaveScene(currentScene);
        Debug.Log("[ThirdPersonSetup] Scene marked dirty and saved successfully!");

        EditorUtility.DisplayDialog("Success", 
            "Successfully configured Third-Person Character Controller!\n\n" +
            "- Pangopal_01 rig is set to Humanoid.\n" +
            "- Animator Controller with Speed Blend Tree created.\n" +
            "- Camera detached and ThirdPersonCameraController attached.\n" +
            "- CharacterController and movement script attached.\n\n" +
            "Please open the new controller in Assets/Animation/ and drag/drop your Idle, Walk, and Run animation clips to complete setup.", 
            "OK");
    }
}
