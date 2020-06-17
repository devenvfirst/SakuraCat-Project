// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Naninovel
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer)), DisallowMultipleComponent]
    public class TransitionalSpriteRenderer : MonoBehaviour
    {
        public Texture MainTexture { get => material.MainTexture; set { material.MainTexture = depthMaterial.MainTexture = value; RebuildMeshQuad (); } }
        public Texture TransitionTexture { get => material.TransitionTexture; set { material.TransitionTexture = value; depthMaterial.TransitionTexture = value; RebuildMeshQuad (); } }
        public Texture DissolveTexture { get => material.DissolveTexture; set { material.DissolveTexture = value; depthMaterial.DissolveTexture = value; } }
        public string TransitionName { get => material.TransitionName; set { material.TransitionName = value; depthMaterial.TransitionName = value; } }
        public float TransitionProgress { get => material.TransitionProgress; set { material.TransitionProgress = value; depthMaterial.TransitionProgress = value; } }
        public Vector4 TransitionParams { get => material.TransitionParams; set { material.TransitionParams = value; depthMaterial.TransitionParams = value; } }
        public Vector2 RandomSeed { get => material.RandomSeed; set { material.RandomSeed = value; depthMaterial.RandomSeed = value; } }
        public Color TintColor { get => material.TintColor; set { material.TintColor = value; depthMaterial.TintColor = value; } }
        public float Opacity { get => material.Opacity; set { material.Opacity = value; depthMaterial.Opacity = value; } }
        public bool FlipX { get => material.FlipX; set { material.FlipX = value; depthMaterial.FlipX = value; } }
        public bool FlipY { get => material.FlipY; set { material.FlipY = value; depthMaterial.FlipY = value; } }
        public bool DepthPassEnabled { get => depthMaterial.DepthPassEnabled; set => depthMaterial.DepthPassEnabled = value; }
        public float DepthAlphaCutoff { get => depthMaterial.DepthAlphaCutoff; set => depthMaterial.DepthAlphaCutoff = value; }
        public Shader CustomShader { get => customShader; set { customShader = value; if (value) InitializeMeshRenderer(); } }
        public Vector2 Pivot { get => pivot; set { if (value != Pivot) { pivot = value; RebuildMeshQuad(); }; } }
        public int PixelsPerUnit { get => pixelsPerUnit; set { if (value != PixelsPerUnit) { pixelsPerUnit = value; RebuildMeshQuad(); }; } }
        public Transition Transition
        {
            get => new Transition(TransitionName, TransitionParams, DissolveTexture);
            set { TransitionName = value.Name; TransitionParams = value.Parameters; DissolveTexture = value.DissolveTexture; }
        }

        private readonly Tweener<FloatTween> transitionTweener = new Tweener<FloatTween>();
        private readonly Tweener<ColorTween> colorTweener = new Tweener<ColorTween>();
        private readonly Tweener<FloatTween> fadeTweener = new Tweener<FloatTween>();
        private readonly List<Vector3> vertices = new Vector3[4].ToList();
        private readonly List<Vector2> mainUVs = new Vector2[4].ToList();
        private readonly List<Vector2> transitionUVs = new Vector2[4].ToList();
        private readonly List<int> triangles = new List<int> { 0, 1, 2, 3, 2, 1 };
        private Shader customShader;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private TransitionalSpriteMaterial material, depthMaterial;
        private Vector2 pivot = Vector2.one * .5f;
        private int pixelsPerUnit = 100;

        private void Awake ()
        {
            InitializeMeshFilter();
            InitializeMeshRenderer();
        }

        private void OnEnable ()
        {
            meshRenderer.enabled = true;
            material.UpdateRandomSeed();
        }

        private void OnDisable ()
        {
            meshRenderer.enabled = false;
        }

        public async UniTask TransitionToAsync (Texture texture, float duration, EasingType easingType = default,
            Transition? transition = default, CancellationToken cancellationToken = default)
        {
            if (transitionTweener.Running)
            {
                transitionTweener.CompleteInstantly();
                await AsyncUtils.WaitEndOfFrame; // Materials are updated later in render loop, so wait before further modifications.
                if (cancellationToken.IsCancellationRequested) return;
            }

            if (transition.HasValue)
                Transition = transition.Value;

            if (duration <= 0)
            {
                MainTexture = texture;
                TransitionProgress = 0;
                return;
            }
            else
            {
                if (!ObjectUtils.IsValid(MainTexture))
                    MainTexture = texture;

                TransitionTexture = texture;
                var tween = new FloatTween(TransitionProgress, 1, duration, value => TransitionProgress = value, false, easingType, this);
                await transitionTweener.RunAsync(tween, cancellationToken);
                if (cancellationToken.IsCancellationRequested) return;
                MainTexture = TransitionTexture;
                TransitionProgress = 0;
            }
        }

        public async UniTask TintToAsync (Color color, float duration, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            if (colorTweener.Running) colorTweener.CompleteInstantly();

            if (duration <= 0)
            {
                TintColor = color;
                return;
            }

            var tween = new ColorTween(TintColor, color, ColorTweenMode.All, duration, value => TintColor = value, false, easingType, this);
            await colorTweener.RunAsync(tween, cancellationToken);
        }

        public async UniTask FadeToAsync (float opacity, float duration, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            if (fadeTweener.Running) fadeTweener.CompleteInstantly();

            if (duration <= 0)
            {
                Opacity = opacity;
                return;
            }

            var tween = new FloatTween(Opacity, opacity, duration, value => Opacity = value, false, easingType, this);
            await fadeTweener.RunAsync(tween, cancellationToken);
        }

        public async UniTask FadeOutAsync (float duration, EasingType easingType = default, CancellationToken cancellationToken = default) => await FadeToAsync(0, duration, easingType, cancellationToken);

        public async UniTask FadeInAsync (float duration, EasingType easingType = default, CancellationToken cancellationToken = default) => await FadeToAsync(1, duration, easingType, cancellationToken);

        private void InitializeMeshFilter ()
        {
            if (!meshFilter)
            {
                meshFilter = GetComponent<MeshFilter>();
                if (!meshFilter) meshFilter = gameObject.AddComponent<MeshFilter>();
                meshFilter.hideFlags = HideFlags.HideInInspector;
            }

            meshFilter.mesh = new Mesh();
            meshFilter.mesh.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
            meshFilter.mesh.name = "Generated Quad Mesh (Instance)";
        }

        private void InitializeMeshRenderer ()
        {
            if (!meshRenderer)
            {
                meshRenderer = GetComponent<MeshRenderer>();
                if (!meshRenderer) meshRenderer = gameObject.AddComponent<MeshRenderer>();
                meshRenderer.hideFlags = HideFlags.HideInInspector;
            }

            material = new TransitionalSpriteMaterial(TransitionalSpriteMaterial.Variant.Default, customShader);
            depthMaterial = new TransitionalSpriteMaterial(TransitionalSpriteMaterial.Variant.Depth, customShader);

            meshRenderer.materials = new[] {
                material,
                depthMaterial,
            };
        }

        private void RebuildMeshQuad ()
        {
            if (!meshFilter || !MainTexture) return;

            meshFilter.mesh.Clear();

            // Find required texture sizes.
            var textureWidth = TransitionTexture && TransitionTexture.width > MainTexture.width ? TransitionTexture.width : MainTexture.width;
            var textureHeight = TransitionTexture && TransitionTexture.height > MainTexture.height ? TransitionTexture.height : MainTexture.height;

            // Setup vertices.
            var quadHalfWidth = textureWidth * .5f / PixelsPerUnit;
            var quadHalfHeight = textureHeight * .5f / PixelsPerUnit;
            vertices[0] = new Vector3(-quadHalfWidth, -quadHalfHeight, 0);
            vertices[1] = new Vector3(-quadHalfWidth, quadHalfHeight, 0);
            vertices[2] = new Vector3(quadHalfWidth, -quadHalfHeight, 0);
            vertices[3] = new Vector3(quadHalfWidth, quadHalfHeight, 0);

            // Setup main texture UVs.
            var mainScaleRatioX = textureWidth / (float)MainTexture.width - 1;
            var mainScaleRatioY = textureHeight / (float)MainTexture.height - 1;
            var mainMaxX = 1 + mainScaleRatioX * (1 - Pivot.x);
            var mainMaxY = 1 + mainScaleRatioY * (1 - Pivot.y);
            var mainMinX = 0 - mainScaleRatioX * Pivot.x;
            var mainMinY = 0 - mainScaleRatioY * Pivot.y;
            mainUVs[0] = new Vector2(mainMinX, mainMinY);
            mainUVs[1] = new Vector2(mainMinX, mainMaxY);
            mainUVs[2] = new Vector2(mainMaxX, mainMinY);
            mainUVs[3] = new Vector2(mainMaxX, mainMaxY);

            if (TransitionTexture)
            {
                // Setup transition texture UVs.
                var transitionScaleRatioX = textureWidth / (float)TransitionTexture.width - 1;
                var transitionScaleRatioY = textureHeight / (float)TransitionTexture.height - 1;
                var transitionMaxX = 1 + transitionScaleRatioX * (1 - Pivot.x);
                var transitionMaxY = 1 + transitionScaleRatioY * (1 - Pivot.y);
                var transitionMinX = 0 - transitionScaleRatioX * Pivot.x;
                var transitionMinY = 0 - transitionScaleRatioY * Pivot.y;
                transitionUVs[0] = new Vector2(transitionMinX, transitionMinY);
                transitionUVs[1] = new Vector2(transitionMinX, transitionMaxY);
                transitionUVs[2] = new Vector2(transitionMaxX, transitionMinY);
                transitionUVs[3] = new Vector2(transitionMaxX, transitionMaxY);
            }

            // Apply pivot.
            UpdatePivot();

            // Create quad.
            meshFilter.mesh.SetVertices(vertices);
            meshFilter.mesh.SetUVs(0, mainUVs);
            meshFilter.mesh.SetUVs(1, transitionUVs);
            meshFilter.mesh.SetTriangles(triangles, 0);
        }

        /// <summary>
        /// Corrects geometry data to to match current pivot value.
        /// </summary>
        private void UpdatePivot ()
        {
            var spriteRect = EvaluateSpriteRect();

            var curPivot = new Vector2(-spriteRect.min.x / spriteRect.size.x, -spriteRect.min.y / spriteRect.size.y);
            if (curPivot == Pivot) return;

            var curDeltaX = spriteRect.size.x * curPivot.x;
            var curDeltaY = spriteRect.size.y * curPivot.y;
            var newDeltaX = spriteRect.size.x * Pivot.x;
            var newDeltaY = spriteRect.size.y * Pivot.y;

            var deltaPos = new Vector3(newDeltaX - curDeltaX, newDeltaY - curDeltaY);

            for (int i = 0; i < vertices.Count; i++)
                vertices[i] -= deltaPos;
        }

        /// <summary>
        /// Calculates sprite rectangle using vertex data.
        /// </summary>
        private Rect EvaluateSpriteRect ()
        {
            var minVertPos = new Vector2(vertices.Min(v => v.x), vertices.Min(v => v.y));
            var maxVertPos = new Vector2(vertices.Max(v => v.x), vertices.Max(v => v.y));
            var spriteSizeX = Mathf.Abs(maxVertPos.x - minVertPos.x);
            var spriteSizeY = Mathf.Abs(maxVertPos.y - minVertPos.y);
            var spriteSize = new Vector2(spriteSizeX, spriteSizeY);
            return new Rect(minVertPos, spriteSize);
        }
    } 
}
