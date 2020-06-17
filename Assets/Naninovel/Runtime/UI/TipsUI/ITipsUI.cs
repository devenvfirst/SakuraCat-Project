// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.


namespace Naninovel.UI
{
    /// <summary>
    /// Represents a set of UI elements used for browsing unlockable tips.
    /// </summary>
    public interface ITipsUI : IManagedUI
    {
        /// <summary>
        /// Number of existing unlockable tip items, independent of the unlock state.
        /// </summary>
        int TipsCount { get; }
    }
}
