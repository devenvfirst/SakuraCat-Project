// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using Naninovel.Commands;
using System.Linq;
using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Naninovel.FX
{
    public class DigitalGlitch : MonoBehaviour, Spawn.IParameterized, Spawn.IAwaitable
    {
        protected float Duration { get; private set; }
        protected float Intensity { get; private set; }

        protected ISpawnManager SpawnManager => Engine.GetService<ISpawnManager>();
        protected virtual string SpawnedPath { get; private set; }

        [SerializeField] private Shader glitchShader = default;
        [SerializeField] private Texture glitchTexture = default;
        [SerializeField] private float defaultDuration = 1f;
        [SerializeField] private float defaultIntensity = 1f;

        private CameraComponent cameraComponent;

        public virtual void SetSpawnParameters (string[] parameters)
        {
            this.AssertRequiredObjects(glitchShader, glitchTexture);

            SpawnedPath = gameObject.name;

            Duration = Mathf.Abs(parameters?.ElementAtOrDefault(0)?.AsInvariantFloat() ?? defaultDuration);
            Intensity = Mathf.Abs(parameters?.ElementAtOrDefault(1)?.AsInvariantFloat() ?? defaultIntensity);
        }

        public async UniTask AwaitSpawnAsync (CancellationToken cancellationToken = default) 
        {
            if (cameraComponent is null)
            {
                var cameraMngr = Engine.GetService<ICameraManager>().Camera;
                cameraComponent = cameraMngr.gameObject.AddComponent<CameraComponent>();
                cameraComponent.Shader = glitchShader;
                cameraComponent.GlitchTexture = glitchTexture;
            }
            cameraComponent.Intensity = Intensity;

            await UniTask.Delay(System.TimeSpan.FromSeconds(Duration));
            if (cancellationToken.IsCancellationRequested) return;

            if (SpawnManager.IsObjectSpawned(SpawnedPath))
                SpawnManager.DestroySpawnedObject(SpawnedPath);
        }

        private void OnDestroy ()
        {
            if (cameraComponent)
                Destroy(cameraComponent);
        }

        private class CameraComponent : MonoBehaviour
        {
            public Shader Shader;
            public Texture GlitchTexture;
            public float Intensity = 1.0f;
            public bool RandomGlitchFrequency = true;
            public float GlitchFrequency = .5f;
            public bool PerformUVShifting = true;
            public float ShiftAmount = 1f;
            public bool PerformScreenShifting = true;
            public bool PerformColorShifting = true;
            public Color TintColor = new Color(.2f, .2f, 0f, 0f);
            public bool BurnColors = true;
            public bool DodgeColors = false;

            protected Material Material
            {
                get
                {
                    if (material is null)
                    {
                        material = new Material(Shader);
                        material.SetTexture("_GlitchTex", GlitchTexture);
                        material.SetTextureScale("_GlitchTex", new Vector2(Screen.width / (float)GlitchTexture.width, Screen.height / (float)GlitchTexture.height));
                        material.hideFlags = HideFlags.HideAndDontSave;
                    }
                    return material;
                }
            }

            private float glitchUp, glitchDown, flicker, glitchUpTime = .05f, glitchDownTime = .05f, flickerTime = .5f;

            private Material material;

            private void Start ()
            {
                material = null; // force to reinit the material on scene start

                if (!Shader || !Shader.isSupported)
                    enabled = false;

                flickerTime = RandomGlitchFrequency ? Random.value : 1f - GlitchFrequency;
                glitchUpTime = RandomGlitchFrequency ? Random.value : .1f - GlitchFrequency / 10f;
                glitchDownTime = RandomGlitchFrequency ? Random.value : .1f - GlitchFrequency / 10f;
            }

            private void OnDisable ()
            {
                if (material) Destroy(material);
            }

            private void OnRenderImage (RenderTexture source, RenderTexture destination)
            {
                Material.SetFloat("_Intensity", Intensity);
                Material.SetColor("_ColorTint", TintColor);
                Material.SetFloat("_BurnColors", BurnColors ? 1 : 0);
                Material.SetFloat("_DodgeColors", DodgeColors ? 1 : 0);
                Material.SetFloat("_PerformUVShifting", PerformUVShifting ? 1 : 0);
                Material.SetFloat("_PerformColorShifting", PerformColorShifting ? 1 : 0);
                Material.SetFloat("_PerformScreenShifting", PerformScreenShifting ? 1 : 0);

                if (Intensity == 0) Material.SetFloat("filterRadius", 0);

                glitchUp += Time.deltaTime * Intensity;
                glitchDown += Time.deltaTime * Intensity;
                flicker += Time.deltaTime * Intensity;

                if (flicker > flickerTime)
                {
                    Material.SetFloat("filterRadius", Random.Range(-3f, 3f) * Intensity * ShiftAmount);
                    Material.SetTextureOffset("_GlitchTex", new Vector2(Random.Range(-3f, 3f), Random.Range(-3f, 3f)));
                    flicker = 0;
                    flickerTime = RandomGlitchFrequency ? Random.value : 1f - GlitchFrequency;
                }

                if (glitchUp > glitchUpTime)
                {
                    if (Random.Range(0f, 1f) < .1f * Intensity) Material.SetFloat("flipUp", Random.Range(0f, 1f) * Intensity);
                    else Material.SetFloat("flipUp", 0);

                    glitchUp = 0;
                    glitchUpTime = RandomGlitchFrequency ? Random.value / 10f : .1f - GlitchFrequency / 10f;
                }

                if (glitchDown > glitchDownTime)
                {
                    if (Random.Range(0f, 1f) < .1f * Intensity) Material.SetFloat("flipDown", 1f - Random.Range(0f, 1f) * Intensity);
                    else Material.SetFloat("flipDown", 1f);

                    glitchDown = 0;
                    glitchDownTime = RandomGlitchFrequency ? Random.value / 10f : .1f - GlitchFrequency / 10f;
                }

                if (Random.Range(0f, 1f) < .1f * Intensity * (RandomGlitchFrequency ? 1 : GlitchFrequency))
                    Material.SetFloat("displace", Random.value * Intensity);
                else Material.SetFloat("displace", 0);

                Graphics.Blit(source, destination, Material);
            }
        }
    }
}
