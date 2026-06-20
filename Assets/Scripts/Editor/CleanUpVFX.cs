using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class CleanUpVFX
{
    static CleanUpVFX()
    {
        CleanAndFix();
    }

    [MenuItem("Tools/Clean Up VFX")]
    public static void CleanAndFixMenu()
    {
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

        Debug.LogWarning("[CleanUpVFX] Initiating clean and fix...");

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

        // Save scene and assets
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.LogWarning("[CleanUpVFX] Saved changes successfully.");
    }
}
// Touch to recompile in Edit Mode
