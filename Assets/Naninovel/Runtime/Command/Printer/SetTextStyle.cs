// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Permamently applies [text styles](/guide/text-printers.md#text-styles) to the contents of a text printer.
    /// </summary>
    /// <remarks>
    /// You can also use rich text tags inside text messages to apply the styles selectively.
    /// </remarks>
    /// <example>
    /// ; Print first two sentences in bold red text with 45px size, 
    /// ; then reset the style and print the last sentence using default style. 
    /// @style color=#ff0000,b,size=45
    /// Lorem ipsum dolor sit amet.
    /// Cras ut nisi eget ex viverra egestas in nec magna.
    /// @style default
    /// Consectetur adipiscing elit.
    /// 
    /// ; Print starting part of the sentence normally, but the last one in bold.
    /// Lorem ipsum sit amet. &lt;b&gt;Consectetur adipiscing elit.&lt;/b&gt;
    /// </example>
    [CommandAlias("style")]
    public class SetTextStyle : PrinterCommand
    {
        /// <summary>
        /// Text formatting tags to apply. Angle brackets should be ommited, eg use `b` for &lt;b&gt; and `size=100` for &lt;size=100&gt;. Use `default` keyword to reset the style.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), RequiredParameter]
        public StringListParameter TextStyles;
        /// <summary>
        /// ID of the printer actor to use. Will use a default one when not provided.
        /// </summary>
        [ParameterAlias("printer")]
        public StringParameter PrinterId;

        protected override string AssignedPrinterId => PrinterId;

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var printer = await GetOrAddPrinterAsync();
            if (cancellationToken.IsCancellationRequested) return;

            if (TextStyles.Length == 1 && TextStyles[0].Value.EqualsFastIgnoreCase("default"))
                printer.RichTextTags = null;
            else printer.RichTextTags = TextStyles;
        }
    } 
}
