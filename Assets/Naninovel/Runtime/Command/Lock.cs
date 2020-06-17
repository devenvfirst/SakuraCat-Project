// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Sets an [unlockable item](/guide/unlockable-items.md) with the provided ID to `locked` state.
    /// </summary>
    /// <remarks>
    /// The unlocked state of the items is stored in [global scope](/guide/state-management.md#global-state).<br/>
    /// In case item with the provided ID is not registered in the global state map, 
    /// the corresponding record will automatically be added.
    /// </remarks>
    /// <example>
    /// @lock CG/FightScene1
    /// </example>
    public class Lock : Command, Command.IForceWait
    {
        /// <summary>
        /// ID of the unlockable item. Use `*` to lock all the registered unlockable items. 
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), RequiredParameter]
        public StringParameter Id;

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var unlockableManager = Engine.GetService<IUnlockableManager>();

            if (Id.Value.EqualsFastIgnoreCase("*")) unlockableManager.LockAllItems();
            else unlockableManager.LockItem(Id);

            await Engine.GetService<IStateManager>().SaveGlobalStateAsync();
        }
    } 
}
