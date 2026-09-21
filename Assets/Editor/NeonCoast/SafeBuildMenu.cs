using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NeonCoast.EditorTools
{
    public static class SafeBuildMenu
    {
        private const string OutputPath = @"C:\Users\Aksel\Documents\Neon Coast Racing - Build\Neon Coast Racing.exe";

        [MenuItem("Neon Coast/Build Current Game (Safe)")]
        public static void BuildCurrentGame()
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
                throw new InvalidOperationException("No enabled scenes were found in Build Settings.");

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });

            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"Build failed: {report.summary.result}");

            Debug.Log($"[Neon Coast] Safe Windows build completed: {OutputPath}");
            EditorUtility.RevealInFinder(OutputPath);
        }
    }
}
