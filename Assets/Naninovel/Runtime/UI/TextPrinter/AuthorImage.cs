// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.UI
{
    /// <summary>
    /// Represents image of the current text message author (character) avatar.
    /// </summary>
    [RequireComponent(typeof(RawImage), typeof(CanvasGroup))]
    public class AuthorImage : ScriptableUIBehaviour
    {
        protected virtual string ShaderName => "Naninovel/TransitionalUI";
        protected virtual Texture MainTexture { get => rawImage.texture; set => rawImage.texture = value ? value : Texture2D.blackTexture; }
        protected virtual Texture TransitionTexture { get => material.GetTexture(transitionTexId); set => material.SetTexture(transitionTexId, value); }
        protected virtual float TransitionProgress { get => material.GetFloat(transitionProgressId); set => material.SetFloat(transitionProgressId, value); }

        private static readonly int transitionTexId = Shader.PropertyToID("_TransitionTex");
        private static readonly int transitionProgressId = Shader.PropertyToID("_TransitionProgress");

        private RawImage rawImage;
        private Material material;
        private Tweener<FloatTween> transitionTweener;

        /// <summary>
        /// Crossfades current image's texture with the provided one over <see cref="ScriptableUIBehaviour.FadeTime"/>.
        /// When null is provided, will hide the image instead.
        /// </summary>
        public virtual async UniTask ChangeTextureAsync (Texture texture, CancellationToken cancellationToken = default)
        {
            if (transitionTweener.Running)
            {
                // Don't start again if already transitioning to the same texture.
                if (texture == TransitionTexture) return;
                transitionTweener.CompleteInstantly();
            }

            TransitionTexture = texture;
            var tween = new FloatTween(TransitionProgress, 1, FadeTime, value => TransitionProgress = value, false, target: material);
            await transitionTweener.RunAsync(tween, cancellationToken);

            // To prevent concurrency issues, make sure nothing is executed after the above async transition task.
        }

        protected override void Awake ()
        {
            base.Awake();

            transitionTweener = new Tweener<FloatTween>(FinishTransition);
            rawImage = GetComponent<RawImage>();

            material = new Material(Shader.Find(ShaderName));
            material.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
            rawImage.material = material;

            MainTexture = TransitionTexture = null;
        }

        private void FinishTransition ()
        {
            MainTexture = TransitionTexture;
            TransitionProgress = 0;
        }
    }
}
