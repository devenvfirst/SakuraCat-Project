// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.IO;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    public class AboutWindow : EditorWindow
    {
        public static string InstalledVersion { get => PlayerPrefs.GetString(installedVersionKey); set => PlayerPrefs.SetString(installedVersionKey, value); }

        private const string installedVersionKey = "Naninovel." + nameof(AboutWindow) + "." + nameof(InstalledVersion);
        private const string releaseNotesUri = "https://github.com/Elringus/NaninovelWeb/releases";
        private const string guideUri = "https://naninovel.com/guide/getting-started.html";
        private const string commandsUri = "https://naninovel.com/api/";
        private const string forumUri = "https://forum.naninovel.com";
        private const string discordUri = "https://discord.gg/BfkNqem";
        private const string supportUri = "https://naninovel.com/support/";
        private const string reviewUri = "https://assetstore.unity.com/packages/templates/systems/naninovel-visual-novel-engine-135453#reviews";

        private const int windowWidth = 365;
        private const int windowHeight = 480;

        private EngineVersion engineVersion;
        private GUIContent logoContent;

        private void OnEnable ()
        {
            engineVersion = EngineVersion.LoadFromResources();
            InstalledVersion = engineVersion.Version;
            var logoPath = PathUtils.AbsoluteToAssetPath(Path.Combine(PackagePath.EditorResourcesPath, "NaninovelLogo.png"));
            logoContent = new GUIContent(AssetDatabase.LoadAssetAtPath<Texture2D>(logoPath));
        }

        public void OnGUI ()
        {
            var rect = new Rect(5, 10, windowWidth - 10, windowHeight);
            GUILayout.BeginArea(rect);

            GUILayout.BeginHorizontal();
            GUILayout.Space(75);
            GUILayout.BeginVertical();

            var currentColor = GUI.contentColor;
            GUI.contentColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            GUILayout.Label(logoContent, GUIStyle.none);
            GUI.contentColor = currentColor;

            GUILayout.Space(3);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(110);
            EditorGUILayout.SelectableLabel($"{engineVersion.Version} build {engineVersion.Build}");
            GUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Release Notes", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("For the list of additions, changes and fixes associated with this release, see the notes on GitHub.", EditorStyles.wordWrappedLabel);
            if (GUILayout.Button("Release Notes")) Application.OpenURL(releaseNotesUri + $"/tag/{engineVersion.Version}");

            GUILayout.Space(7);

            EditorGUILayout.LabelField("Online Resources", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Please read getting started guide before using the engine. Command reference will navigate you through the available script commands. Chat with other users on our forum and discord server. To contact developer see the support page.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.BeginHorizontal();
           
            if (GUILayout.Button("Guide")) Application.OpenURL(guideUri);
            if (GUILayout.Button("Commands")) Application.OpenURL(commandsUri);
            if (GUILayout.Button("Forum")) Application.OpenURL(forumUri);
            if (GUILayout.Button("Discord")) Application.OpenURL(discordUri);
            if (GUILayout.Button("Support")) Application.OpenURL(supportUri);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(7);

            EditorGUILayout.LabelField("Rate Naninovel", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("We really hope you like Naninovel! If you feel like it, please leave a review on the Asset Store, that helps us out a lot.", EditorStyles.wordWrappedLabel);
            if (GUILayout.Button("Review On Asset Store")) Application.OpenURL(reviewUri);

            GUILayout.EndArea();
        }

        [InitializeOnLoadMethod]
        private static void FirstTimeSetup ()
        {
            EditorApplication.delayCall += ExecuteFirstTimeSetup;
        }

        private static void ExecuteFirstTimeSetup ()
        {
            // First time ever launch.
            if (string.IsNullOrWhiteSpace(InstalledVersion))
            {
                OpenWindow();
                return;
            }

            // First time after update launch.
            var engineVersion = EngineVersion.LoadFromResources();
            if (engineVersion && engineVersion.Version != InstalledVersion)
                OpenWindow();
        }

        [MenuItem("Naninovel/About", priority = 1)]
        private static void OpenWindow ()
        {
            var position = new Rect(100, 100, windowWidth, windowHeight);
            GetWindowWithRect<AboutWindow>(position, true, "About Naninovel", true);
        }
    }
}
