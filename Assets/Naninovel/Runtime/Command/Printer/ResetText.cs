// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Resets (clears) the contents of a text printer.
    /// </summary>
    /// <example>
    /// ; Clear the content of a default printer.
    /// @resetText
    /// ; Clear the content of a printer with ID `Fullscreen`.
    /// @resetText Fullscreen
    /// </example>
    public class ResetText : PrinterCommand
    {
        /// <summary>
        /// ID of the printer actor to use. Will use a default one when not provided.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias)]
        public StringParameter PrinterId;

        protected override string AssignedPrinterId => PrinterId;

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var printer = await GetOrAddPrinterAsync();
            if (cancellationToken.IsCancellationRequested) return;

            printer.Text = string.Empty;
            printer.RevealProgress = 0f;
        }
    } 
}
