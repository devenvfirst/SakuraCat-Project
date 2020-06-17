// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Naninovel.Commands
{
    /// <summary>
    /// Slides (moves over X-axis) actor (character, background, text printer or choice handler) with the provided ID and optionally changes actor appearance.
    /// </summary>
    /// <remarks>
    /// Be aware, that this command searches for an actor with the provided ID over all the actor managers, 
    /// and in case multiple actors with the same ID exist (eg, a character and a text printer), this will affect only the first found one.
    /// </remarks>
    /// <example>
    /// ; Given `Jenna` actor is not currenly visible, reveal it with a 
    /// ; `Angry` appearance and slide to the center of the screen.
    /// @slide Jenna.Angry to:50
    /// 
    /// ; Given `Sheba` actor is currenly visible,
    /// ; hide and slide it out of the screen over the left border.
    /// @slide Sheba to:-10 visible:false
    /// 
    /// ; Slide `Mia` actor from left side of the screen to the right 
    /// ; over 5 seconds using `EaseOutBounce` animation easing.
    /// @slide Sheba from:15 to:85 time:5 easing:EaseOutBounce
    /// </example>
    [CommandAlias("slide")]
    public class SlideActor : Command
    {
        /// <summary>
        /// ID of the actor to slide and (optionally) appearance to set.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), RequiredParameter]
        public NamedStringParameter IdAndAppearance;
        /// <summary>
        /// Position over X-axis (in 0 to 100 range, in percents from the left border of the screen) to slide the actor from.
        /// When not provided, will use current actor position in case it's visible and a random off-screen position otherwise.
        /// </summary>
        [ParameterAlias("from")]
        public DecimalParameter FromPositionX;
        /// <summary>
        /// Position over X-axis (in 0 to 100 range, in percents from the left border of the screen) to slide the actor to.
        /// </summary>
        [ParameterAlias("to"), RequiredParameter]
        public DecimalParameter ToPositionX;
        /// <summary>
        /// Change visibility status of the actor (show or hide).
        /// When not set and target actor is hidden, will still automatically show it.
        /// </summary>
        public BooleanParameter Visible;
        /// <summary>
        /// Name of the easing function to use for the modifications.
        /// <br/><br/>
        /// Available options: Linear, SmoothStep, Spring, EaseInQuad, EaseOutQuad, EaseInOutQuad, EaseInCubic, EaseOutCubic, EaseInOutCubic, EaseInQuart, EaseOutQuart, EaseInOutQuart, EaseInQuint, EaseOutQuint, EaseInOutQuint, EaseInSine, EaseOutSine, EaseInOutSine, EaseInExpo, EaseOutExpo, EaseInOutExpo, EaseInCirc, EaseOutCirc, EaseInOutCirc, EaseInBounce, EaseOutBounce, EaseInOutBounce, EaseInBack, EaseOutBack, EaseInOutBack, EaseInElastic, EaseOutElastic, EaseInOutElastic.
        /// <br/><br/>
        /// When not specified, will use a default easing function set in the actor's manager configuration settings.
        /// </summary>
        [ParameterAlias("easing")]
        public StringParameter EasingTypeName;
        /// <summary>
        /// Duration (in seconds) of the slide animation. Default value: 0.35 seconds.
        /// </summary>
        [ParameterAlias("time")]
        public DecimalParameter Duration = .35f;

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var actorId = IdAndAppearance.Name;
            var manager = Engine.GetAllServices<IActorManager>(c => c.ActorExists(actorId)).FirstOrDefault();

            if (manager is null)
            {
                Debug.LogError($"Can't find a manager with `{actorId}` actor.");
                return;
            }

            var tasks = new List<UniTask>();

            var cameraConfig = Engine.GetConfiguration<CameraConfiguration>();
            var actor = manager.GetActor(actorId);
            var fromPos = Assigned(FromPositionX) ? cameraConfig.SceneToWorldSpace(new Vector2(FromPositionX.Value / 100f, 0)).x 
                : actor.Visible ? actor.Position.x : cameraConfig.SceneToWorldSpace(new Vector2(Random.value > .5f ? -.1f : 1.1f, 0)).x;
            var toPos = cameraConfig.SceneToWorldSpace(new Vector2(ToPositionX / 100f, 0)).x;

            var easingType = manager.Configuration.DefaultEasing;
            if (Assigned(EasingTypeName) && !System.Enum.TryParse(EasingTypeName, true, out easingType))
                LogWarningWithPosition($"Failed to parse `{EasingTypeName}` easing.");

            actor.ChangePositionX(fromPos);

            if (!actor.Visible)
            {
                if (IdAndAppearance.NamedValue.HasValue)
                    actor.Appearance = IdAndAppearance.NamedValue;
                Visible = true;
            }
            else if (IdAndAppearance.NamedValue.HasValue)
                tasks.Add(actor.ChangeAppearanceAsync(IdAndAppearance.NamedValue, Duration, easingType, null, cancellationToken));

            if (Assigned(Visible)) tasks.Add(actor.ChangeVisibilityAsync(Visible, Duration, easingType, cancellationToken));

            tasks.Add(actor.ChangePositionXAsync(toPos, Duration, easingType, cancellationToken));

            await UniTask.WhenAll(tasks);
        }
    } 
}
