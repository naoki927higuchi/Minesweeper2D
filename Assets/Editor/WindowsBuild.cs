using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Minesweeper.Editor
{
    public static class WindowsBuild
    {
        // Keep the original menu and batch entry point working.
        [MenuItem("Minesweeper/Build Windows x64")]
        public static void Build() { BuildRelease(); }

        [MenuItem("Minesweeper/Build Release (Windows x64)")]
        public static void BuildRelease()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before building.");
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string version = Argument("-releaseVersion") ?? File.ReadAllText(Path.Combine(root, "VERSION.txt")).Trim();
            ReleasePackage.ValidateVersion(version);
            string player = Path.Combine(root, "bin", "Release-" + version);
            if (Directory.Exists(player)) throw new IOException("Output already exists: " + player);
            Directory.CreateDirectory(player);

            // Apply settings only on an explicit build, never on script reload.
            PlayerSettings.companyName = "FieldNotes";
            PlayerSettings.productName = "Minesweeper";
            PlayerSettings.bundleVersion = version;
            PlayerSettings.defaultScreenWidth = 1160;
            PlayerSettings.defaultScreenHeight = 860;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Main.unity" },
                locationPathName = Path.Combine(player, "Minesweeper.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Windows build failed: " + report.summary.result);
            if ((report.summary.options & (BuildOptions.Development | BuildOptions.AllowDebugging)) != 0)
                throw new Exception("Distribution builds must not enable development or debugging.");
            Debug.Log("RELEASE_EXE=" + Path.Combine(player, "Minesweeper.exe"));
            if (!Application.isBatchMode) EditorUtility.RevealInFinder(player);
        }

        private static string Argument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
                if (args[i] == name)
                {
                    if (i + 1 >= args.Length || args[i + 1].StartsWith("-"))
                        throw new ArgumentException("Missing value for " + name);
                    return args[i + 1];
                }
            return null;
        }
    }
}
