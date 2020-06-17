// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Naninovel.Commands
{
    /// <summary>
    /// Arranges specified characters by X-axis.
    /// When no parameters provided, will execute an auto-arrange evenly distributing visible characters by X-axis.
    /// </summary>
    /// <example>
    /// ; Evenly distribute all the visible characters
    /// @arrange
    /// 
    /// ; Place character with ID `Jenna` 15%, `Felix` 50% and `Mia` 85% away 
    /// ; from the left border of the screen.
    /// @arrange Jenna.15,Felix.50,Mia.85
    /// </example>
    [CommandAlias("arrange")]
    public class ArrangeCharacters : Command
    {
        /// <summary>
        /// A collection of character ID to scene X-axis position (relative to the left screen border, in percents) named values.
        /// Position 0 relates to the left border and 100 to the right border of the screen; 50 is the center.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias)]
        public NamedDecimalListParameter CharacterPositions;
        /// <summary>
        /// When performing auto-arrange, controls whether to also make the characters look at the scene origin (enabled by default).
        /// </summary>
        [ParameterAlias("look")]
        public BooleanParameter LookAtOrigin = true;
        /// <summary>
        /// Duration (in seconds) of the arrangement animation. Default value: 0.35 seconds.
        /// </summary>
        [ParameterAlias("time")]
        public DecimalParameter Duration = .35f;

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var manager = Engine.GetService<ICharacterManager>();

            // When positions are not specified execute auto arrange.
            if (!Assigned(CharacterPositions))
            {
                await manager.ArrangeCharactersAsync(LookAtOrigin, Duration, EasingType.SmoothStep, cancellationToken);
                return;
            }

            var actors = manager.GetAllActors().ToList();
            var arrangeTasks = new List<UniTask>();

            foreach (var actorPos in CharacterPositions)
            {
                if (!actorPos.HasValue) continue;

                var actor = actors.Find(a => a.Id.EqualsFastIgnoreCase(actorPos.Name));
                var posX = actorPos.NamedValue / 100f; // Implementation is expecting local scene pos, not percents.
                if (actor is null)
                {
                    LogWarningWithPosition($"Actor '{actorPos.Name}' not found while executing arranging task.");
                    continue;
                }
                var newPosX = Engine.GetConfiguration<CameraConfiguration>().SceneToWorldSpace(new Vector2(posX, 0)).x;
                var newDir = manager.LookAtOriginDirection(newPosX);
                arrangeTasks.Add(actor.ChangeLookDirectionAsync(newDir, Duration, EasingType.SmoothStep, cancellationToken));
                arrangeTasks.Add(actor.ChangePositionXAsync(newPosX, Duration, EasingType.SmoothStep, cancellationToken));
            }

            // Sorting by z in order of declaration (first is bottom).
            var declaredActorIds = CharacterPositions
                .Where(a => !string.IsNullOrEmpty(a?.Value?.Name))
                .Select(a => a.Name).ToList();
            declaredActorIds.Reverse();
            for (int i = 0; i < declaredActorIds.Count - 1; i++)
            {
                var currentActor = actors.Find(a => a.Id.EqualsFastIgnoreCase(declaredActorIds[i]));
                var nextActor = actors.Find(a => a.Id.EqualsFastIgnoreCase(declaredActorIds[i + 1]));
                if (currentActor is null || nextActor is null) continue;

                if (currentActor.Position.z > nextActor.Position.z)
                {
                    var lowerZPos = nextActor.Position.z;
                    var higherZPos = currentActor.Position.z;

                    nextActor.ChangePositionZ(higherZPos);
                    currentActor.ChangePositionZ(lowerZPos);
                }
            }

            await UniTask.WhenAll(arrangeTasks);
        }
    }
}
