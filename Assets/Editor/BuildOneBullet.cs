using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class BuildRuleShot
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string BuildFolder = "Builds/RuleShot-Windows";
    private const string BuildPath = BuildFolder + "/RuleShot.exe";

    [MenuItem("Build/Build RuleShot Windows")]
    public static void BuildWindowsFromMenu()
    {
        BuildWindows(false);
    }

    public static void BuildWindowsCli()
    {
        BuildWindows(true);
    }

    private static void BuildWindows(bool exitWhenDone)
    {
        Directory.CreateDirectory(BuildFolder);

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = BuildPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        bool succeeded = report.summary.result == BuildResult.Succeeded;

        if (exitWhenDone)
        {
            EditorApplication.Exit(succeeded ? 0 : 1);
        }
    }
}
