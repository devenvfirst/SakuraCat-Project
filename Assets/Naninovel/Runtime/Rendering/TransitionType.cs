// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.


namespace Naninovel
{
    /// <summary>
    /// Describes names of available built-in transition render effect types.
    /// </summary>
    public static class TransitionType
    {
        public const string Crossfade = nameof(Crossfade);
        public const string BandedSwirl = nameof(BandedSwirl);
        public const string Blinds = nameof(Blinds);
        public const string CircleReveal = nameof(CircleReveal);
        public const string CircleStretch = nameof(CircleStretch);
        public const string CloudReveal = nameof(CloudReveal);
        public const string Crumble = nameof(Crumble);
        public const string Dissolve = nameof(Dissolve);
        public const string DropFade = nameof(DropFade);
        public const string LineReveal = nameof(LineReveal);
        public const string Pixelate = nameof(Pixelate);
        public const string RadialBlur = nameof(RadialBlur);
        public const string RadialWiggle = nameof(RadialWiggle);
        public const string RandomCircleReveal = nameof(RandomCircleReveal);
        public const string Ripple = nameof(Ripple);
        public const string RotateCrumble = nameof(RotateCrumble);
        public const string Saturate = nameof(Saturate);
        public const string Shrink = nameof(Shrink);
        public const string SlideIn = nameof(SlideIn);
        public const string SwirlGrid = nameof(SwirlGrid);
        public const string Swirl = nameof(Swirl);
        public const string Water = nameof(Water);
        public const string Waterfall = nameof(Waterfall);
        public const string Wave = nameof(Wave);

        /// <summary>
        /// Special type for user-defined transition masks.
        /// </summary>
        public const string Custom = nameof(Custom);
    }
}
