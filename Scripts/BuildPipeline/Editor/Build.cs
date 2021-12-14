using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.IO.Compression;
using System.Reflection;
using Debug = UnityEngine.Debug;

using UnityEditor;
using UnityEditor.Build.Reporting;
using PacotePenseCre.BuildPipeline;

namespace PacotePenseCre.Editor.BuildPipeline
{
    public class Build : MonoBehaviour
    {
        public delegate void BuildCallback();

        public static void RunBuild(string[] buildScenes, BuildInfo buildInfo, BuildTarget target, BuildOptions options, BuildCallback callback)
        {
            ClearLog();
            string sceneName = buildScenes[0].Substring(buildScenes[0].LastIndexOf(@"/") + 1).Replace(".unity", "");
            string buildLocation = GetBuildDirectory(target, sceneName, buildInfo.Release);
            string buildFile = buildInfo.ApplicationName + ".exe";

            Debug.Log(string.Format("[BUILD] [{0}] Started @ {1:MM/dd/yy hh:mm:ss}", target.ToString(), DateTime.Now));

            CreateBuildFolder(buildLocation, buildFile, target);
            BuildUnityProject(buildScenes, buildInfo, target, options, buildLocation, buildFile);

            Debug.Log(string.Format("[BUILD] [{0}] Completed @ {1:MM/dd/yy hh:mm:ss}.", target.ToString(), DateTime.Now));

            callback();
        }

        public static void Zip(string[] buildScenes, BuildInfo buildInfo, BuildTarget target)
        {
            if (buildInfo.OneBuildPerScene)
            {
                for (int i = 0; i < buildScenes.Length; i++)
                {
                    string sceneName = buildScenes[i].Substring(buildScenes[i].LastIndexOf(@"/") + 1).Replace(".unity", "");
                    string buildLocation = GetBuildDirectory(target, sceneName, buildInfo.Release);
                    string zipFileName = buildInfo.ApplicationName.Replace(" ", string.Empty) + "_" + buildInfo.GetVersionName + "_" + DateTime.Now.ToString("yyyy-MM-dd_HH-MM") + ".zip";
                    string destinationArchiveFileName = Path.Combine(Directory.GetParent(buildLocation).Parent.Parent.Parent.FullName, zipFileName);
                    Debug.Log(string.Format("[BUILD] [{0}] Archiving {1} into {2}", target.ToString(), buildLocation, destinationArchiveFileName));

                    ZipFile.CreateFromDirectory(buildLocation, destinationArchiveFileName);
                }
            }
            else
            {
                string sceneName = buildScenes[0].Substring(buildScenes[0].LastIndexOf(@"/") + 1).Replace(".unity", "");
                string buildLocation = GetBuildDirectory(target, sceneName, buildInfo.Release);
                string zipFileName = buildInfo.ApplicationName.Replace(" ", string.Empty) + "_" + buildInfo.GetVersionName + "_" + DateTime.Now.ToString("yyyy-MM-dd_HH-MM") + ".zip";
                string destinationArchiveFileName = Path.Combine(Directory.GetParent(buildLocation).Parent.Parent.Parent.FullName, zipFileName);
                Debug.Log(string.Format("[BUILD] [{0}] Archiving {1} into {2}", target.ToString(), buildLocation, destinationArchiveFileName));

                ZipFile.CreateFromDirectory(buildLocation, destinationArchiveFileName);
            }
        }

        private static string GetBuildDirectory(BuildTarget target, string sceneName, bool isRelease)
        {
            string buildLocation = String.Empty;
            if (target == BuildTarget.StandaloneWindows64)
            {
                buildLocation += "Builds/Windows/";
                buildLocation += isRelease ? "Release/" : "Debug/";
                buildLocation += sceneName + "/";
            }

            return Path.GetFullPath(buildLocation);
        }

        private static void CreateBuildFolder(string buildLocation, string buildFile, BuildTarget target)
        {
            DirectoryInfo dirBuild = new DirectoryInfo(buildLocation);

            // if the directory exists, delete it.
            if (dirBuild.Exists)
            {
                Debug.Log("[BUILD] Cleaning Directory.");

                FileTools.MakeFilesWritable(dirBuild);

                if (target == BuildTarget.StandaloneWindows64)
                {
                    if (File.Exists(buildFile))
                        File.Delete(buildFile);
                }
                else
                {
                    Directory.Delete(buildLocation, true);
                }
            }

            dirBuild.Create();

            Debug.Log("[BUILD] Created: " + Path.GetFullPath(buildLocation));
        }

        private static void BuildUnityProject(string[] buildScenes, BuildInfo buildInfo, BuildTarget target, BuildOptions options, string buildLocation, string buildFile)
        {
            SetupPlayerSettings(buildInfo);
            BuildReport result = UnityEditor.BuildPipeline.BuildPlayer(buildScenes, buildLocation + buildFile, target, options);
            // error logs
            if (result && result.summary.totalErrors > 0)
            {
                Debug.LogError(result.summary);
            }
            else
            {
                Debug.Log("[BUILD] Unity Build Successful.");
            }
        }

        private static void SetupPlayerSettings(BuildInfo buildInfo)
        {
            if (!buildInfo.Release)
            {
                new DebugPlayerSettings().ApplySettings(buildInfo.ApplicationName, buildInfo.CompanyName);
            }
            else
            {
                new ReleasePlayerSettings().ApplySettings(buildInfo.ApplicationName, buildInfo.CompanyName);
            }
        }

        public static void ClearLog()
        {
            Assembly assembly = Assembly.GetAssembly(typeof(SceneView));

            Type type = assembly.GetType("UnityEditor.LogEntries");
            MethodInfo method = type.GetMethod("Clear");
            method.Invoke(new object(), null);
        }
    }
}