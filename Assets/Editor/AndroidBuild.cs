using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Minesweeper.Editor
{
    public static class AndroidBuild
    {
        [MenuItem("Minesweeper/Build Release APK (Android ARM64)")]
        public static void BuildRelease()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string version = File.ReadAllText(Path.Combine(root, "VERSION.txt")).Trim();
            ReleasePackage.ValidateVersion(version);
            string output = Path.Combine(root, "bin", "Android", "Release-" + version, "Minesweeper-" + version + ".apk");
            if (File.Exists(output)) throw new IOException("APK already exists: " + output);
            string key = Environment.GetEnvironmentVariable("MINESWEEPER_KEYSTORE");
            string password = Environment.GetEnvironmentVariable("MINESWEEPER_KEY_PASSWORD");
            if (string.IsNullOrEmpty(key) || !File.Exists(key) || string.IsNullOrEmpty(password))
                throw new InvalidOperationException("Release signing key required. Use Build-Android.ps1.");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            PlayerSettings.companyName = "FieldNotes";
            PlayerSettings.productName = "Minesweeper";
            PlayerSettings.bundleVersion = version;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.fieldnotes.minesweeper");
            var semantic = Version.Parse(version);
            PlayerSettings.Android.bundleVersionCode = checked(semantic.Major * 10000 + semantic.Minor * 100 + semantic.Build);
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Android, Il2CppCompilerConfiguration.Release);
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = key;
            PlayerSettings.Android.keystorePass = password;
            PlayerSettings.Android.keyaliasName = "minesweeper";
            PlayerSettings.Android.keyaliasPass = password;
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;
            EditorUserBuildSettings.connectProfiler = false;
            EditorUserBuildSettings.buildWithDeepProfilingSupport = false;
#pragma warning disable CS0618
            EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Disabled;
#pragma warning restore CS0618
            try
            {
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { "Assets/Scenes/Main.unity" }, locationPathName = output,
                    target = BuildTarget.Android, options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded ||
                    (report.summary.options & (BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler)) != 0)
                    throw new Exception("Android Release build failed: " + report.summary.result);
                Debug.Log("RELEASE_APK=" + output);
            }
            finally
            {
                PlayerSettings.Android.keystorePass = "";
                PlayerSettings.Android.keyaliasPass = "";
                PlayerSettings.Android.keystoreName = "";
                PlayerSettings.Android.keyaliasName = "";
                PlayerSettings.Android.useCustomKeystore = false;
            }
        }
    }
}
