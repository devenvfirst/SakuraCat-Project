// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;

namespace Naninovel.UI
{
    /// <summary>
    /// Represents text reveal progress state.
    /// </summary>
    public class TextRevealState
    {
        public bool InProgress { get; private set; }
        public int CharactersToReveal { get; private set; }
        public float RevealDuration { get; private set; }
        public CancellationToken CancellationToken { get; private set; }
        public int CharactersRevealed { get; set; }

        public virtual void Start (int count, float duration, CancellationToken cancellationToken)
        {
            InProgress = true;
            CharactersRevealed = 0;
            CharactersToReveal = count;
            RevealDuration = duration;
            CancellationToken = cancellationToken;
        }

        public virtual void Reset ()
        {
            InProgress = false;
            CharactersToReveal = CharactersRevealed = 0;
            RevealDuration = 0f;
            CancellationToken = default;
        }
    }
}
