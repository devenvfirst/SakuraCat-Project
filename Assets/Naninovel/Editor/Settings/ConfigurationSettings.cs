// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;
using UnityEngine.UIElements;

namespace Naninovel
{
    /// <summary>
    /// A default editor and project settings provider for <see cref="Naninovel.Configuration"/> assets.
    /// </summary>
    public class ConfigurationSettings : SettingsProvider
    {
        protected Type ConfigurationType { get; }
        protected Configuration Configuration { get; private set; }
        protected SerializedObject SerializedObject { get; private set; }
        protected virtual string EditorTitle { get; }
        protected virtual string HelpUri { get; }
        /// <summary>
        /// Override to use custom drawers instead of the default ones in <see cref="DrawDefaultEditor()"/>.
        /// </summary>
        protected virtual Dictionary<string, Action<SerializedProperty>> OverrideConfigurationDrawers { get; }

        private const string settingsPathPrefix = "Project/Naninovel/";
        private static readonly GUIContent helpIcon;
        private static readonly Type settingsScopeType = typeof(EditorWindow).Assembly.GetType("UnityEditor.SettingsWindow+GUIScope");
        private static readonly Dictionary<Type, Type> settingsTypeMap = BuildSettingsTypeMap();

        private Dictionary<string, Action<SerializedProperty>> overrideDrawers;
        private UnityEngine.Object[] assetTargets;

        static ConfigurationSettings ()
        {
            helpIcon = new GUIContent(GUIContents.HelpIcon);
            helpIcon.tooltip = "Open naninovel guide in web browser.";
        }

        protected ConfigurationSettings (Type configType)
            : base(TypeToSettingsPath(configType), SettingsScope.Project)
        {
            Debug.Assert(typeof(Configuration).IsAssignableFrom(configType));
            ConfigurationType = configType;
            EditorTitle = ConfigurationType.Name.Replace("Configuration", string.Empty).InsertCamel();
            HelpUri = $"guide/configuration.html#{ConfigurationType.Name.Replace("Configuration", string.Empty).InsertCamel('-').ToLowerInvariant()}";
            overrideDrawers = OverrideConfigurationDrawers;
        }

        public static TConfig LoadOrDefaultAndSave<TConfig> () 
            where TConfig : Configuration, new()
        {
            var configuration = ProjectConfigurationProvider.LoadOrDefault<TConfig>();
            if (!AssetDatabase.Contains(configuration))
                SaveConfigurationObject(configuration);

            return configuration;
        }

        public override void OnActivate (string searchContext, VisualElement rootElement)
        {
            Configuration = ProjectConfigurationProvider.LoadOrDefault(ConfigurationType);
            SerializedObject = new SerializedObject(Configuration);
            keywords = GetSearchKeywordsFromSerializedObject(SerializedObject);

            // Save the asset in case it was just generated.
            if (!AssetDatabase.Contains(Configuration))
                SaveConfigurationObject(Configuration);

            assetTargets = new[] { Configuration };
        }

        public override void OnTitleBarGUI ()
        {
            const int upperMargin = 6, rightMargin = 2;

            var rect = GUILayoutUtility.GetRect(helpIcon, GUIStyles.IconButton);
            rect.y = upperMargin;
            rect.x -= rightMargin;
            PresetSelector.DrawPresetButton(rect, assetTargets);

            rect.x -= rect.width + rightMargin;
            if (GUI.Button(rect, helpIcon, GUIStyles.IconButton))
                Application.OpenURL($"https://naninovel.com/{HelpUri}");
        }

        public override void OnGUI (string searchContext)
        {
            if (SerializedObject is null || !ObjectUtils.IsValid(SerializedObject.targetObject))
            {
                EditorGUILayout.HelpBox($"{EditorTitle} configuration asset has been deleted or moved. Try re-opening the settings window or restarting the Unity editor.", MessageType.Error);
                return;
            }

            using (Activator.CreateInstance(settingsScopeType) as IDisposable)
            {
                SerializedObject.Update();
                DrawConfigurationEditor();
                SerializedObject.ApplyModifiedProperties();
            }
        }

