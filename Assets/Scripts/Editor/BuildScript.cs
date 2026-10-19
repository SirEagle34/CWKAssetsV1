using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class BuildScript
{
    public static void PerformAndroidBuild()
    {
        string outputPath = Environment.GetCommandLineArgs()
            .FirstOrDefault(arg => arg.StartsWith("-customBuildPath=", StringComparison.OrdinalIgnoreCase))
            ?.Substring("-customBuildPath=".Length);

        if (string.IsNullOrEmpty(outputPath))
        {
            outputPath = Path.GetFullPath(Path.Combine(".", "test24.apk"));
        }

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            throw new Exception("No enabled scenes found in Build Settings.");
        }

        string directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
        BuildSummary summary = report.summary;

        UnityEngine.Debug.Log("[BuildScript] Android build result: " + summary.result);
        UnityEngine.Debug.Log("[BuildScript] Output: " + summary.outputPath);
        UnityEngine.Debug.Log("[BuildScript] Size: " + summary.totalSize + " bytes");
        UnityEngine.Debug.Log("[BuildScript] Time: " + summary.totalTime);

        if (summary.result != BuildResult.Succeeded)
        {
            throw new Exception("Android build failed: " + summary.result);
        }
    }
}
