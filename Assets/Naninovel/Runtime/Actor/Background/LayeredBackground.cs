// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.


namespace Naninovel
{
    /// <summary>
    /// A <see cref="IBackgroundActor"/> implementation using <see cref="LayeredActorBehaviour"/> to represent the actor.
    /// </summary>
    public class LayeredBackground : LayeredActor, IBackgroundActor
    {
        public LayeredBackground (string id, BackgroundMetadata metadata) 
            : base(id, metadata) { }

    } 
}
