using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class SolarSystemSceneAutoWire
{
    static SolarSystemSceneAutoWire()
    {
        EditorApplication.delayCall += EnsureExperienceComponent;
    }

    private static void EnsureExperienceComponent()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();

        if (!activeScene.IsValid() || !activeScene.isLoaded)
        {
            return;
        }

        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            mainCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        if (mainCamera.GetComponent<SolarSystemExperience>() != null)
        {
            return;
        }

        mainCamera.gameObject.AddComponent<SolarSystemExperience>();
        EditorSceneManager.MarkSceneDirty(activeScene);
    }
}
