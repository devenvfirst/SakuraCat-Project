// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.UI
{
    public class SceneTransitionUI : CustomUI, ISceneTransitionUI
    {
        protected override string[] AllowedSamplers => new[] { InputConfiguration.RollbackName };
        protected RawImage Image => image;

        [SerializeField] private RawImage image = default;

        private readonly Tweener<FloatTween> transitionTweener = new Tweener<FloatTween>();
        private ICameraManager cameraManager;
        private TransitionalSpriteMaterial material;
        private RenderTexture sceneTexture;

        public virtual void CaptureScene ()
        {
            if (sceneTexture)
                RenderTexture.ReleaseTemporary(sceneTexture);

            sceneTexture = RenderTexture.GetTemporary(cameraManager.Camera.scaledPixelWidth, cameraManager.Camera.scaledPixelHeight);
            var initialRenderTexture = cameraManager.Camera.targetTexture;
            cameraManager.Camera.targetTexture = sceneTexture;
            cameraManager.Camera.Render();
            cameraManager.Camera.targetTexture = initialRenderTexture;

            material.TransitionProgress = 0;
            material.MainTexture = sceneTexture;
            image.texture = sceneTexture;
            SetVisibility(true);
        }

        public virtual async UniTask TransitionAsync (Transition transition, float duration, EasingType easingType = EasingType.Linear, CancellationToken cancellationToken = default)
        {
            if (transitionTweener.Running)
                transitionTweener.CompleteInstantly();

            material.UpdateRandomSeed();
            material.TransitionProgress = 0;
            material.TransitionName = transition.Name;
            material.TransitionParams = transition.Parameters;
            if (ObjectUtils.IsValid(transition.DissolveTexture))
                material.DissolveTexture = transition.DissolveTexture;

            var transitionTexture = RenderTexture.GetTemporary(cameraManager.Camera.scaledPixelWidth, cameraManager.Camera.scaledPixelHeight);
            var initialRenderTexture = cameraManager.Camera.targetTexture;
            cameraManager.Camera.targetTexture = transitionTexture;
            material.TransitionTexture = transitionTexture;

            var tween = new FloatTween(material.TransitionProgress, 1, duration, value => material.TransitionProgress = value, false, easingType, material);
            await transitionTweener.RunAsync(tween, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                // Try restore camera target texture before cancellation, otherwise it'll mess when rolling back.
                if (cameraManager != null && cameraManager.Camera)
                    cameraManager.Camera.targetTexture = initialRenderTexture;
                RenderTexture.ReleaseTemporary(transitionTexture);
                return;
            }

            cameraManager.Camera.targetTexture = initialRenderTexture;
            SetVisibility(false);
            RenderTexture.ReleaseTemporary(transitionTexture);

            // In case of rollbacks, revert to the original scene texture.
            material.TransitionProgress = 0;
        }

        protected override void Awake ()
        {
            base.Awake();

            this.AssertRequiredObjects(image);

            cameraManager = Engine.GetService<ICameraManager>();
            material = new TransitionalSpriteMaterial(TransitionalSpriteMaterial.Variant.Default);
            image.material = material;
        }

        protected override void OnDestroy ()
        {
            if (sceneTexture)
                RenderTexture.ReleaseTemporary(sceneTexture);

            base.OnDestroy();
        }
    }
}
