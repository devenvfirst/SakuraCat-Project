// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Linq;
using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Naninovel.Commands
{
    public abstract class ModifyActor<TActor, TState, TMeta, TConfig, TManager> : Command, Command.IPreloadable 
        where TActor : class, IActor
        where TState : ActorState<TActor>, new()
        where TMeta : ActorMetadata
        where TConfig : ActorManagerConfiguration<TMeta>, new()
        where TManager : class, IActorManager<TActor, TState, TMeta, TConfig>
    {
        /// <summary>
        /// ID of the actor to modify; specify `*` to affect all visible actors.
        /// </summary>
        public StringParameter Id;
        /// <summary>
        /// Appearance (or pose) to set for the modified actor.
        /// </summary>
        public StringParameter Appearance;
        /// <summary>
        /// Type of the [transition effect](/guide/transition-effects.md) to use (crossfade is used by default).
        /// </summary>
        public StringParameter Transition;
        /// <summary>
        /// Parameters of the transition effect.
        /// </summary>
        [ParameterAlias("params")]
        public DecimalListParameter TransitionParams;
        /// <summary>
        /// Path to the [custom dissolve](/guide/transition-effects.md#custom-transition-effects) texture (path should be relative to a `Resources` folder).
        /// Has effect only when the transition is set to `Custom` mode.
        /// </summary>
        [ParameterAlias("dissolve")]
        public StringParameter DissolveTexturePath;
        /// <summary>
        /// Visibility status to set for the modified actor.
        /// </summary>
        public BooleanParameter Visible;
        /// <summary>
        /// Position (in world space) to set for the modified actor. 
        /// Use Z-component (third member) to move (sort) by depth while in ortho mode.
        /// </summary>
        public DecimalListParameter Position;
        /// <summary>
        /// Rotation to set for the modified actor.
        /// </summary>
        public DecimalListParameter Rotation;
        /// <summary>
        /// Scale to set for the modified actor.
        /// </summary>
        public DecimalListParameter Scale;
        /// <summary>
        /// Tint color to set for the modified actor.
        /// <br/><br/>
        /// Strings that begin with `#` will be parsed as hexadecimal in the following way: 
        /// `#RGB` (becomes RRGGBB), `#RRGGBB`, `#RGBA` (becomes RRGGBBAA), `#RRGGBBAA`; when alpha is not specified will default to FF.
        /// <br/><br/>
        /// Strings that do not begin with `#` will be parsed as literal colors, with the following supported:
        /// red, cyan, blue, darkblue, lightblue, purple, yellow, lime, fuchsia, white, silver, grey, black, orange, brown, maroon, green, olive, navy, teal, aqua, magenta.
        /// </summary>
        [ParameterAlias("tint")]
        public StringParameter TintColor;
        /// <summary>
        /// Name of the easing function to use for the modification.
        /// <br/><br/>
        /// Available options: Linear, SmoothStep, Spring, EaseInQuad, EaseOutQuad, EaseInOutQuad, EaseInCubic, EaseOutCubic, EaseInOutCubic, EaseInQuart, EaseOutQuart, EaseInOutQuart, EaseInQuint, EaseOutQuint, EaseInOutQuint, EaseInSine, EaseOutSine, EaseInOutSine, EaseInExpo, EaseOutExpo, EaseInOutExpo, EaseInCirc, EaseOutCirc, EaseInOutCirc, EaseInBounce, EaseOutBounce, EaseInOutBounce, EaseInBack, EaseOutBack, EaseInOutBack, EaseInElastic, EaseOutElastic, EaseInOutElastic.
        /// <br/><br/>
        /// When not specified, will use a default easing function set in the actor's manager configuration settings.
        /// </summary>
        [ParameterAlias("easing")]
        public StringParameter EasingTypeName;
        /// <summary>
        /// Duration (in seconds) of the modification. Default value: 0.35 seconds.
        /// </summary>
        [ParameterAlias("time")]
        public DecimalParameter Duration = .35f;

        protected virtual string AssignedId => Id;
        protected virtual string AssignedTransition => Transition;
        protected virtual string AssignedAppearance => Pose?.Appearance ?? Appearance;
        protected virtual bool? AssignedVisibility => Assigned(Visible) ? Visible.Value : Pose != null ? Pose?.Visible : ActorManager.Configuration.AutoShowOnModify ? (bool?)true : null;
        protected virtual float?[] AssignedPosition => Assigned(Position) ? Position : Pose != null ? new float?[] { Pose.Position.x, Pose.Position.y, Pose.Position.z } : null;
        protected virtual float?[] AssignedRotation => Assigned(Rotation) ? Rotation : Pose != null ? new float?[] { Pose.Rotation.eulerAngles.x, Pose.Rotation.eulerAngles.y, Pose.Rotation.eulerAngles.z } : null;
        protected virtual float?[] AssignedScale => Assigned(Scale) ? Scale : Pose != null ? new float?[] { Pose.Scale.x, Pose.Scale.y, Pose.Scale.z } : null;
        protected virtual Color? AssignedTintColor => Assigned(TintColor) ? ParseColor(TintColor) : Pose?.TintColor;
        protected virtual TState Pose => ActorManager.Configuration.GetMetadataOrDefault(Id).GetPoseOrNull<TState>(Appearance);
        protected virtual TManager ActorManager => Engine.GetService<TManager>();
        protected virtual bool AllowPreload => Assigned(Id) && !Id.DynamicValue && Assigned(Appearance) && !Appearance.DynamicValue;

        private Texture2D preloadedDissolveTexture;

        public virtual async UniTask HoldResourcesAsync ()
        {
            if (Assigned(DissolveTexturePath) && !DissolveTexturePath.DynamicValue)
            {
                var loader = Resources.LoadAsync<Texture2D>(DissolveTexturePath);
                await loader;
                preloadedDissolveTexture = loader.asset as Texture2D;
            }

            if (!AllowPreload || string.IsNullOrEmpty(AssignedId)) return;
            var actor = await ActorManager.GetOrAddActorAsync(AssignedId);
            await actor.HoldResourcesAsync(this, AssignedAppearance);
        }

        public virtual void ReleaseResources ()
        {
            preloadedDissolveTexture = null;

            if (!AllowPreload || ActorManager is null || string.IsNullOrEmpty(AssignedId)) return;
            if (ActorManager.ActorExists(AssignedId))
                ActorManager.GetActor(AssignedId).ReleaseResources(this, AssignedAppearance);
        }

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            if (ActorManager is null)
            {
                LogErrorWithPosition("Can't resolve actors manager.");
                return;
            }

            if (string.IsNullOrEmpty(AssignedId))
            {
                LogErrorWithPosition("Actor ID was not provided.");
                return;
            }

            var easingType = ActorManager.Configuration.DefaultEasing;
            if (Assigned(EasingTypeName) && !Enum.TryParse(EasingTypeName, true, out easingType))
                LogWarningWithPosition($"Failed to parse `{EasingTypeName}` easing.");

            if (AssignedId == "*")
            {
                var actors = ActorManager.GetAllActors().Where(a => a.Visible);
                await UniTask.WhenAll(actors.Select(a => ApplyModificationsAsync(a, easingType, cancellationToken)));
            }
            else
            {
                var actor = await ActorManager.GetOrAddActorAsync(AssignedId) as TActor;
                if (cancellationToken.IsCancellationRequested) return;
                await ApplyModificationsAsync(actor, easingType, cancellationToken);
            }
        }

        protected virtual async UniTask ApplyModificationsAsync (TActor actor, EasingType easingType, CancellationToken cancellationToken)
        {
            // In case the actor is hidden, apply all the modifications (except visibility) without animation.
            var duration = actor.Visible ? Duration : 0;
            await UniTask.WhenAll(
                // Change appearance with normal duration when a transition is assigned to preserve the effect.
                ApplyAppearanceModificationAsync(actor, easingType, string.IsNullOrEmpty(AssignedTransition) ? duration : Duration, cancellationToken),
                ApplyPositionModificationAsync(actor, easingType, duration, cancellationToken),
                ApplyRotationModificationAsync(actor, easingType, duration, cancellationToken),
                ApplyScaleModificationAsync(actor, easingType, duration, cancellationToken),
                ApplyTintColorModificationAsync(actor, easingType, duration, cancellationToken),
                ApplyVisibilityModificationAsync(actor, easingType, Duration, cancellationToken)
            );
        }

        protected virtual async UniTask ApplyAppearanceModificationAsync (TActor actor, EasingType easingType, float duration, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(AssignedAppearance)) return;

            var transitionName = !string.IsNullOrEmpty(AssignedTransition) ? AssignedTransition : TransitionType.Crossfade;
            var defaultParams = TransitionUtils.GetDefaultParams(transitionName);
            var transitionParams = Assigned(TransitionParams) ? new Vector4(
                    TransitionParams.ElementAtOrNull(0) ?? defaultParams.x,
                    TransitionParams.ElementAtOrNull(1) ?? defaultParams.y,
                    TransitionParams.ElementAtOrNull(2) ?? defaultParams.z,
                    TransitionParams.ElementAtOrNull(3) ?? defaultParams.w) : defaultParams;
            if (Assigned(DissolveTexturePath) && !ObjectUtils.IsValid(preloadedDissolveTexture))
                preloadedDissolveTexture = Resources.Load<Texture2D>(DissolveTexturePath);
            var transition = new Transition(transitionName, transitionParams, preloadedDissolveTexture);

            await actor.ChangeAppearanceAsync(AssignedAppearance, duration, easingType, transition, cancellationToken);
        }

        protected virtual async UniTask ApplyVisibilityModificationAsync (TActor actor, EasingType easingType, float duration, CancellationToken cancellationToken)
        {
            if (!AssignedVisibility.HasValue) return;
            await actor.ChangeVisibilityAsync(AssignedVisibility.Value, duration, easingType, cancellationToken);
        }

        protected virtual async UniTask ApplyPositionModificationAsync (TActor actor, EasingType easingType, float duration, CancellationToken cancellationToken)
        {
            var position = AssignedPosition;
            if (position is null) return;
            await actor.ChangePositionAsync(new Vector3(
                    position.ElementAtOrDefault(0) ?? actor.Position.x,
                    position.ElementAtOrDefault(1) ?? actor.Position.y,
                    position.ElementAtOrDefault(2) ?? actor.Position.z), duration, easingType, cancellationToken);
        }

        protected virtual async UniTask ApplyRotationModificationAsync (TActor actor, EasingType easingType, float duration, CancellationToken cancellationToken)
        {
            var rotation = AssignedRotation;
            if (rotation is null) return;
            await actor.ChangeRotationAsync(Quaternion.Euler(
                    rotation.ElementAtOrDefault(0) ?? actor.Rotation.eulerAngles.x,
                    rotation.ElementAtOrDefault(1) ?? actor.Rotation.eulerAngles.y,
                    rotation.ElementAtOrDefault(2) ?? actor.Rotation.eulerAngles.z), duration, easingType, cancellationToken);
        }

        protected virtual async UniTask ApplyScaleModificationAsync (TActor actor, EasingType easingType, float duration, CancellationToken cancellationToken)
        {
            var scale = AssignedScale;
            if (scale is null) return;
            await actor.ChangeScaleAsync(new Vector3(
                    scale.ElementAtOrDefault(0) ?? actor.Scale.x,
                    scale.ElementAtOrDefault(1) ?? actor.Scale.y,
                    scale.ElementAtOrDefault(2) ?? actor.Scale.z), duration, easingType, cancellationToken);
        }

        protected virtual async UniTask ApplyTintColorModificationAsync (TActor actor, EasingType easingType, float duration, CancellationToken cancellationToken)
        {
            if (!AssignedTintColor.HasValue) return;
            await actor.ChangeTintColorAsync(AssignedTintColor.Value, duration, easingType, cancellationToken);
        }

        protected virtual Color? ParseColor (string color)
        {
            if (string.IsNullOrEmpty(color)) return null;

            if (!ColorUtility.TryParseHtmlString(TintColor, out var result))
            {
                LogErrorWithPosition($"Failed to parse `{TintColor}` color to apply tint modification. See the API docs for supported color formats.");
                return null;
            }
            return result;
        }
    } 
}
