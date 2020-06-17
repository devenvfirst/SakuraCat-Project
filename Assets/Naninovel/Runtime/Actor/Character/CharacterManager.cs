// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Naninovel
{
    /// <inheritdoc cref="ICharacterManager"/>
    [InitializeAtRuntime]
    public class CharacterManager : OrthoActorManager<ICharacterActor, CharacterState, CharacterMetadata, CharactersConfiguration>, ICharacterManager
    {
        [Serializable]
        public new class GameState
        {
            public SerializableLiteralStringMap CharIdToAvatarPathMap = new SerializableLiteralStringMap();
        }

        public event Action<CharacterAvatarChangedArgs> OnCharacterAvatarChanged;

        private readonly ITextManager textManager;
        private readonly ILocalizationManager localizationManager;
        private readonly ICustomVariableManager customVariableManager;
        private readonly ITextPrinterManager textPrinterManager;
        private SerializableLiteralStringMap charIdToAvatarPathMap = new SerializableLiteralStringMap();
        private LocalizableResourceLoader<Texture2D> avatarTextureLoader;

        public CharacterManager (CharactersConfiguration config, CameraConfiguration cameraConfig, ITextManager textManager, 
            ILocalizationManager localizationManager, ICustomVariableManager customVariableManager, ITextPrinterManager textPrinterManager)
            : base(config, cameraConfig)
        {
            this.textManager = textManager;
            this.localizationManager = localizationManager;
            this.customVariableManager = customVariableManager;
            this.textPrinterManager = textPrinterManager;
        }

        public override void ResetService ()
        {
            base.ResetService();

            charIdToAvatarPathMap.Clear();
        }

        public override async UniTask InitializeServiceAsync ()
        {
            await base.InitializeServiceAsync();

            var providerMngr = Engine.GetService<IResourceProviderManager>();
            avatarTextureLoader = Configuration.AvatarLoader.CreateLocalizableFor<Texture2D>(providerMngr, localizationManager);

            textPrinterManager.OnPrintTextStarted += HandleAuthorHighlighting;

            // TODO: Load only the required avatar textures.
            var avatarResources = await avatarTextureLoader.LoadAllAsync();
            foreach (var resource in avatarResources)
                resource.Hold(this);
        }

        public override void DestroyService ()
        {
            base.DestroyService();

            if (textPrinterManager != null)
                textPrinterManager.OnPrintTextStarted -= HandleAuthorHighlighting;

            if (avatarTextureLoader != null)
                foreach (var resource in avatarTextureLoader.GetAllLoaded())
                    resource.Release(this);
        }

        public override void SaveServiceState (GameStateMap stateMap)
        {
            base.SaveServiceState(stateMap);

            var gameState = new GameState() {
                CharIdToAvatarPathMap = new SerializableLiteralStringMap(charIdToAvatarPathMap)
            };
            stateMap.SetState(gameState);
        }

        public override async UniTask LoadServiceStateAsync (GameStateMap stateMap)
        {
            await base.LoadServiceStateAsync(stateMap);

            var state = stateMap.GetState<GameState>();
            if (state is null)
            {
                if (charIdToAvatarPathMap.Count > 0)
                    foreach (var charId in charIdToAvatarPathMap.Keys.ToList())
                        RemoveAvatarTextureFor(charId);
                return;
            }

            // Remove non-existing avatar mappings.
            if (charIdToAvatarPathMap.Count > 0)
                foreach (var charId in charIdToAvatarPathMap.Keys.ToList())
                    if (!state.CharIdToAvatarPathMap.ContainsKey(charId))
                        RemoveAvatarTextureFor(charId);
            // Add new or changed avatar mappings.
            foreach (var kv in state.CharIdToAvatarPathMap)
                SetAvatarTexturePathFor(kv.Key, kv.Value);
        }

        public bool AvatarTextureExists (string avatarTexturePath) => avatarTextureLoader.IsLoaded(avatarTexturePath);

        public void RemoveAvatarTextureFor (string characterId)
        {
            if (!charIdToAvatarPathMap.ContainsKey(characterId)) return;

            charIdToAvatarPathMap.Remove(characterId);
            OnCharacterAvatarChanged?.Invoke(new CharacterAvatarChangedArgs(characterId, null));
        }

        public Texture2D GetAvatarTextureFor (string characterId)
        {
            var avatarTexturePath = GetAvatarTexturePathFor(characterId);
            if (avatarTexturePath is null) return null;
            return avatarTextureLoader.GetLoadedOrNull(avatarTexturePath);
        }

        public string GetAvatarTexturePathFor (string characterId)
        {
            if (!charIdToAvatarPathMap.TryGetValue(characterId ?? string.Empty, out var avatarTexturePath))
            {
                var defaultPath = $"{characterId}/Default"; // Attempt default path.
                return AvatarTextureExists(defaultPath) ? defaultPath : null;
            }
            if (!AvatarTextureExists(avatarTexturePath)) return null;
            return avatarTexturePath;
        }

        public void SetAvatarTexturePathFor (string characterId, string avatarTexturePath)
        {
            if (!ActorExists(characterId))
            {
                Debug.LogWarning($"Failed to assign `{avatarTexturePath}` avatar texture to `{characterId}` character: character with the provided ID not found.");
                return;
            }

            if (!AvatarTextureExists(avatarTexturePath))
            {
                Debug.LogWarning($"Failed to assign `{avatarTexturePath}` avatar texture to `{characterId}` character: avatar texture with the provided path not found.");
                return;
            }

            charIdToAvatarPathMap.TryGetValue(characterId ?? string.Empty, out var initialPath);
            charIdToAvatarPathMap[characterId] = avatarTexturePath;

            if (initialPath != avatarTexturePath)
            {
                var avatarTexture = GetAvatarTextureFor(characterId);
                OnCharacterAvatarChanged?.Invoke(new CharacterAvatarChangedArgs(characterId, avatarTexture));
            }
        }

        /// <inheritdoc/>
        /// <remarks>
        /// When using a non-source locale, will first attempt to find a corresponding record 
        /// in the managed text documents, and, if not found, check the character metadata.
        /// In case the display name is found and is wrapped in curely braces, attempt to extract the value 
        /// from a custom variable.
        /// </remarks>
        public string GetDisplayName (string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId)) return null;

            var displayName = default(string);

            if (!localizationManager.SourceLocaleSelected())
                displayName = textManager.GetRecordValue(characterId, CharactersConfiguration.DisplayNamesCategory);

            if (string.IsNullOrEmpty(displayName))
                displayName = Configuration.GetMetadataOrDefault(characterId).DisplayName;

            if (!string.IsNullOrEmpty(displayName) && displayName.StartsWithFast("{") && displayName.EndsWithFast("}"))
            {
                var customVarName = displayName.GetAfterFirst("{").GetBeforeLast("}");
                if (!customVariableManager.VariableExists(customVarName))
                {
                    Debug.LogWarning($"Failed to retrieve `{customVarName}` custom variable binded to `{characterId}` character display name.");
                    return null;
                }
                displayName = customVariableManager.GetVariableValue(customVarName);
            }

            return string.IsNullOrEmpty(displayName) ? null : displayName;
        }

        public CharacterLookDirection LookAtOriginDirection (float xPos)
        {
            if (Mathf.Approximately(xPos, GlobalSceneOrigin.x)) return CharacterLookDirection.Center;
            return xPos < GlobalSceneOrigin.x ? CharacterLookDirection.Right : CharacterLookDirection.Left;
        }

        public async UniTask ArrangeCharactersAsync (bool lookAtOrigin = true, float duration = 0, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            var actors = ManagedActors?.Values?.Where(c => c.Visible)?.OrderBy(c => c.Id)?.ToList();
            if (actors is null || actors.Count == 0) return;
            var stepSize = CameraConfiguration.ReferenceSize.x / actors.Count;
            var halfRefSize = CameraConfiguration.ReferenceSize.x / 2f;
            var evenCount = 1;
            var unevenCount = 1;

            var tasks = new List<UniTask>();
            for (int i = 0; i < actors.Count; i++)
            {
                var isEven = i.IsEven();
                float posX;
                if (isEven)
                {
                    var step = (evenCount * stepSize) / 2f;
                    posX = -halfRefSize + step;
                    evenCount++;
                }
                else
                {
                    var step = (unevenCount * stepSize) / 2f;
                    posX = halfRefSize - step;
                    unevenCount++;
                }
                tasks.Add(actors[i].ChangePositionXAsync(posX, duration, easingType, cancellationToken));

                if (lookAtOrigin)
                {
                    var lookDir = LookAtOriginDirection(posX);
                    tasks.Add(actors[i].ChangeLookDirectionAsync(lookDir, duration, easingType, cancellationToken));
                }
            }
            await UniTask.WhenAll(tasks);
        }

        protected override async UniTask<ICharacterActor> ConstructActorAsync (string actorId)
        {
            var actor = await base.ConstructActorAsync(actorId);

            // When adding new character place it at the scene origin by default.
            actor.Position = new Vector3(GlobalSceneOrigin.x, GlobalSceneOrigin.y, actor.Position.z);

            var meta = Configuration.GetMetadataOrDefault(actorId);
            if (meta.HighlightWhenSpeaking)
                actor.TintColor = meta.NotSpeakingTint;

            return actor;
        }

        protected virtual void HandleAuthorHighlighting (PrintTextArgs args)
        {
            if (ManagedActors.Count == 0) return;

            var visibleActors = ManagedActors.Count(a => a.Value.Visible);

            foreach (var actor in ManagedActors.Values)
            {
                var actorMeta = Configuration.GetMetadataOrDefault(actor.Id);
                if (!actorMeta.HighlightWhenSpeaking) continue;
                var tintColor = (actorMeta.HighlightCharacterCount > visibleActors || actor.Id == args.AuthorId) ? actorMeta.SpeakingTint : actorMeta.NotSpeakingTint;
                actor.ChangeTintColorAsync(tintColor, actorMeta.HighlightDuration, actorMeta.HighlightEasing).Forget();
            }

            if (string.IsNullOrEmpty(args.AuthorId) || !ActorExists(args.AuthorId)) return;
            var authorMeta = Configuration.GetMetadataOrDefault(args.AuthorId);
            if (authorMeta.HighlightWhenSpeaking && authorMeta.HighlightCharacterCount <= visibleActors && authorMeta.PlaceOnTop)
            {
                var topmostChar = ManagedActors.Values.OrderBy(c => c.Position.z).FirstOrDefault();
                if (!topmostChar.Id.EqualsFast(args.AuthorId))
                {
                    var authorChar = GetActor(args.AuthorId);
                    var authorZPos = authorChar.Position.z;
                    var topmostZPos = topmostChar.Position.z < authorZPos ? topmostChar.Position.z : topmostChar.Position.z - .1f;
                    authorChar.ChangePositionZAsync(topmostZPos, authorMeta.HighlightDuration, authorMeta.HighlightEasing).Forget();
                    topmostChar.ChangePositionZAsync(authorZPos, authorMeta.HighlightDuration, authorMeta.HighlightEasing).Forget();
                }
            }
        }
    }
}
