// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.


namespace Naninovel
{
    /// <summary>
    /// Provides implementations of the built-in debug console commands (editor-only).
    /// </summary>
    public static class ConsoleCommands
    {
        [ConsoleCommand]
        public static async void Reload ()
        {
            await HotReloadService.ReloadPlayedScriptAsync();
        }
    }
}
