// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using System.Threading;
using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Modifies a [character actor](/guide/characters.md).
    /// </summary>
    /// <example>
    /// ; Shows character with ID `Sora` with a default appearance.
    /// @char Sora
    /// 
    /// ; Same as above, but sets appearance to `Happy`.
    /// @char Sora.Happy
    /// 
    /// ; Same as above, but also positions the character 45% away from the left border 
    /// ; of the screen and 10% away from the bottom border; also makes him look to the left.
    /// @char Sora.Happy look:left pos:45,10
    /// 
    /// ; Make Sora appear at the bottom-center and in front of Felix
    /// @char Sora pos:50,0,-1
    /// @char Felix pos:,,0
    /// 
    /// ; Tint all visible characters on scene.
    /// @char * tint:#ffdc22
    /// </example>
    [CommandAlias("char")]
    public class ModifyCharacter : ModifyOrthoActor<ICharacterActor, CharacterState, CharacterMetadata, CharactersConfiguration, ICharacterManager>
    {
        /// <summary>
        /// ID of the character to modify (specify `*` to affect all visible characters) and an appearance (or [pose](/guide/characters.md#poses)) to set.
        /// When appearance is not provided, will use either a `Default` (is exists) or a random one.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), RequiredParameter]
        public NamedStringParameter IdAndAppearance;
        /// <summary>
        /// Look direction of the actor; supported values: left, right, center.
        /// </summary>
        [ParameterAlias("look")]
        public StringParameter LookDirection;
        /// <summary>
        /// Name (path) of the [avatar texture](/guide/characters.md#avatar-textures) to assign for the character.
        /// Use `none` to remove (un-assign) avatar texture from the character.
        /// </summary>
        [ParameterAlias("avatar")]
        public StringParameter AvatarTexturePath;

        protected override bool AllowPreload => !IdAndAppearance.DynamicValue;
        // Allows specifying ID via the nameless parameter.
        protected override string AssignedId => IdAndAppearance?.Name;
        // Allows specifying appearance via the nameless parameter.
        protected override string AssignedAppearance => Pose?.Appearance ?? IdAndAppearance?.NamedValue;
        protected override CharacterState Pose => ActorManager.Configuration.GetMetadataOrDefault(IdAndAppearance?.Name).GetPoseOrNull<CharacterState>(IdAndAppearance?.NamedValue);
        protected virtual CharacterLookDirection? AssignedLookDirection => Assigned(LookDirection) ? ParseLookDirection(LookDirection) : Pose?.LookDirection;

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            await base.ExecuteAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested) return;

            if (!Assigned(AvatarTexturePath)) // Check if we can map current appearance to an avatar texture path.
            {
                var avatarPath = $"{AssignedId}/{AssignedAppearance}";
                if (ActorManager.AvatarTextureExists(avatarPath) && ActorManager.GetAvatarTexturePathFor(AssignedId) != avatarPath)
                    ActorManager.SetAvatarTexturePathFor(AssignedId, avatarPath);
                else // Check if a default avatar texture for the character exists and assign if it does.
                {
                    var defaultAvatarPath = $"{AssignedId}/Default";
                    if (ActorManager.AvatarTextureExists(defaultAvatarPath) && ActorManager.GetAvatarTexturePathFor(AssignedId) != defaultAvatarPath)
                        ActorManager.SetAvatarTexturePathFor(AssignedId, defaultAvatarPath);
                }
            }
            else // User provided specific avatar texture path, assigning it.
            {
                if (AvatarTexturePath?.Value.EqualsFastIgnoreCase("none") ?? false)
                    ActorManager.RemoveAvatarTextureFor(AssignedId);
                else ActorManager.SetAvatarTexturePathFor(AssignedId, AvatarTexturePath);
            }
        }

        protected override async UniTask ApplyModificationsAsync (ICharacterActor actor, EasingType easingType, CancellationToken cancellationToken)
        {
            // Execute auto-arrange if adding to scene, no position specified and auto-arrange on add is enabled.
            var autoArrange = Pose is null && !Assigned(ScenePosition) && AssignedPosition is null && !actor.Visible && AssignedVisibility.HasValue && AssignedVisibility.Value && ActorManager.Configuration.AutoArrangeOnAdd;

            var tasks = new List<UniTask>();
            tasks.Add(base.ApplyModificationsAsync(actor, easingType, cancellationToken));
            tasks.Add(ApplyLookDirectionModificationAsync(actor, easingType, cancellationToken));

            if (autoArrange) 
                tasks.Add(ActorManager.ArrangeCharactersAsync(!Assigned(LookDirection), Duration, easingType, cancellationToken));

            await UniTask.WhenAll(tasks);
        }

        protected virtual async UniTask ApplyLookDirectionModificationAsync (ICharacterActor actor, EasingType easingType, CancellationToken cancellationToken)
        {
            if (!AssignedLookDirection.HasValue) return;
            await actor.ChangeLookDirectionAsync(AssignedLookDirection.Value, Duration, easingType, cancellationToken);
        }

        protected virtual CharacterLookDirection? ParseLookDirection (string value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            if (value.EqualsFastIgnoreCase("right")) return CharacterLookDirection.Right;
            else if (value.EqualsFastIgnoreCase("left")) return CharacterLookDirection.Left;
            else if (value.EqualsFastIgnoreCase("center")) return CharacterLookDirection.Center;
            else { LogErrorWithPosition($"`{value}` is not a valid value for a character look direction; see API guide for `@char` command for the list of supported values."); return null; }
        }
    } 
}
