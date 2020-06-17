// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.


namespace Naninovel.Commands
{
    /// <summary>
    /// Modifies a [background actor](/guide/backgrounds.md).
    /// </summary>
    /// <remarks>
    /// Backgrounds are handled a bit differently from characters to better accommodate traditional VN game flow. 
    /// Most of the time you'll probably have a single background actor on scene, which will constantly transition to different appearances.
    /// To remove the hassle of repeating same actor ID in scripts, it's possible to provide only 
    /// the background appearance and transition type (optional) as a nameless parameter assuming `MainBackground` 
    /// actor should be affected. When this is not the case, ID of the background actor can be explicitly provided via the `id` parameter.
    /// </remarks>
    /// <example>
    /// ; Set `River` as the appearance of the main background
    /// @back River
    /// 
    /// ; Same as above, but also use a `RadialBlur` transition effect
    /// @back River.RadialBlur
    /// 
    /// ; Tint all visible backgrounds on scene.
    /// @back id:* tint:#ffdc22
    /// 
    /// ; Given an `ExplosionSound` SFX and an `ExplosionSprite` background, the following 
    /// ; script sequence will simulate two explosions appearing far and close to the camera.
    /// @sfx ExplosionSound volume:0.1
    /// @back id:ExplosionSprite scale:0.3 pos:55,60 time:0 isVisible:false
    /// @back id:ExplosionSprite
    /// @fx ShakeBackground params:,1
    /// @hide ExplosionSprite
    /// @sfx ExplosionSound volume:1.5
    /// @back id:ExplosionSprite pos:65 scale:1
    /// @fx ShakeBackground params:,3
    /// @hide ExplosionSprite
    /// </example>
    [CommandAlias("back")]
    public class ModifyBackground : ModifyOrthoActor<IBackgroundActor, BackgroundState, BackgroundMetadata, BackgroundsConfiguration, IBackgroundManager>
    {
        /// <summary>
        /// Appearance (or [pose](/guide/backgrounds.md#poses)) to set for the modified background and type of a [transition effect](/guide/transition-effects.md) to use.
        /// When transition is not provided, a cross-fade effect will be used by default.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias)]
        public NamedStringParameter AppearanceAndTransition;

        protected override bool AllowPreload => Assigned(AppearanceAndTransition) ? !AppearanceAndTransition.DynamicValue : false;
        // Default to main background when no ID is specified.
        protected override string AssignedId => base.AssignedId ?? BackgroundsConfiguration.MainActorId;
        // Allows specifying background appearance as nameless parameter.
        protected override string AssignedAppearance => Pose?.Appearance ?? AppearanceAndTransition?.Name ?? base.AssignedAppearance;
        // Allows specifying background transition as nameless parameter.
        protected override string AssignedTransition => AppearanceAndTransition?.NamedValue ?? base.AssignedTransition;
        protected override BackgroundState Pose => ActorManager.Configuration.GetMetadataOrDefault(AssignedId).GetPoseOrNull<BackgroundState>(AppearanceAndTransition?.Name);
    }
}
