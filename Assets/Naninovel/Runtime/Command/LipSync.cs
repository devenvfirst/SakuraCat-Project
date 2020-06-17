// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Allows to force-stop the lip sync mouth animation for a character with the provided ID; when stopped, the animation
    /// won't start again, until this command is used again to allow it.
    /// The character should be able to receive the lip sync events (currently generic and Live2D implementations only).
    /// See [characters guide](/guide/characters.md#lip-sync) for more information on lip sync feature.
    /// </summary>
    /// <example>
    /// ; Given auto voicing is disabled and lip sync is driven by text messages,
    /// ; exclude punctuation from the mouth animation.
    /// Kohaku: Lorem ipsum dolor sit amet[lipSync Kohaku.false]... [lipSync Kohaku.true]Consectetur adipiscing elit.
    /// </example>
    public class LipSync : Command
    {
        /// <summary>
        /// Implementation is a <see cref="ICharacterActor"/>, that is able to receive <see cref="LipSync"/> commands.
        /// </summary>
        public interface IReceiver
        {
            /// <summary>
            /// The character should refrain from the lip sync mouth animation until allowed.
            /// </summary>
            void AllowLipSync (bool active);
        }

        /// <summary>
        /// Character ID followed by a boolean (true or false) on whether to halt or allow the lip sync animation.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), RequiredParameter]
        public NamedBooleanParameter CharIdAndAllow;

        public override UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var characterManager = Engine.GetService<ICharacterManager>();
            if (!characterManager.ActorExists(CharIdAndAllow.Name))
            {
                LogWarningWithPosition($"Failed to control lip sync for `{CharIdAndAllow.Name}`: character with the ID doesn't exist.");
                return UniTask.CompletedTask;
            }

            if (characterManager.GetActor(CharIdAndAllow.Name) is IReceiver receiver)
                receiver.AllowLipSync(CharIdAndAllow.NamedValue ?? false);
            else
            {
                LogWarningWithPosition($"Failed to control lip sync for `{CharIdAndAllow.Name}`: character is not able to receive lip sync events.");
                return UniTask.CompletedTask;
            }

            return UniTask.CompletedTask;
        }
    }
}
