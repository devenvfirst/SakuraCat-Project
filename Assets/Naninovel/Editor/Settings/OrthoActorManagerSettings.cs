// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using UnityEditor;

namespace Naninovel
{
    public abstract class OrthoActorManagerSettings<TConfig, TActor, TMeta> : ActorManagerSettings<TConfig, TActor, TMeta>
        where TConfig : OrthoActorManagerConfiguration<TMeta>
        where TActor : IActor
        where TMeta : OrthoActorMetadata
    {
        protected override Dictionary<string, Action<SerializedProperty>> OverrideMetaDrawers => new Dictionary<string, Action<SerializedProperty>> {
            [nameof(OrthoActorMetadata.DepthAlphaCutoff)] = property => { if (EditedMetadata.EnableDepthPass) EditorGUILayout.PropertyField(property); },
            [nameof(OrthoActorMetadata.CustomShader)] = property => { if (!Type.GetType(EditedMetadata?.Implementation)?.Name?.StartsWithFast("Generic") ?? false) EditorGUILayout.PropertyField(property); },
        };
    }
}
