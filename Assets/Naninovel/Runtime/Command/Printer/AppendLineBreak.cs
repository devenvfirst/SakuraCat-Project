// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Adds a line break to a text printer.
    /// </summary>
    /// <example>
    /// ; Second sentence will be printed on a new line
    /// Lorem ipsum dolor sit amet.[br]Consectetur adipiscing elit.
    /// 
    /// ; Second sentence will be printer two lines under the first one
    /// Lorem ipsum dolor sit amet.[br 2]Consectetur adipiscing elit.
    /// </example>
    [CommandAlias("br")]
    public class AppendLineBreak : PrinterCommand
    {
        /// <summary>
        /// Number of line breaks to add.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias)]
        public IntegerParameter Count = 1;
        /// <summary>
        /// ID of the printer actor to use. Will use a default one when not provided.
        /// </summary>
        [ParameterAlias("printer")]
        public StringParameter PrinterId;
        /// <summary>
        /// ID of the actor, which should be associated with the appended line break.
        /// </summary>
        [ParameterAlias("author")]
        public StringParameter AuthorId;

        protected override string AssignedPrinterId => PrinterId;
        protected override string AssignedAuthorId => AuthorId;
        protected IUIManager UIManager => Engine.GetService<IUIManager>();

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var printer = await GetOrAddPrinterAsync();
            if (cancellationToken.IsCancellationRequested) return;
            var backlogUI = UIManager.GetUI<UI.IBacklogUI>();

            for (int i = 0; i < Count; i++)
            {
                printer.Text += "\n";
                backlogUI?.AppendMessage("\n");
            }
        }
    }
}
