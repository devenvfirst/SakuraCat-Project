// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;

namespace Naninovel
{
    /// <summary>
    /// Implementation is able to manage <see cref="IResourceProvider"/> objects.
    /// </summary>
    public interface IResourceProviderManager : IEngineService<ResourceProviderConfiguration>
    {
        /// <summary>
        /// Event invoked when a message is logged by a managed provider.
        /// </summary>
        event Action<string> OnProviderMessage;

        /// <summary>
        /// Checks whether a resource provider of provided type (assembly-qualified name) is available.
        /// </summary>
        bool ProviderInitialized (string providerType);
        /// <summary>
        /// Returns a resource provider of the requested type (assembly-qualified name).
        /// </summary>
        IResourceProvider GetProvider (string providerType);
        /// <summary>
        /// Returns resource providers of the the requested types (assembly-qualified names), in the requested order.
        /// </summary>
        List<IResourceProvider> GetProviders (List<string> providerTypes);
    } 
}
