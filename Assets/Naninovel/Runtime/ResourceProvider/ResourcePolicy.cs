// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.


namespace Naninovel
{
    /// <summary>
    /// Dictates when the resources are loaded and unloaded during script execution.
    /// </summary>
    public enum ResourcePolicy
    {
        /// <summary>
        /// All the resources required for the script execution are pre-loaded when starting 
        /// the playback and unloaded only when the script has finished playing.
        /// </summary>
        Static,
        /// <summary>
        /// Only the resources required for the next <see cref="ResourceProviderConfiguration.DynamicPolicySteps"/> commands
        /// are pre-loaded during the script execution and all the unused resources are unloaded immediately. 
        /// Use this mode when targetting platforms with strict memory limitations and it's impossible to properly orginize naninovel scripts.
        /// </summary>
        Dynamic
    }
}
