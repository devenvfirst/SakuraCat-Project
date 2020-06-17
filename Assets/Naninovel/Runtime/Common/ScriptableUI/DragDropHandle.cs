// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Naninovel
{
    /// <summary>
    /// Used by <see cref="DragDrop"/>.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class DragDropHandle : MonoBehaviour, IDragHandler
    {
        public event Action<Vector2> OnDragged;

        public void OnDrag (PointerEventData eventData)
        {
            OnDragged?.Invoke(eventData.position);
        }
    }
}