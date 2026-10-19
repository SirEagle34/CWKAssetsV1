using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Post-proceso Gradle que:
/// 1. Inyecta android.useAndroidX=true en gradle.properties
/// 2. Copia res/xml/file_provider_paths.xml al proyecto generado
/// </summary>
public class AndroidGradlePropertiesFix : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 99;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        FixGradleProperties(path);
        CopyFileProviderPaths(path);
        FixKotlinDuplicateClasses(path);
        FixLauncherKotlinDuplicateClasses(path);
    }

    private void FixGradleProperties(string path)
    {
        string gradlePropertiesPath = Path.GetFullPath(Path.Combine(path, "..", "gradle.properties"));

        if (!File.Exists(gradlePropertiesPath))
        {
            Debug.LogWarning("[AndroidGradlePropertiesFix] gradle.properties no encontrado en: " + gradlePropertiesPath);
            return;
        }

        string content = File.ReadAllText(gradlePropertiesPath);
        bool modified = false;

        if (!content.Contains("android.useAndroidX=true"))
        {
            content += "\nandroid.useAndroidX=true";
            modified = true;
        }

        if (!content.Contains("android.suppressUnsupportedCompileSdk"))
        {
            content += "\nandroid.suppressUnsupportedCompileSdk=34";
            modified = true;
        }

        if (modified)
        {
            File.WriteAllText(gradlePropertiesPath, content);
            Debug.Log("[AndroidGradlePropertiesFix] gradle.properties actualizado con propiedades AndroidX.");
        }
    }

    private void CopyFileProviderPaths(string path)
    {
        // path = .../Gradle/unityLibrary
        string destDir = Path.Combine(path, "src", "main", "res", "xml");
        string destFile = Path.Combine(destDir, "file_provider_paths.xml");

        if (File.Exists(destFile))
            return;

        // Buscar el archivo fuente en Assets/Plugins/Android
        string sourceFile = Path.GetFullPath(
            Path.Combine(Application.dataPath, "Plugins", "Android", "src", "main", "res", "xml", "file_provider_paths.xml"));

        if (!File.Exists(sourceFile))
        {
            Debug.LogWarning("[AndroidGradlePropertiesFix] file_provider_paths.xml no encontrado en: " + sourceFile);
            return;
        }

        Directory.CreateDirectory(destDir);
        File.Copy(sourceFile, destFile, overwrite: true);
        Debug.Log("[AndroidGradlePropertiesFix] file_provider_paths.xml copiado a res/xml/");
    }

    private void FixKotlinDuplicateClasses(string path)
    {
        // path = .../Gradle/unityLibrary
        string gradlePath = Path.Combine(path, "build.gradle");
        if (!File.Exists(gradlePath))
            return;

        string content = File.ReadAllText(gradlePath);
        if (content.Contains("exclude group: 'org.jetbrains.kotlin', module: 'kotlin-stdlib-jdk7'") &&
            content.Contains("exclude group: 'org.jetbrains.kotlin', module: 'kotlin-stdlib-jdk8'"))
            return;

        const string block = "\nconfigurations.all {\n" +
                             "    exclude group: 'org.jetbrains.kotlin', module: 'kotlin-stdlib-jdk7'\n" +
                             "    exclude group: 'org.jetbrains.kotlin', module: 'kotlin-stdlib-jdk8'\n" +
                             "}\n";

        if (content.Contains("apply plugin: 'com.android.library'"))
        {
            content = content.Replace("apply plugin: 'com.android.library'",
                "apply plugin: 'com.android.library'" + block);
            File.WriteAllText(gradlePath, content);
            Debug.Log("[AndroidGradlePropertiesFix] Exclusion Kotlin aplicada en unityLibrary/build.gradle");
        }
    }

    private void FixLauncherKotlinDuplicateClasses(string path)
    {
        string gradlePath = Path.GetFullPath(Path.Combine(path, "..", "launcher", "build.gradle"));
        if (!File.Exists(gradlePath))
            return;

        string content = File.ReadAllText(gradlePath);
        if (content.Contains("exclude group: 'org.jetbrains.kotlin', module: 'kotlin-stdlib-jdk7'") &&
            content.Contains("exclude group: 'org.jetbrains.kotlin', module: 'kotlin-stdlib-jdk8'"))
            return;

        const string block = "\nconfigurations.all {\n" +
                             "    exclude group: 'org.jetbrains.kotlin', module: 'kotlin-stdlib-jdk7'\n" +
                             "    exclude group: 'org.jetbrains.kotlin', module: 'kotlin-stdlib-jdk8'\n" +
                             "}\n";

        if (content.Contains("apply plugin: 'com.android.application'"))
        {
            content = content.Replace("apply plugin: 'com.android.application'",
                "apply plugin: 'com.android.application'" + block);
            File.WriteAllText(gradlePath, content);
            Debug.Log("[AndroidGradlePropertiesFix] Exclusion Kotlin aplicada en launcher/build.gradle");
        }
    }
}
