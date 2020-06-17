// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Naninovel.Commands
{
    /// <summary>
    /// Modifies the main camera, changing offset, zoom level and rotation over time.
    /// Check [this video](https://youtu.be/zy28jaMss8w) for a quick demonstration of the command effect.
    /// </summary>
    /// <example>
    /// ; Offset over X-axis (pan) the camera by -3 units and offset over Y-axis by 1.5 units
    /// @camera offset:-3,1.5
    /// 
    /// ; Set camera in perspective mode, zoom-in by 50% and move back by 5 units
    /// @camera ortho:false offset:,,-5 zoom:0.5
    /// 
    /// ; Set camera in orthographic mode and roll by 10 degrees clock-wise
    /// @camera ortho:true roll:10
    /// 
    /// ; Offset, zoom and roll simultaneously animated over 5 seconds
    /// @camera offset:-3,1.5 zoom:0.5 roll:10 time:5
    /// 
    /// ; Instantly reset camera to the default state
    /// @camera offset:0,0 zoom:0 rotation:0,0,0 time:0
    /// 
    /// ; Toggle `FancyCameraFilter` and `Bloom` components attached to the camera
    /// @camera toggle:FancyCameraFilter,Bloom
    /// 
    /// ; Set `FancyCameraFilter` component enabled and `Bloom` disabled
    /// @camera set:FancyCameraFilter.true,Bloom.false
    /// </example>
    [CommandAlias("camera")]
    public class ModifyCamera : Command
    {
        /// <summary>
        /// Local camera position offset in units by X,Y,Z axes.
        /// </summary>
        public DecimalListParameter Offset;
        /// <summary>
        /// Local camera rotation by Z-axis in angle degrees (0.0 to 360.0 or -180.0 to 180.0).
        /// The same as third component of `rotation` parameter; ignored when `rotation` is specified.
        /// </summary>
        public DecimalParameter Roll;
        /// <summary>
        /// Local camera rotation over X,Y,Z-axes in angle degrees (0.0 to 360.0 or -180.0 to 180.0).
        /// </summary>
        public DecimalListParameter Rotation;
        /// <summary>
        /// Relatize camera zoom (orthographic size or field of view, depending on the render mode), in 0.0 (no zoom) to 1.0 (full zoom) range.
        /// </summary>
        public DecimalParameter Zoom;
        /// <summary>
        /// Whether the camera should render in orthographic (true) or perspective (false) mode.
        /// </summary>
        [ParameterAlias("ortho")]
        public BooleanParameter Orthographic;
        /// <summary>
        /// Names of the components to toggle (enable if disabled and vice-versa). The components should be attached to the same gameobject as the camera.
        /// This can be used to toggle [custom post-processing effects](/guide/special-effects.md#camera-effects).
        /// </summary>
        [ParameterAlias("toggle")]
        public StringListParameter ToggleTypeNames;
        /// <summary>
        /// Names of the components to enable or disable. The components should be attached to the same gameobject as the camera.
        /// This can be used to explicitly enable or disable [custom post-processing effects](/guide/special-effects.md#camera-effects).
        /// Specified components enabled state will override effect of `toggle` parameter.
        /// </summary>
        [ParameterAlias("set")]
        public NamedBooleanListParameter SetTypeNames;
        /// <summary>
        /// Name of the easing function to use for the modification.
        /// <br/><br/>
        /// Available options: Linear, SmoothStep, Spring, EaseInQuad, EaseOutQuad, EaseInOutQuad, EaseInCubic, EaseOutCubic, EaseInOutCubic, EaseInQuart, EaseOutQuart, EaseInOutQuart, EaseInQuint, EaseOutQuint, EaseInOutQuint, EaseInSine, EaseOutSine, EaseInOutSine, EaseInExpo, EaseOutExpo, EaseInOutExpo, EaseInCirc, EaseOutCirc, EaseInOutCirc, EaseInBounce, EaseOutBounce, EaseInOutBounce, EaseInBack, EaseOutBack, EaseInOutBack, EaseInElastic, EaseOutElastic, EaseInOutElastic.
        /// <br/><br/>
        /// When not specified, will use a default easing function set in the camera configuration settings.
        /// </summary>
        [ParameterAlias("easing")]
        public StringParameter EasingTypeName;
        /// <summary>
        /// Duration (in seconds) of the modification. Default value: 0.35 seconds.
        /// </summary>
        [ParameterAlias("time")]
        public DecimalParameter Duration = .35f;

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var cameraManager = Engine.GetService<ICameraManager>();

            var easingType = cameraManager.Configuration.DefaultEasing;
            if (Assigned(EasingTypeName) && !Enum.TryParse(EasingTypeName, true, out easingType))
                LogWarningWithPosition($"Failed to parse `{EasingTypeName}` easing.");

            if (Assigned(Orthographic))
                cameraManager.Camera.orthographic = Orthographic;

            if (Assigned(ToggleTypeNames))
                foreach (var name in ToggleTypeNames)
                    DoForComponent(name, c => c.enabled = !c.enabled);

            if (Assigned(SetTypeNames))
                foreach (var kv in SetTypeNames)
                    if (kv.HasValue && !string.IsNullOrWhiteSpace(kv.Name) && kv.NamedValue.HasValue) 
                        DoForComponent(kv.Name, c => c.enabled = kv.Value?.Value ?? false);

            var tasks = new List<UniTask>();

            if (Assigned(Offset)) tasks.Add(cameraManager.ChangeOffsetAsync(ArrayUtils.ToVector3(Offset, Vector3.zero), Duration, easingType, cancellationToken));
            if (Assigned(Rotation)) tasks.Add(cameraManager.ChangeRotationAsync(Quaternion.Euler(
                Rotation.ElementAtOrDefault(0) ?? cameraManager.Rotation.eulerAngles.x,
                Rotation.ElementAtOrDefault(1) ?? cameraManager.Rotation.eulerAngles.y,
                Rotation.ElementAtOrDefault(2) ?? cameraManager.Rotation.eulerAngles.z), Duration, easingType, cancellationToken));
            else if (Assigned(Roll)) tasks.Add(cameraManager.ChangeRotationAsync(Quaternion.Euler(
                cameraManager.Rotation.eulerAngles.x,
                cameraManager.Rotation.eulerAngles.y, 
                Roll), Duration, easingType, cancellationToken));
            if (Assigned(Zoom)) tasks.Add(cameraManager.ChangeZoomAsync(Zoom, Duration, easingType, cancellationToken));

            await UniTask.WhenAll(tasks);

            void DoForComponent (string componentName, Action<MonoBehaviour> action)
            {
                var cmp = cameraManager.Camera.gameObject.GetComponent(componentName) as MonoBehaviour;
                if (!cmp)
                {
                    LogWithPosition($"Failed to toggle `{componentName}` camera component; the component is not found on the camera's gameobject.");
                    return;
                }
                action.Invoke(cmp);
            }
        }
    }
}
