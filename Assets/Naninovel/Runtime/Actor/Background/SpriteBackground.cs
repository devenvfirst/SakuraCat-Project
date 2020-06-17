// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.


namespace Naninovel
{
    /// <summary>
    /// A <see cref="IBackgroundActor"/> implementation using <see cref="SpriteActor"/> to represent the actor.
    /// </summary>
    public class SpriteBackground : SpriteActor, IBackgroundActor
    {
        public SpriteBackground (string id, BackgroundMetadata metadata) 
            : base(id, metadata) { }

    } 
}
