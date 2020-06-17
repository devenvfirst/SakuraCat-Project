// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Naninovel.Commands
{
    /// <summary>
    /// Modifies a [text printer actor](/guide/text-printers.md).
    /// </summary>
    /// <example>
    /// ; Will make `Wide` printer default and hide any other visible printers.
    /// @printer Wide
    /// 
    /// ; Will assign `Right` appearance to `Bubble` printer, make is default,
    /// ; position at the center of the screen and won't hide other printers.
    /// @printer Bubble.Right pos:50,50 hideOther:false
    /// </example>
    [CommandAlias("printer")]
    public class ModifyTextPrinter : PrinterCommand, Command.IPreloadable
    {
        /// <summary>
        /// ID of the printer to modify and the appearance to set. 
        /// When ID or appearance are not provided, will use default ones.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias)]
        public NamedStringParameter IdAndAppearance;
        /// <summary>
        /// Whether to make the printer the default one.
        /// Default printer will be subject of all the printer-related commands when `printer` parameter is not specified.
        /// </summary>
        [ParameterAlias("default")]
        public BooleanParameter MakeDefault = true;
        /// <summary>
        /// Whether to hide all the other printers.
        /// </summary>
        public BooleanParameter HideOther = true;
        /// <summary>
        /// Position (relative to the screen borders, in percents) to set for the modified printer.
        /// Position is described as follows: `0,0` is the bottom left, `50,50` is the center and `100,100` is the top right corner of the screen.
        /// </summary>
        [ParameterAlias("pos")]
        public DecimalListParameter ScenePosition;
        /// <summary>
        /// Whether to show or hide the printer.
        /// </summary>
        public BooleanParameter Visible;
        /// <summary>
        /// Duration (in seconds) of the modification. Default value: 0.35 seconds.
        /// </summary>
        [ParameterAlias("time")]
        public DecimalParameter Duration = .35f;

        protected override string AssignedPrinterId => Assigned(IdAndAppearance) ? IdAndAppearance.Name : null;

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            if (MakeDefault && !string.IsNullOrEmpty(AssignedPrinterId))
                PrinterManager.DefaultPrinterId = AssignedPrinterId;

            if (HideOther)
                foreach (var prntr in PrinterManager.GetAllActors())
                    if (prntr.Id != (AssignedPrinterId ?? PrinterManager.DefaultPrinterId) && prntr.Visible)
                        prntr.ChangeVisibilityAsync(false, Duration).Forget();

            var printer = default(ITextPrinterActor);

            var appearance = Assigned(IdAndAppearance) ? IdAndAppearance.NamedValue : null;
            if (!string.IsNullOrEmpty(appearance))
            {
                if (printer is null) printer = await GetOrAddPrinterAsync();
                await printer.ChangeAppearanceAsync(appearance, Duration, cancellationToken: cancellationToken);
            }

            if (Assigned(ScenePosition))
            {
                if (printer is null) printer = await GetOrAddPrinterAsync();
                var position = new Vector3(
                    ScenePosition.ElementAtOrNull(0) != null ? ScenePosition[0].Value / 100f : printer.Position.x,
                    ScenePosition.ElementAtOrNull(1) != null ? ScenePosition[1].Value / 100f : printer.Position.y,
                    ScenePosition.ElementAtOrNull(2) ?? printer.Position.z
                );
                await printer.ChangePositionAsync(position, Duration, cancellationToken: cancellationToken);
            }

            if (Assigned(Visible) || Configuration.AutoShowOnModify)
            {
                if (printer is null) printer = await GetOrAddPrinterAsync();
                await printer.ChangeVisibilityAsync(Visible, Duration, cancellationToken: cancellationToken);
            }
        }
    } 
}
