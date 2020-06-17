// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.


namespace Naninovel
{
    /// <summary>
    /// A <see cref="IBackgroundActor"/> implementation using <see cref="BackgroundActorBehaviour"/> to represent the actor.
    /// </summary>
    /// <remarks>
    /// Resource prefab should have a <see cref="BackgroundActorBehaviour"/> component attached to the root object.
    /// Apperance and other property changes changes are routed to the events of the <see cref="BackgroundActorBehaviour"/> component.
    /// </remarks>
    public class GenericBackground : GenericActor<BackgroundActorBehaviour>, IBackgroundActor
    {
        public GenericBackground (string id, BackgroundMetadata metadata)
            : base(id, metadata) { }

    }
}
