// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Represents a layer inside <see cref="LayeredActorBehaviour"/> object.
    /// </summary>
    public class LayeredActorLayer
    {
        public readonly string Name;
        public readonly string Group = string.Empty;
        public readonly Mesh Mesh;
        public readonly SpriteRenderer SpriteRenderer;
        public bool Enabled { get => SpriteRenderer.enabled; set => SpriteRenderer.enabled = value; }
        public Vector2 Position => SpriteRenderer.transform.position;
        public Quaternion Rotation => SpriteRenderer.transform.localRotation;
        public Vector2 Scale => SpriteRenderer.transform.lossyScale;
        public Texture Texture => SpriteRenderer.sprite.texture;

        public LayeredActorLayer (SpriteRenderer spriteRenderer)
        {
            this.SpriteRenderer = spriteRenderer;
            if (Application.isPlaying)
                spriteRenderer.forceRenderingOff = true;
            Mesh = BuildSpriteMesh(spriteRenderer);
            Name = spriteRenderer.gameObject.name;

            var transform = spriteRenderer.transform.parent;
            while (transform != null && !transform.TryGetComponent<LayeredActorBehaviour>(out _))
            {
                Group = transform.name + (string.IsNullOrEmpty(Group) ? string.Empty : $"/{Group}");
                transform = transform.parent;
            }
        }

        public override bool Equals (object obj) => obj is LayeredActorLayer layer && Equals(layer);

        public bool Equals (LayeredActorLayer other) => Group == other.Group && Name == other.Name;

        public override int GetHashCode ()
        {
            var hashCode = -570022382;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Group);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            return hashCode;
        }

        public static bool operator == (LayeredActorLayer left, LayeredActorLayer right) => left.Equals(right);

        public static bool operator != (LayeredActorLayer left, LayeredActorLayer right) => !(left == right);

        private static Mesh BuildSpriteMesh (SpriteRenderer spriteRenderer)
        {
            var sprite = spriteRenderer.sprite;
            var mesh = new Mesh();
            mesh.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
            mesh.name = $"{sprite.name} Sprite Mesh";
            mesh.vertices = Array.ConvertAll(sprite.vertices, i => new Vector3(i.x * (spriteRenderer.flipX ? -1 : 1), i.y * (spriteRenderer.flipY ? -1 : 1)));
            mesh.uv = sprite.uv;
            mesh.triangles = Array.ConvertAll(sprite.triangles, i => (int)i);
            return mesh;
        }
    }
}
