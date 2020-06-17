// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Handles camera transform offset when camera look mode is activated.
    /// </summary>
    public class CameraLookController
    {
        [System.Serializable]
        public struct State { public bool Enabled, Gravity; public Vector2 Zone, Speed; }

        /// <summary>
        /// Whether the controller is active and is controlling the camera offset.
        /// </summary>
        public bool Enabled { get; set; }
        /// <summary>
        /// A bound box with X,Y sizes in units from the initial camera position, describing how far the camera can be moved.
        /// </summary>
        public Vector2 LookZone { get; set; }
        /// <summary>
        /// Camera movement speed (sensitivity) by X,Y axes, in units per second.
        /// </summary>
        public Vector2 LookSpeed { get; set; }
        /// <summary>
        /// Whether to automatically move camera to the initial position when the look input is not active 
        /// (eg, mouse is in the center of the sreen or analog stick is in default position).
        /// </summary>
        public bool Gravity { get; set; }

        private Vector2 position => cameraManager.Camera.transform.position;
        private readonly Vector2 origin;
        private readonly ICameraManager cameraManager;
        private IInputSampler xSampler, ySampler;

        public CameraLookController (ICameraManager cameraManager, IInputSampler xSampler, IInputSampler ySampler)
        {
            this.cameraManager = cameraManager;
            this.xSampler = xSampler;
            this.ySampler = ySampler;

            origin = cameraManager.Configuration.InitialPosition + cameraManager.Offset;
        }

        public State GetState () => new State { Enabled = Enabled, Zone = LookZone, Speed = LookSpeed, Gravity = Gravity };

        public void Update ()
        {
            if (!Enabled) return;

            var offsetX = (xSampler?.Value ?? 0) * LookSpeed.x * Time.deltaTime;
            var offsetY = (ySampler?.Value ?? 0) * LookSpeed.y * Time.deltaTime;

            if (Gravity && position != origin)
            {
                var gravX = (position.x - origin.x) * LookSpeed.x * Time.deltaTime;
                var gravY = (position.y - origin.y) * LookSpeed.y * Time.deltaTime;
                offsetX = (xSampler?.Active ?? false) && Mathf.Abs(gravX) > Mathf.Abs(offsetX) ? 0 : offsetX - gravX;
                offsetY = (ySampler?.Active ?? false) && Mathf.Abs(gravY) > Mathf.Abs(offsetY) ? 0 : offsetY - gravY;
            }

            var bounds = new Rect(origin - LookZone / 2f, LookZone);

            if (position.x + offsetX < bounds.xMin)
                offsetX = bounds.xMin - position.x;
            else if (position.x + offsetX > bounds.xMax)
                offsetX = bounds.xMax - position.x;

            if (position.y + offsetY < bounds.yMin)
                offsetY = bounds.yMin - position.y;
            else if (position.y + offsetY > bounds.yMax)
                offsetY = bounds.yMax - position.y;

            cameraManager.Camera.transform.position += new Vector3(offsetX, offsetY);
        }
    }
}
