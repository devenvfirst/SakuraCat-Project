// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Hides a text printer.
    /// </summary>
    /// <example>
    /// ; Hide a default printer.
    /// @hidePrinter
    /// ; Hide printer with ID `Wide`.
    /// @hidePrinter Wide
    /// </example>
    public class HidePrinter : PrinterCommand
    {
        /// <summary>
        /// ID of the printer actor to use. Will use a default one when not provided.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias)]
        public StringParameter PrinterId;
        /// <summary>
        /// Duration (in seconds) of the hide animation.
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
            var hideDuration = Assigned(Duration) ? Duration.Value : printerMeta.ChangeVisibilityDuration;

            await printer.ChangeVisibilityAsync(false, hideDuration, cancellationToken: cancellationToken);
        }
    } 
}
