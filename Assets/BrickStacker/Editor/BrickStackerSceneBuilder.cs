using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BrickStackerSceneBuilder
{
    const string MenuScenePath = "Assets/BrickStacker/Scenes/BrickMenu.unity";
    const string GameScenePath = "Assets/BrickStacker/Scenes/BrickGame.unity";

    [InitializeOnLoadMethod]
    public static void EnsureScenesExist()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (!System.IO.File.Exists(MenuScenePath) || !System.IO.File.Exists(GameScenePath))
                RebuildScenes();
        };
    }

    [MenuItem("Brick Stacker/Rebuild Scenes")]
    public static void RebuildScenes()
    {
        BuildMenuScene();
        BuildGameScene();
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MenuScenePath, true),
            new EditorBuildSettingsScene(GameScenePath, true)
        };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(MenuScenePath);
        Debug.Log("Brick Stacker scenes rebuilt. Press Play from BrickMenu.");
    }

    static void BuildMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "BrickMenu";
        CreateCamera(new Vector3(0, 0, -10), 5.4f, new Color(0.33f, 0.15f, 0.055f));
        var root = new GameObject("Menu Controller");
        root.AddComponent<MenuController>();
        EditorSceneManager.SaveScene(scene, MenuScenePath);
    }

    static void BuildGameScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "BrickGame";
        CreateCamera(new Vector3(0.95f, -0.35f, -10), 11.55f, new Color(0.33f, 0.15f, 0.055f));
        var root = new GameObject("Brick Game Controller");
        root.AddComponent<BrickGameController>();
        EditorSceneManager.SaveScene(scene, GameScenePath);
    }

    static void CreateCamera(Vector3 position, float size, Color background)
    {
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = position;
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = size;
        camera.backgroundColor = background;
        cameraObject.AddComponent<AudioListener>();
    }
}
