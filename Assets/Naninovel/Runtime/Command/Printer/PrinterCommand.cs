// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UniRx.Async;

namespace Naninovel.Commands
{
    public abstract class PrinterCommand : Command
    {
        protected abstract string AssignedPrinterId { get; }
        protected virtual string AssignedAuthorId => null;

        protected ITextPrinterManager PrinterManager => Engine.GetService<ITextPrinterManager>();
        protected ICharacterManager CharacterManager => Engine.GetService<ICharacterManager>();
        protected TextPrintersConfiguration Configuration => PrinterManager.Configuration;

        private ITextPrinterActor heldPrinterActor;

        public virtual async UniTask HoldResourcesAsync ()
        {
            heldPrinterActor = await GetOrAddPrinterAsync();
            await heldPrinterActor.HoldResourcesAsync(this, null);
        }

        public virtual void ReleaseResources ()
        {
            heldPrinterActor?.ReleaseResources(this, null);
        }

        protected virtual async UniTask<ITextPrinterActor> GetOrAddPrinterAsync ()
        {
            var printerId = default(string);

            if (string.IsNullOrEmpty(AssignedPrinterId) && !string.IsNullOrEmpty(AssignedAuthorId))
                printerId = CharacterManager.Configuration.GetMetadataOrDefault(AssignedAuthorId).LinkedPrinter;
            
            if (string.IsNullOrEmpty(printerId))
                printerId = AssignedPrinterId;

            return await PrinterManager.GetOrAddActorAsync(printerId ?? PrinterManager.DefaultPrinterId);
        }
    }
}