        /// <summary>
        /// Override this method for custom configuration editors.
        /// </summary>
        protected virtual void DrawConfigurationEditor ()
        {
            DrawDefaultEditor();
        }

        /// <summary>
        /// Draws a default editor for each serializable property of the configuration object.
        /// Will skip "m_Script" property and use <see cref="OverrideConfigurationDrawers"/> instead of the default drawers.
        /// </summary>
        protected void DrawDefaultEditor ()
        {
            var property = SerializedObject.GetIterator();
            var enterChildren = true;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (property.propertyPath == "m_Script") continue;
                if (overrideDrawers != null && overrideDrawers.ContainsKey(property.propertyPath))
                {
                    overrideDrawers[property.propertyPath]?.Invoke(property);
                    continue;
                }

                EditorGUILayout.PropertyField(property, true);
            }
        }

        protected static string TypeToSettingsPath (Type type)
        {
            return settingsPathPrefix + type.Name.Replace("Configuration", string.Empty).InsertCamel();
        }

        private static void SaveConfigurationObject (Configuration configuration)
        {
            var generatedDataPath = ProjectConfigurationProvider.LoadOrDefault<EngineConfiguration>().GeneratedDataPath;
            var dirPath = PathUtils.Combine(Application.dataPath, $"{generatedDataPath}/Resources/{ProjectConfigurationProvider.DefaultResourcesPath}");
            var assetPath = PathUtils.AbsoluteToAssetPath(PathUtils.Combine(dirPath, $"{configuration.GetType().Name}.asset"));
            System.IO.Directory.CreateDirectory(dirPath);
            AssetDatabase.CreateAsset(configuration, assetPath);
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Builds a <see cref="Naninovel.Configuration"/> to <see cref="ConfigurationSettings"/> types map based on the available implementations in the project. 
        /// When a <see cref="ConfigurationSettings{TConfig}"/> for a configuration is found, will map it, otherwise will use a base <see cref="ConfigurationSettings"/>.
        /// </summary>
        private static Dictionary<Type, Type> BuildSettingsTypeMap ()
        {
            bool IsEditorFor (Type editorType, Type configType)
            {
                var type = editorType.BaseType;
                while (type != null)
                {
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ConfigurationSettings<>) && type.GetGenericArguments()[0] == configType)
                        return true;
                    type = type.BaseType;
                }
                return false;
            }

            var configTypes = ReflectionUtils.ExportedDomainTypes.Where(t => t.IsSubclassOf(typeof(Configuration)) && !t.IsAbstract);
            var editorTypes = ReflectionUtils.ExportedDomainTypes.Where(t => t.IsSubclassOf(typeof(ConfigurationSettings)) && !t.IsAbstract);
            var typeMap = new Dictionary<Type, Type>();
            foreach (var configType in configTypes)
            {
                var editorType = editorTypes.FirstOrDefault(t => IsEditorFor(t, configType)) ?? typeof(ConfigurationSettings);
                typeMap.Add(configType, editorType);
            }

            return typeMap;
        }

        [SettingsProviderGroup]
        private static SettingsProvider[] CreateProviders ()
        {
            return settingsTypeMap
                .Select(kv => kv.Value == typeof(ConfigurationSettings) ? new ConfigurationSettings(kv.Key) : Activator.CreateInstance(kv.Value) as SettingsProvider).ToArray();
        }

        [MenuItem("Naninovel/Configuration", priority = 1)]
        private static void OpenWindow ()
        {
            var engineSettingsPath = TypeToSettingsPath(typeof(EngineConfiguration));
            SettingsService.OpenProjectSettings(engineSettingsPath);
        }
    }

    /// <summary>
    /// Derive from this class to create custom editors for <see cref="Naninovel.Configuration"/> assets.
    /// </summary>
    /// <typeparam name="TConfig">Type of the configuration asset this editor is built for.</typeparam>
    public abstract class ConfigurationSettings<TConfig> : ConfigurationSettings where TConfig : Configuration
    {
        protected new TConfig Configuration => base.Configuration as TConfig;
        protected static string SettingsPath => TypeToSettingsPath(typeof(TConfig));

        protected ConfigurationSettings ()
            : base(typeof(TConfig)) { }
    }
}
