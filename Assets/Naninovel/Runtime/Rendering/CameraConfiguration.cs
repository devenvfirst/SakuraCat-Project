// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel
{
    [System.Serializable]
    public class CameraConfiguration : Configuration
    {
        [Tooltip("The reference resolution is used to evaluate proper rendering dimensions, so that sprite assets (like backgrounds and characters) are correctly positioned on scene. As a rule of thumb, set this equal to the resolution of the background textures you make for the game.")]
        public Vector2Int ReferenceResolution = new Vector2Int(1920, 1080);
        [Tooltip("Whether to automatically correct camera's orthographic size based on the current display aspect ratio to ensure the backgrounds and characters are positioned correctly.")]
        public bool AutoCorrectOrthoSize = true;
        [Tooltip("The orthographic size to set by default when auto correction is disabled.")]
        public float DefaultOrthoSize = 5.35f;
        [Tooltip("Initial world position of the camera.")]
        public Vector3 InitialPosition = new Vector3(0, 0, -10);
        [Tooltip("Whether the camera should render in orthographic (enabled) or perspective (disabled) mode by default. Has no effect when a custom camera prefab is assigned.")]
        public bool Orthographic = true;
        [Tooltip("A prefab with a camera component to use for rendering. Will use a default one when not specified. In case you wish to set some camera properties (background color, FOV, HDR, etc) or add post-processing scripts, create a prefab with the desired camera setup and assign the prefab to this field.")]
        public Camera CustomCameraPrefab = null;
        [Tooltip("Whether to render the UI in a separate camera. This will allow to use individual configuration for the main and UI cameras and prevent post-processing (image) effects from affecting the UI at the cost of a slight rendering overhead.")]
        public bool UseUICamera = true;
        [Tooltip("A prefab with a camera component to use for UI rendering. Will use a default one when not specified. Has no effect when `UseUICamera` is disabled")]
        public Camera CustomUICameraPrefab = null;
        [Tooltip("Eeasing function to use by default for all the camera modifications (changing zoom, position, rotation, etc).")]
        public EasingType DefaultEasing = EasingType.Linear;

        [Header("Thumbnails")]
        [Tooltip("The resolution in which thumbnails to preview game save slots will be captured.")]
        public Vector2Int ThumbnailResolution = new Vector2Int(240, 140);
        [Tooltip("Whether to ignore UI layer when capturing thumbnails.")]
        public bool HideUIInThumbnails = false;

        /// <summary>
        /// Size of the scene in ortho space.
        /// </summary>
        public Vector2 ReferenceSize => (Vector2)ReferenceResolution / PixelsPerUnit;
        /// <summary>
        /// Maximum allowed camera ortho size to prevent camera from rendering areas out of <see cref="ReferenceSize"/>.
        /// </summary>
        public float MaxOrthoSize => ReferenceResolution.x / ReferenceAspect / 200f;
        /// <summary>
        /// Reference aspect ratio based on <see cref="ReferenceResolution"/>.
        /// </summary>
        public float ReferenceAspect => (float)ReferenceResolution.x / ReferenceResolution.y;
        /// <summary>
        /// PPU value evaluated from <see cref="ReferenceResolution"/> and <see cref="MaxOrthoSize"/>.
        /// </summary>
        public float PixelsPerUnit => ReferenceResolution.y / (MaxOrthoSize * 2f);

        /// <summary>
        /// Converts provided ortho scene position into world space based on <see cref="ReferenceSize"/>.
        /// </summary>
        /// <param name="scenePosition">x0y0 is at the bottom left and x1y1 is at the top right corner of the screen.</param>
        public Vector3 SceneToWorldSpace (Vector3 scenePosition)
        {
            var originPosition = -ReferenceSize / 2f;
            var resultXY = originPosition + Vector2.Scale(scenePosition, ReferenceSize);

            return new Vector3(resultXY.x, resultXY.y, scenePosition.z);
        }

        /// <summary>
        /// Inverse to <see cref="SceneToWorldSpace(Vector3)"/>.
        /// </summary>
        public Vector3 WorldToSceneSpace (Vector3 worldPosition)
        {
            var originPosition = -ReferenceSize / 2f;
            var resultXY = new Vector2 {
                x = (worldPosition.x - originPosition.x) / ReferenceSize.x,
                y = (worldPosition.y - originPosition.y) / ReferenceSize.y,
            };

            return new Vector3(resultXY.x, resultXY.y, worldPosition.z);
        }
    }
}
