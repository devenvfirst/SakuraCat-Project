// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Allows detecting touch swipes by sampling user input.
    /// </summary>
    [Serializable]
    public class InputSwipeTrigger
    {
        [Tooltip("Swipe of which direction should be registered.")]
        public InputSwipeDirection Direction = default;
        [Tooltip("How much fingers (touches) should be active to register the swipe."), Range(1, 5)]
        public int FingerCount = 1;
        [Tooltip("Minimum required swipe speed to activate the trigger, in screen factor per second.")]
        public float MinimumSpeed = 2f;

        /// <summary>
        /// Returns whether the swipe is currently registered.
        /// </summary>
        public bool Sample ()
        {
            #if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount != FingerCount) return false;

            for (int i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                var speed = new Vector2(touch.deltaPosition.x / Screen.width, touch.deltaPosition.y / Screen.height) / touch.deltaTime;

                if (Mathf.Abs(touch.deltaPosition.x) > Mathf.Abs(touch.deltaPosition.y))
                {
                    if (Direction == InputSwipeDirection.Left && speed.x <= -MinimumSpeed) return true;
                    if (Direction == InputSwipeDirection.Right && speed.x >= MinimumSpeed) return true;
                }
                else
                {
                    if (Direction == InputSwipeDirection.Up && speed.y >= MinimumSpeed) return true;
                    if (Direction == InputSwipeDirection.Down && speed.y <= -MinimumSpeed) return true;
                }
            }
            #endif
            return false;
        }
    }
}