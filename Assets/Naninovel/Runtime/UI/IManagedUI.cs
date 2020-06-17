// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using UniRx.Async;
using UnityEngine;

namespace Naninovel.UI
{
    /// <summary>
    /// Implementation represents a UI object managed by <see cref="IUIManager"/> service.
    /// </summary>
    public interface IManagedUI
    {
        /// <summary>
        /// Event invoked when <see cref="Visible"/> of the UI object is changed.
        /// </summary>
        event Action<bool> OnVisibilityChanged;

        /// <summary>
        /// Whether the UI element is currently visible to the user.
        /// </summary>
        /// <remarks>
        /// The visibility should change instantly when set.
        /// </remarks>
        bool Visible { get; set; }
        /// <summary>
        /// The order in which the element is sorted among other elements.
        /// </summary>
        int SortingOrder { get; set; }
        /// <summary>
        /// Rendering mode of the UI element.
        /// </summary>
        RenderMode RenderMode { get; set; }
        /// <summary>
        /// Camera the UI element uses for reference when scaling and handling user input.
        /// </summary>
        Camera RenderCamera { get; set; }
        /// <summary>
        /// Whether the UI element should be subject to user interaction.
        /// </summary>
        bool Interactable { get; set; }

        /// <summary>
        /// Allows to execute an async initialization logic.
        /// Invoked once by <see cref="IUIManager"/> when managed UI is instatiated.
        /// </summary>
        UniTask InitializeAsync ();
        /// <summary>
        /// Gradually changes visibility over default (implementation-specific) or specified <paramref name="duration"/> (in seconds).
        /// </summary>
        /// <remarks>
        /// <see cref="Visible"/> should be set to <paramref name="visible"/> at the time this method is invoked.
        /// Should not return until visibility (as perceived by user) has been completely changed (including any associated animations).
        /// </remarks>
        UniTask ChangeVisibilityAsync (bool visible, float? duration = null);
        /// <summary>
        /// Applies provided font to all the text elements contained in the UI.
        /// </summary>
        void SetFont (Font font);
        /// <summary>
        /// Applies provided font size to all the text elements contained in the UI.
        /// </summary>
        void SetFontSize (int size);
    }
}
