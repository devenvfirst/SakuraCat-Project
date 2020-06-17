// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Animate properties of the actors with the specified IDs via key frames.
    /// Key frames for the animated parameters are delimited with `|` literals.
    /// </summary>
    /// <remarks>
    /// Be aware, that this command searches for actors with the provided IDs over all the actor managers, 
    /// and in case multiple actors with the same ID exist (eg, a character and a text printer), this will affect only the first found one.
    /// <br/><br/>
    /// When running the animate commands in parallel (`wait` is set to false) the affected actors state can mutate unpredictably.
    /// This could cause unexpected results when rolling back or performing other commands that affect state of the actor. Make sure to reset
    /// affected properties of the animated actors (position, tint, appearance, etc) after the command finishes or use `@animate CharacterId` 
    /// (without any args) to stop the animation prematurely.
    /// </remarks>
    /// <example>
    /// ; Animate `Kohaku` actor over three animation steps (key frames), 
    /// ; changing positions: first step will take 1, second — 0.5 and third — 3 seconds.
    /// @animate Kohaku posX:50|0|85 time:1|0.5|3
    /// 
    /// ; Start loop animations of `Yuko` and `Kohaku` actors; notice, that you can skip
    /// ; key values indicating that the parameter shouldn't change during the animation step.
    /// @animate Kohaku,Yuko loop:true appearance:Surprise|Sad|Default|Angry transition:DropFade|Ripple|Pixelate posX:15|85|50 posY:0|-25|-85 scale:1|1.25|1.85 tint:#25f1f8|lightblue|#ffffff|olive easing:EaseInBounce|EaseInQuad time:3|2|1|0.5 wait:false
    /// ...
    /// ; Stop the animations.
    /// @animate Yuko,Kohaku loop:false
    /// 
    /// ; Start a long background animation for `Kohaku`.
    /// @animate Kohaku posX:90|0|90 scale:1|2|1 time:10 wait:false
    /// ; Do something else while the animation is running.
    /// ...
    /// ; Here we're going to set a specific position for the character,
    /// ; but the animation could still be running in background, so reset it first.
    /// @animate Kohaku
    /// ; Now it's safe to modify previously animated properties.
    /// @char Kohaku pos:50 scale:1
    /// </example>
    [CommandAlias("animate")]
    public class AnimateActor : Command
    {
        /// <summary>
        /// Literal used to delimit adjecent animation key values.
        /// </summary>
        public const char KeyDelimiter = '|';
        /// <summary>
        /// Path to the prefab to spawn with <see cref="ISpawnManager"/>.
        /// </summary>
        public const string prefabPath = "Animate";

        /// <summary>
        /// IDs of the actors to animate.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), RequiredParameter]
        public StringListParameter ActorIds;
        /// <summary>
        /// Whether to loop the animation; make sure to set `wait` to false when loop is enabled,
        /// otherwise script playback will loop indefinitely.
        /// </summary>
        public BooleanParameter Loop = false;
        /// <summary>
        /// Appearances to set for the animated actors.
        /// </summary>
        public StringParameter Appearance;
        /// <summary>
        /// Type of the [transition effect](/guide/transition-effects.md) to use when animating appearance change (crossfade is used by default).
        /// </summary>
        public StringParameter Transition;
        /// <summary>
        /// Visibility status to set for the animated actors.
        /// </summary>
        public StringParameter Visibility;
        /// <summary>
        /// Position values over X-axis (in 0 to 100 range, in percents from the left border of the screen) to set for the animated actors.
        /// </summary>
        [ParameterAlias("posX")]
        public StringParameter ScenePositionX;
        /// <summary>
        /// Position values over Y-axis (in 0 to 100 range, in percents from the bottom border of the screen) to set for the animated actors.
        /// </summary>
        [ParameterAlias("posY")]
        public StringParameter ScenePositionY;
        /// <summary>
        /// Position values over Z-axis (in world space) to set for the animated actors; while in ortho mode, can only be used for sorting.
        /// </summary>
        [ParameterAlias("posZ")]
        public StringParameter PositionZ;
        /// <summary>
        /// Rotation values (over Z-axis) to set for the animated actors.
        /// </summary>
        public StringParameter Rotation;
        /// <summary>
        /// Scale values (uniform) to set for the animated actors.
        /// </summary>
        public StringParameter Scale;
        /// <summary>
        /// Tint colors to set for the animated actors.
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
        /// Names of the easing functions to use for the animations.
        /// <br/><br/>
        /// Available options: Linear, SmoothStep, Spring, EaseInQuad, EaseOutQuad, EaseInOutQuad, EaseInCubic, EaseOutCubic, EaseInOutCubic, EaseInQuart, EaseOutQuart, EaseInOutQuart, EaseInQuint, EaseOutQuint, EaseInOutQuint, EaseInSine, EaseOutSine, EaseInOutSine, EaseInExpo, EaseOutExpo, EaseInOutExpo, EaseInCirc, EaseOutCirc, EaseInOutCirc, EaseInBounce, EaseOutBounce, EaseInOutBounce, EaseInBack, EaseOutBack, EaseInOutBack, EaseInElastic, EaseOutElastic, EaseInOutElastic.
        /// <br/><br/>
        /// When not specified, will use a default easing function set in the actor's manager configuration settings.
        /// </summary>
        [ParameterAlias("easing")]
        public StringParameter EasingTypeName;
        /// <summary>
        /// Duration of the animations per key, in seconds.
        /// When a key value is missing, will use one from a previous key.
        /// When not assigned, will use 0.35 seconds duration for all keys.
        /// </summary>
        [ParameterAlias("time")]
        public StringParameter Duration;

        private const string defaultDuration = "0.35";

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var spawnManager = Engine.GetService<ISpawnManager>();
            var tasks = new List<UniTask>();

            foreach (var actorId in ActorIds)
            {
                var parameters = new string[13]; // Don't cache it, otherwise parameters will leak across actors on async spawn init.

                parameters[0] = actorId;
                parameters[1] = Loop.Value.ToString(CultureInfo.InvariantCulture);
                parameters[2] = Assigned(Appearance) ? Appearance : null;
                parameters[3] = Assigned(Transition) ? Transition : null;
                parameters[4] = Assigned(Visibility) ? Visibility : null;
                parameters[5] = Assigned(ScenePositionX) ? ScenePositionX : null;
                parameters[6] = Assigned(ScenePositionY) ? ScenePositionY : null;
                parameters[7] = Assigned(PositionZ) ? PositionZ : null;
                parameters[8] = Assigned(Rotation) ? Rotation : null;
                parameters[9] = Assigned(Scale) ? Scale : null;
                parameters[10] = Assigned(TintColor) ? TintColor : null;
                parameters[11] = Assigned(EasingTypeName) ? EasingTypeName : null;
                parameters[12] = Assigned(Duration) ? Duration.Value : defaultDuration;

                var spawnPath = $"{prefabPath}{SpawnManager.IdDelimiter}{actorId}";
                if (spawnManager.IsObjectSpawned(spawnPath))
                    tasks.Add(spawnManager.UpdateSpawnedAsync(spawnPath, cancellationToken, parameters));
                else tasks.Add(spawnManager.SpawnAsync(spawnPath, cancellationToken, parameters));
            }

            await UniTask.WhenAll(tasks);
        }
    }
}
