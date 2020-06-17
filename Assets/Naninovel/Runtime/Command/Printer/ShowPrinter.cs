// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Shows a text printer.
    /// </summary>
    /// <example>
    /// ; Show a default printer.
    /// @showPrinter
    /// ; Show printer with ID `Wide`.
    /// @showPrinter Wide
    /// </example>
    public class ShowPrinter : PrinterCommand
    {
        /// <summary>
        /// ID of the printer actor to use. Will use a default one when not provided.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias)]
        public StringParameter PrinterId;
        /// <summary>
        /// Duration (in seconds) of the show animation.
        /// Default value for each printer is set in the actor configuration.
        /// </summary>
        [ParameterAlias("time")]
        public DecimalParameter Duration;

        protected override string AssignedPrinterId => PrinterId;

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var printer = await GetOrAddPrinterAsync();
            if (cancellationToken.IsCancellationRequested) return;

            var printerMeta = PrinterManager.Configuration.GetMetadataOrDefault(printer.Id);
            var showDuration = Assigned(Duration) ? Duration.Value : printerMeta.ChangeVisibilityDuration;

            await printer.ChangeVisibilityAsync(true, showDuration, cancellationToken: cancellationToken);
        }
    } 
}
