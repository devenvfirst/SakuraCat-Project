// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using UnityEditor;

namespace Naninovel
{
    public class ResourceProviderSettings : ConfigurationSettings<ResourceProviderConfiguration>
    {
        protected override string HelpUri => "guide/resource-providers.html";

        protected override Dictionary<string, Action<SerializedProperty>> OverrideConfigurationDrawers => new Dictionary<string, Action<SerializedProperty>> {
            [nameof(ResourceProviderConfiguration.DynamicPolicySteps)] = property => { if (Configuration.ResourcePolicy == ResourcePolicy.Dynamic) EditorGUILayout.PropertyField(property); },
            [nameof(ResourceProviderConfiguration.OptimizeLoadingPriority)] = property => { if (Configuration.ResourcePolicy == ResourcePolicy.Dynamic) EditorGUILayout.PropertyField(property); },
            [nameof(ResourceProviderConfiguration.UseAddressables)] = property => {
                if (!Configuration.EnableBuildProcessing)
                {
                    EditorGUILayout.HelpBox("While the build processing is disabled, assets assigned as Naninovel resources may not be available in the build. Make sure to manually invoke `BuildProcessor.PreprocessBuild()` and `BuildProcessor.PostprocessBuild()` methods when building the game.", MessageType.Warning);
                    return;
                }

                #if ADDRESSABLES_AVAILABLE
                EditorGUILayout.PropertyField(property);
                if (!Configuration.UseAddressables)
                    EditorGUILayout.HelpBox("When `Use Addressables` is disabled, all the assets assigned as Naninovel resources and not stored in `Resources` folders will be copied and re-imported when building the player, which could significantly increase the build time.", MessageType.Warning);
                #else
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.Toggle(property.displayName, false);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.HelpBox("Consider installing the Addressable Asset System (via Unity's package manager). When the system is not available, all the assets assigned as Naninovel resources and not stored in `Resources` folders will be copied and re-imported when building the player, which could significantly increase the build time.", MessageType.Warning);
                #endif
            },
            [nameof(ResourceProviderConfiguration.AutoBuildBundles)] = property => {
                #if ADDRESSABLES_AVAILABLE
                if (Configuration.EnableBuildProcessing && Configuration.UseAddressables) EditorGUILayout.PropertyField(property);
                #endif
            },
        };
    }
}
