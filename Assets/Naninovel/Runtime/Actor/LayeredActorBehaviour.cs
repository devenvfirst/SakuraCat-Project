// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// When applied to a <see cref="GameObject"/>, containing child objects with <see cref="SpriteRenderer"/> components (layers), 
    /// handles the composition (layers enabled state) and rendering to a texture in back to front order based on z-position and sprite order.
    /// Will prevent the child sprite renderers from being rendered by the cameras at play mode.
    /// </summary>
    [ExecuteAlways]
    public class LayeredActorBehaviour : MonoBehaviour
    {
        [Serializable]
        public class CompositionMapItem
        {
            public string Key = default;
            [TextArea(1, 5)]
            public string Composition = default;
        }

        /// <summary>
        /// Current layer composition of the actor.
        /// </summary>
        public string Composition => string.Join(splitLiteral, layers.Select(l => $"{l.Group}{(l.Enabled ? enableLiteral : disableLiteral)}{l.Name}"));

        private const string selectLiteral = ">";
        private const string enableLiteral = "+";
        private const string disableLiteral = "-";
        private const string splitLiteral = ",";
        private static readonly string[] splitLiterals = new[] { splitLiteral };

        [Tooltip("Allows to map layer composition expressions to keys; the keys can then be used to specify layered actor appearances instead of the full expressions.")]
        [SerializeField] private List<CompositionMapItem> compositionMap = new List<CompositionMapItem>();

        private Material renderMaterial;
        private LayeredActorLayer[] layers;
        private Vector2 canvasSize;

        /// <summary>
        /// Applies provided layer composition expression to the actor.
        /// </summary>
        /// <remarks>
        /// Value format: path/to/object/parent>SelectedObjName,another/path+EnabledObjName,another/path-DisabledObjName,...
        /// Select operator (>) means that only this object is enabled inside the group, others should be disabled.
        /// When no target objects provided, all the layers inside the group will be affected (recursively, including child groups).
        /// </remarks>
        public virtual void ApplyComposition (string value)
        {
            if (layers is null || layers.Length == 0 || string.IsNullOrEmpty(value)) return;

            value = value.Trim();

            if (compositionMap.Any(i => i.Key == value))
                value = compositionMap.First(i => i.Key == value).Composition;

            var expressions = value.Split(splitLiterals, StringSplitOptions.RemoveEmptyEntries);

            foreach (var expression in expressions)
            {
                if (ParseExpression(expression, selectLiteral, out var group, out var name)) // Select expression (>).
                {
                    if (string.IsNullOrEmpty(name)) // Enable all in the group, disable all in neighbour groups.
                    {
                        var parentGroup = group.Contains("/") ? group.GetBeforeLast("/") : string.Empty;
                        ForEachLayer(l => l.Group.StartsWithFast(parentGroup), l => l.Enabled = l.Group.EqualsFast(group), group);
                    }
                    else ForEachLayer(l => l.Group.EqualsFast(group), l => l.Enabled = l.Name.EqualsFast(name), group);
                }
                else if (ParseExpression(expression, enableLiteral, out group, out name)) // Enable expression (+).
                {
                    if (string.IsNullOrEmpty(name))
                        ForEachLayer(l => l.Group.StartsWithFast(group), l => l.Enabled = true, group);
                    else ForLayer(group, name, l => l.Enabled = true);
                }
                else if (ParseExpression(expression, disableLiteral, out group, out name)) // Disable expression (-).
                {
                    if (string.IsNullOrEmpty(name))
                        ForEachLayer(l => l.Group.StartsWithFast(group), l => l.Enabled = false, group);
                    else ForLayer(group, name, l => l.Enabled = false);
                }
                else
                {
                    Debug.LogWarning($"Unrecognized `{gameObject.name}` layered actor composition expression: `{expression}`.");
                    continue;
                }
            }

            bool ParseExpression (string expression, string operationLiteral, out string group, out string name)
            {
                group = expression.GetBefore(operationLiteral);
                if (group is null) { name = null; return false; }
                name = expression.Substring(group.Length + operationLiteral.Length);
                return true;
            }

            void ForEachLayer (Func<LayeredActorLayer, bool> selector, Action<LayeredActorLayer> action, string group)
            {
                var layers = this.layers.Where(selector);
                if (layers?.Count() == 0) Debug.LogWarning($"`{gameObject.name}` layered actor composition group `{group}` not found.");
                else foreach (var layer in layers)
                        action.Invoke(layer);
            }

            void ForLayer (string group, string name, Action<LayeredActorLayer> action)
            {
                var layer = this.layers.FirstOrDefault(l => l.Group.EqualsFast(group) && l.Name.EqualsFast(name));
                if (layer is null) Debug.LogWarning($"`{gameObject.name}` layered actor layer `{name}` inside composition group `{group}` not found.");
                else action.Invoke(layer);
            }
        }

        /// <summary>
        /// Renders the enabled layers scaled by <paramref name="pixelsPerUnit"/> to a temporary <see cref="RenderTexture"/>.
        /// Don't forget to releease unused render textures.
        /// </summary>
        public virtual RenderTexture Render (int pixelsPerUnit)
        {
            if (layers is null || layers.Length == 0)
            {
                Debug.LogWarning($"Can't render layered actor `{name}`: layers data is empty. Make sure the actor prefab contains child objects with at least one sprite renderer.");
                return null;
            }

            var renderDimensions = canvasSize * pixelsPerUnit;
            var renderTexture = RenderTexture.GetTemporary(Mathf.RoundToInt(renderDimensions.x), Mathf.RoundToInt(renderDimensions.y), 24, RenderTextureFormat.ARGB32);

            Graphics.SetRenderTarget(renderTexture);
            GL.Clear(true, true, Color.clear);
            GL.PushMatrix();
 
            var orthoMin = Vector3.Scale(-renderDimensions / 2f, transform.parent.localScale) + transform.position * pixelsPerUnit;
            var orthoMax = Vector3.Scale(renderDimensions / 2f, transform.parent.localScale) + transform.position * pixelsPerUnit;
            var orthoMatrix = Matrix4x4.Ortho(orthoMin.x, orthoMax.x, orthoMin.y, orthoMax.y, float.MinValue, float.MaxValue);
            var rotationMatrix = Matrix4x4.Rotate(Quaternion.Inverse(transform.parent.localRotation));
            GL.LoadProjectionMatrix(orthoMatrix);

            for (int i = 0; i < layers.Length; i++)
            {
                var layer = layers[i];
                if (!layer.Enabled) continue;
                renderMaterial.mainTexture = layer.Texture;
                renderMaterial.color = layer.SpriteRenderer.color;
                renderMaterial.SetPass(0);
                var renderPosition = transform.TransformPoint(rotationMatrix // Compensate actor (parent game object) rotation.
                    .MultiplyPoint3x4(transform.InverseTransformPoint(layer.Position)));
                var renderTransform = Matrix4x4.TRS(renderPosition * pixelsPerUnit, layer.Rotation, layer.Scale * pixelsPerUnit);
                Graphics.DrawMeshNow(layer.Mesh, renderTransform);
            }

            GL.PopMatrix();

            return renderTexture;
        }

        protected virtual void BuildLayerData ()
        {
            var spriteRenderers = GetComponentsInChildren<SpriteRenderer>()
                .Where(s => s.sprite)
                .OrderBy(s => s.sortingOrder)
                .OrderByDescending(s => s.transform.position.z);

            if (spriteRenderers.Count() == 0)
            {
                if (layers?.Length > 0)
                    Array.Clear(layers, 0, layers.Length);
                return;
            }

            var maxPosX = spriteRenderers.Max(s => Mathf.Max(Mathf.Abs(s.bounds.max.x), Mathf.Abs(s.bounds.min.x)));
            var maxPosY = spriteRenderers.Max(s => Mathf.Max(Mathf.Abs(s.bounds.max.y), Mathf.Abs(s.bounds.min.y)));
            canvasSize = new Vector2(maxPosX * 2, maxPosY * 2);

            layers = spriteRenderers.Select(s => new LayeredActorLayer(s)).ToArray();
        }

        private void Awake ()
        {
            renderMaterial = new Material(Shader.Find("Sprites/Default"));
            renderMaterial.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;

            BuildLayerData();
        }

        private void Update ()
        {
            if (!Application.isPlaying)
                BuildLayerData();
        }

        private void OnDrawGizmos ()
        {
            Gizmos.DrawWireCube(transform.position, canvasSize);
        }

        private void OnDestroy ()
        {
            if (ObjectUtils.IsValid(renderMaterial))
                ObjectUtils.DestroyOrImmediate(renderMaterial);
        }
    }
}
