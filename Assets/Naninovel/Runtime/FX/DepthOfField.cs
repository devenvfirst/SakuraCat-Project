// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using Naninovel.Commands;
using System.Linq;
using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Naninovel.FX
{
    public class DepthOfField : MonoBehaviour, Spawn.IParameterized, Spawn.IAwaitable, DestroySpawned.IParameterized, DestroySpawned.IAwaitable
    {
        protected float FocusDistance { get; private set; }
        protected float FocalLength { get; private set; }
        protected float Duration { get; private set; }
        protected float StopDuration { get; private set; }

        [SerializeField] private float defaultFocusDistance = 10f;
        [SerializeField] private float defaultFocalLength = 3.75f;
        [SerializeField] private float defaultDuration = 1f;

        private readonly Tweener<FloatTween> focusDistanceTweener = new Tweener<FloatTween>();
        private readonly Tweener<FloatTween> focalLengthTweener = new Tweener<FloatTween>();
        private CameraComponent cameraComponent;

        public virtual void SetSpawnParameters (string[] parameters)
        {
            if (cameraComponent is null)
            {
                var cameraMngr = Engine.GetService<ICameraManager>().Camera;
                cameraComponent = cameraMngr.gameObject.AddComponent<CameraComponent>();
                cameraComponent.UseCameraFov = false;
            }

            if (cameraComponent.PointOfFocus != null)
            {
                cameraComponent.FocusDistance = Vector3.Dot(cameraComponent.PointOfFocus.position - cameraComponent.transform.position, cameraComponent.transform.forward);
                cameraComponent.PointOfFocus = null;
            }

            var focusObjectName = parameters?.ElementAtOrDefault(0);
            if (string.IsNullOrEmpty(focusObjectName))
                FocusDistance = Mathf.Max(0.01f, parameters?.ElementAtOrDefault(1)?.AsInvariantFloat() ?? defaultFocusDistance);
            else
            {
                var obj = GameObject.Find(focusObjectName);
                if (ObjectUtils.IsValid(obj))
                    cameraComponent.PointOfFocus = obj.transform;
                else
                {
                    Debug.LogWarning($"Failed to find game object with name `{focusObjectName}`; depth of field effect will use a default focus distance.");
                    FocusDistance = defaultFocusDistance;
                }
            }
            FocalLength = Mathf.Abs(parameters?.ElementAtOrDefault(2)?.AsInvariantFloat() ?? defaultFocalLength);
            Duration = Mathf.Abs(parameters?.ElementAtOrDefault(3)?.AsInvariantFloat() ?? defaultDuration);
        }

        public async UniTask AwaitSpawnAsync (CancellationToken cancellationToken = default) 
        {
            if (focusDistanceTweener.Running)
                focusDistanceTweener.CompleteInstantly();
            if (focalLengthTweener.Running)
                focalLengthTweener.CompleteInstantly();

            var focusDistanceTween = new FloatTween(cameraComponent.FocusDistance, FocusDistance, Duration, ApplyFocusDistance, target: cameraComponent);
            var focalLengthTween = new FloatTween(cameraComponent.FocalLength, FocalLength, Duration, ApplyFocalLength, target: cameraComponent);

            await UniTask.WhenAll(focusDistanceTweener.RunAsync(focusDistanceTween, cancellationToken), 
                focalLengthTweener.RunAsync(focalLengthTween, cancellationToken));
        }

        public void SetDestroyParameters (string[] parameters)
        {
            StopDuration = Mathf.Abs(parameters?.ElementAtOrDefault(0)?.AsInvariantFloat() ?? defaultDuration);
        }

        public async UniTask AwaitDestroyAsync (CancellationToken cancellationToken = default)
        {
            if (focusDistanceTweener.Running)
                focusDistanceTweener.CompleteInstantly();
            if (focalLengthTweener.Running)
                focalLengthTweener.CompleteInstantly();

            var focalLengthTween = new FloatTween(cameraComponent.FocalLength, 0, StopDuration, ApplyFocalLength);
            await focalLengthTweener.RunAsync(focalLengthTween, cancellationToken);
        }

        private void ApplyFocusDistance (float value)
        {
            cameraComponent.FocusDistance = value;
        }

        private void ApplyFocalLength (float value)
        {
            cameraComponent.FocalLength = value;
        }

        private void OnDestroy ()
        {
            if (cameraComponent)
                Destroy(cameraComponent);
        }

        private class CameraComponent : MonoBehaviour
        {
            public enum KernelSizeType { Small, Medium, Large, VeryLarge }

            public KernelSizeType KernelSize { get; set; } = KernelSizeType.Medium;
            public Transform PointOfFocus { get; set; } = null;
            public float FocusDistance { get; set; } = 0f;
            public float FNumber { get; set; } = 1.4f;
            public bool UseCameraFov { get; set; } = true;
            public float FocalLength { get; set; } = 0f;

            private const float filmHeight = 0.024f;

            private Camera targetCamera;
            private Material material;

            private void OnEnable ()
            {
                targetCamera = GetComponent<Camera>();

                var shader = Shader.Find("Naninovel/FX/DepthOfField");
                if (!shader.isSupported || !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
                {
                    Debug.LogWarning("Naninovel's Depth Of Field is not supported on the current platform.");
                    return;
                }

                if (material is null)
                {
                    material = new Material(shader);
                    material.hideFlags = HideFlags.HideAndDontSave;
                }

                targetCamera.depthTextureMode |= DepthTextureMode.Depth;
            }

            private void OnDestroy ()
            {
                ObjectUtils.DestroyOrImmediate(material);
            }

            private void OnRenderImage (RenderTexture source, RenderTexture destination)
            {
                // If the material hasn't been initialized because of system
                // incompatibility, just blit and return.
                if (material == null)
                {
                    Graphics.Blit(source, destination);
                    // Try to disable itself if it's Player.
                    if (Application.isPlaying) enabled = false;
                    return;
                }

                var width = source.width;
                var height = source.height;
                var format = RenderTextureFormat.ARGBHalf;

                SetUpShaderParameters(source);

                // Pass #1 - Downsampling, prefiltering and CoC calculation
                var rt1 = RenderTexture.GetTemporary(width / 2, height / 2, 0, format);
                source.filterMode = FilterMode.Point;
                Graphics.Blit(source, rt1, material, 0);

                // Pass #2 - Bokeh simulation
                var rt2 = RenderTexture.GetTemporary(width / 2, height / 2, 0, format);
                rt1.filterMode = FilterMode.Bilinear;
                Graphics.Blit(rt1, rt2, material, 1 + (int)KernelSize);

                // Pass #3 - Additional blur
                rt2.filterMode = FilterMode.Bilinear;
                Graphics.Blit(rt2, rt1, material, 5);

                // Pass #4 - Upsampling and composition
                material.SetTexture("_BlurTex", rt1);
                Graphics.Blit(source, destination, material, 6);

                RenderTexture.ReleaseTemporary(rt1);
                RenderTexture.ReleaseTemporary(rt2);
            }

            private float CalculateFocalLength ()
            {
                if (!UseCameraFov) return FocalLength;
                var fov = targetCamera.fieldOfView * Mathf.Deg2Rad;
                return 0.5f * filmHeight / Mathf.Tan(0.5f * fov);
            }

            private float CalculateMaxCoCRadius (int screenHeight)
            {
                // Estimate the allowable maximum radius of CoC from the kernel
                // size (the equation below was empirically derived).
                var radiusInPixels = (float)KernelSize * 4 + 6;

                // Applying a 5% limit to the CoC radius to keep the size of
                // TileMax/NeighborMax small enough.
                return Mathf.Min(0.05f, radiusInPixels / screenHeight);
            }

            private void SetUpShaderParameters (RenderTexture source)
            {
                var dist = PointOfFocus != null ? Vector3.Dot(PointOfFocus.position - targetCamera.transform.position, targetCamera.transform.forward) : FocusDistance;
                var f = CalculateFocalLength();
                var s1 = Mathf.Max(dist, f);
                material.SetFloat("_Distance", s1);

                var coeff = f * f / (FNumber * (s1 - f) * filmHeight * 2);
                material.SetFloat("_LensCoeff", coeff);

                var maxCoC = CalculateMaxCoCRadius(source.height);
                material.SetFloat("_MaxCoC", maxCoC);
                material.SetFloat("_RcpMaxCoC", 1 / maxCoC);

                var rcpAspect = (float)source.height / source.width;
                material.SetFloat("_RcpAspect", rcpAspect);
            }
        }
    }
}
