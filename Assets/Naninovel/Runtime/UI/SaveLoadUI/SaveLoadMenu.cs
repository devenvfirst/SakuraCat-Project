// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class SaveLoadMenu : CustomUI, ISaveLoadUI
    {
        public SaveLoadUIPresentationMode PresentationMode { get => presentationMode; set => SetPresentationMode(value); }

        [ManagedText("DefaultUI")]
        protected static string OverwriteSaveSlotMessage = "Are you sure you want to overwrite save slot?";
        [ManagedText("DefaultUI")]
        protected static string DeleteSaveSlotMessage = "Are you sure you want to delete save slot?";

        protected Toggle QuickLoadToggle => quickLoadToggle;
        protected Toggle SaveToggle => saveToggle;
        protected Toggle LoadToggle => loadToggle;
        protected GameStateSlotsGrid QuickLoadGrid => quickLoadGrid;
        protected GameStateSlotsGrid SaveGrid => saveGrid;
        protected GameStateSlotsGrid LoadGrid => loadGrid;

        [Header("Tabs")]
        [SerializeField] private Toggle quickLoadToggle = null;
        [SerializeField] private Toggle saveToggle = null;
        [SerializeField] private Toggle loadToggle = null;

        [Header("Grids")]
        [SerializeField] private GameStateSlotsGrid quickLoadGrid = null;
        [SerializeField] private GameStateSlotsGrid saveGrid = null;
        [SerializeField] private GameStateSlotsGrid loadGrid = null;

        private IStateManager stateManager;
        private IConfirmationUI confirmationUI;
        private SaveLoadUIPresentationMode presentationMode;
        private ISaveSlotManager<GameStateMap> slotManager => stateManager?.GameStateSlotManager;

        public override async UniTask InitializeAsync ()
        {
            confirmationUI = Engine.GetService<IUIManager>().GetUI<IConfirmationUI>();

            var saveSlots = await LoadAllSaveSlotsAsync();
            if (!Engine.Initializing) return;
            foreach (var slot in saveSlots)
            {
                saveGrid.AddSlot(new GameStateSlot.Constructor(saveGrid.SlotPrototype, slot.Key, slot.Value, HandleSaveSlotClicked, HandleDeleteSlotClicked).ConstructedSlot);
                loadGrid.AddSlot(new GameStateSlot.Constructor(loadGrid.SlotPrototype, slot.Key, slot.Value, HandleLoadSlotClicked, HandleDeleteSlotClicked).ConstructedSlot);
            }

            var quickSaveSlots = await LoadAllQuickSaveSlotsAsync();
            if (!Engine.Initializing) return;
            foreach (var slot in quickSaveSlots)
                quickLoadGrid.AddSlot(new GameStateSlot.Constructor(saveGrid.SlotPrototype, slot.Key, slot.Value, HandleLoadSlotClicked, HandleDeleteQuickLoadSlotClicked).ConstructedSlot);
        }

        public virtual SaveLoadUIPresentationMode GetLastLoadMode ()
        {
            var qLoadTime = quickLoadGrid.LastSaveDateTime;
            var loadTime = loadGrid.LastSaveDateTime;

            if (!qLoadTime.HasValue) return SaveLoadUIPresentationMode.Load;
            if (!loadTime.HasValue) return SaveLoadUIPresentationMode.QuickLoad;

            return quickLoadGrid.LastSaveDateTime > loadGrid.LastSaveDateTime ? 
                SaveLoadUIPresentationMode.QuickLoad : SaveLoadUIPresentationMode.Load;
        }

        protected override void Awake ()
        {
            base.Awake();

            this.AssertRequiredObjects(quickLoadToggle, saveToggle, loadToggle, quickLoadGrid, saveGrid, loadGrid);
            stateManager = Engine.GetService<IStateManager>();
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            stateManager.OnGameSaveFinished += HandleGameSaveFinished;
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            if (stateManager != null)
                stateManager.OnGameSaveFinished -= HandleGameSaveFinished;
        }

        protected virtual void SetPresentationMode (SaveLoadUIPresentationMode value)
        {
            presentationMode = value;
            switch (value)
            {
                case SaveLoadUIPresentationMode.QuickLoad:
                    loadToggle.gameObject.SetActive(true);
                    quickLoadToggle.gameObject.SetActive(true);
                    quickLoadToggle.isOn = true;
                    saveToggle.gameObject.SetActive(false);
                    break;
                case SaveLoadUIPresentationMode.Load:
                    loadToggle.gameObject.SetActive(true);
                    quickLoadToggle.gameObject.SetActive(true);
                    loadToggle.isOn = true;
                    saveToggle.gameObject.SetActive(false);
                    break;
                case SaveLoadUIPresentationMode.Save:
                    saveToggle.gameObject.SetActive(true);
                    saveToggle.isOn = true;
                    loadToggle.gameObject.SetActive(false);
                    quickLoadToggle.gameObject.SetActive(false);
                    break;
            }
        }

        protected virtual async void HandleLoadSlotClicked (string slotId)
        {
            if (!slotManager.SaveSlotExists(slotId)) return;
            Hide();
            Engine.GetService<IUIManager>()?.GetUI<ITitleUI>()?.Hide();
            await stateManager.LoadGameAsync(slotId);
        }

        protected virtual async void HandleSaveSlotClicked (string slotId)
        {
            SetInteractable(false);

            if (slotManager.SaveSlotExists(slotId))
            {
                var confirmed = await confirmationUI.ConfirmAsync(OverwriteSaveSlotMessage);
                if (!confirmed)
                {
                    SetInteractable(true);
                    return;
                }
            }

            var state = await stateManager.SaveGameAsync(slotId);

            saveGrid.GetSlot(slotId).SetState(state);
            loadGrid.GetSlot(slotId).SetState(state);

            SetInteractable(true);
        }

        protected virtual async void HandleDeleteSlotClicked (string slotId)
        {
            if (!slotManager.SaveSlotExists(slotId)) return;

            if (!await confirmationUI.ConfirmAsync(DeleteSaveSlotMessage)) return;

            slotManager.DeleteSaveSlot(slotId);
            saveGrid.GetSlot(slotId).SetEmptyState();
            loadGrid.GetSlot(slotId).SetEmptyState();
        }

        protected virtual async void HandleDeleteQuickLoadSlotClicked (string slotId)
        {
            if (!slotManager.SaveSlotExists(slotId)) return;

            if (!await confirmationUI.ConfirmAsync(DeleteSaveSlotMessage)) return;

            slotManager.DeleteSaveSlot(slotId);
            quickLoadGrid.GetSlot(slotId).SetEmptyState();
        }

        protected virtual async void HandleGameSaveFinished (GameSaveLoadArgs args)
        {
            if (!args.Quick) return;

            // Shifting quick save slots by one to free the first slot.
            for (int i = stateManager.Configuration.QuickSaveSlotLimit - 1; i > 0; i--)
            {
                var currSlotId = stateManager.Configuration.IndexToQuickSaveSlotId(i);
                var prevSlotId = stateManager.Configuration.IndexToQuickSaveSlotId(i + 1);
                var currSlot = quickLoadGrid.GetSlot(currSlotId);
                var prevSlot = quickLoadGrid.GetSlot(prevSlotId);
                prevSlot.SetState(currSlot.State);
            }

            // Setting the new quick save to the first slot.
            var firstSlotId = stateManager.Configuration.IndexToQuickSaveSlotId(1);
            var slotState = await stateManager.GameStateSlotManager.LoadAsync(args.SlotId);
            quickLoadGrid.GetSlot(firstSlotId).SetState(slotState);
        }

        /// <summary>
        /// Slots are provided in [slotId]->[state] map format; null state represents an `empty` slot.
        /// </summary>
        protected virtual async UniTask<IDictionary<string, GameStateMap>> LoadAllSaveSlotsAsync ()
        {
            var result = new Dictionary<string, GameStateMap>();
            for (int i = 1; i <= stateManager.Configuration.SaveSlotLimit; i++)
            {
                var slotId = stateManager.Configuration.IndexToSaveSlotId(i);
                var state = slotManager.SaveSlotExists(slotId) ? await slotManager.LoadAsync(slotId) as GameStateMap : null;
                result.Add(slotId, state);
            }
            return result;
        }

        /// <summary>
        /// Slots are provided in [slotId]->[state] map format; null state represents an `empty` slot.
        /// </summary>
        protected virtual async UniTask<IDictionary<string, GameStateMap>> LoadAllQuickSaveSlotsAsync ()
        {
            var result = new Dictionary<string, GameStateMap>();
            for (int i = 1; i <= stateManager.Configuration.QuickSaveSlotLimit; i++)
            {
                var slotId = stateManager.Configuration.IndexToQuickSaveSlotId(i);
                var state = slotManager.SaveSlotExists(slotId) ? await slotManager.LoadAsync(slotId) as GameStateMap : null;
                result.Add(slotId, state);
            }
            return result;
        }
    }
}
