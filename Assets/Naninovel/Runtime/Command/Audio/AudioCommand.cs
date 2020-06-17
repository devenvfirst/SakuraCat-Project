// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.


namespace Naninovel.Commands
{
    /// <summary>
    /// A base implementation for the audio-related commands.
    /// </summary>
    public abstract class AudioCommand : Command
    {
        protected IAudioManager AudioManager => Engine.GetService<IAudioManager>();
    } 
}
