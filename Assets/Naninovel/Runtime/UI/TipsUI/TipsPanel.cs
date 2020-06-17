// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class TipsPanel : CustomUI, ITipsUI
    {
        [System.Serializable]
        public class TipsSelectedState : SerializableMap<string, bool> { }

        public const string DefaultManagedTextCategory = "Tips";

        public virtual int TipsCount { get; private set; }

        protected string UnlockableIdPrefix => unlockableIdPrefix;
        protected string ManagedTextCategory => managedTextCategory;
        protected RectTransform ItemsContainer => itemsContainer;
        protected TipsListItem ItemPrefab => itemPrefab;
        protected Text TitleText => titleText;
        protected Text NumberText => numberText;
        protected Text CategoryText => categoryText;
        protected Text DescriptionText => descriptionText;

        private const string separatorLiteral = "|";

        [Header("Tips Setup")]
        [Tooltip("All the unlockable item IDs with the specified prefix will be considered Tips items.")]
        [SerializeField] private string unlockableIdPrefix = "Tips";
        [Tooltip("The name of the managed text document (category) where all the tips data is stored.")]
        [SerializeField] private string managedTextCategory = DefaultManagedTextCategory;

        [Header("UI Setup")]
        [SerializeField] private RectTransform itemsContainer = default;
        [SerializeField] private TipsListItem itemPrefab = default;
        [SerializeField] private Text titleText = default;
        [SerializeField] private Text numberText = default;
        [SerializeField] private Text categoryText = default;
        [SerializeField] private Text descriptionText = default;

        private IUnlockableManager unlockableManager;
        private ITextManager textManager;
        private IStateManager stateManager;
        private TipsSelectedState tipsSelectedState = new TipsSelectedState();
        private List<TipsListItem> listItems = new List<TipsListItem>();

        public override UniTask InitializeAsync ()
        {
            tipsSelectedState = stateManager.GlobalState.GetState<TipsSelectedState>() ?? new TipsSelectedState();

            var records = textManager.GetAllRecords(managedTextCategory);
            foreach (var record in records)
            {
                var unlockableId = $"{unlockableIdPrefix}/{record.Key}";
                var title = record.Value.GetBefore(separatorLiteral) ?? record.Value;
                var selectedOnce = tipsSelectedState.TryGetValue(unlockableId, out var selected) && selected;
                var item = TipsListItem.Instantiate(itemPrefab, unlockableId, title, selectedOnce, HandleItemClicked);
                item.transform.SetParent(itemsContainer, false);
                listItems.Add(item);
            }

            foreach (var item in listItems)
                item.SetUnlocked(unlockableManager.ItemUnlocked(item.UnlockableId));

            TipsCount = listItems.Count;

            return UniTask.CompletedTask;
        }

        protected override void Awake ()
        {
            base.Awake();
            this.AssertRequiredObjects(itemsContainer, itemPrefab, titleText, numberText, categoryText, descriptionText);

            unlockableManager = Engine.GetService<IUnlockableManager>();
            textManager = Engine.GetService<ITextManager>();
            stateManager = Engine.GetService<IStateManager>();

            titleText.text = string.Empty;
            numberText.text = string.Empty;
            categoryText.text = string.Empty;
            descriptionText.text = string.Empty;
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            unlockableManager.OnItemUpdated += HandleUnlockableItemUpdated;
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            if (unlockableManager != null)
                unlockableManager.OnItemUpdated -= HandleUnlockableItemUpdated;
        }

        protected virtual void HandleItemClicked (TipsListItem clickedItem)
        {
            if (!unlockableManager.ItemUnlocked(clickedItem.UnlockableId)) return;

            tipsSelectedState[clickedItem.UnlockableId] = true;
            foreach (var item in listItems)
                item.SetSelected(item.UnlockableId.EqualsFast(clickedItem.UnlockableId));
            var recordValue = textManager.GetRecordValue(clickedItem.UnlockableId.GetAfterFirst($"{unlockableIdPrefix}/"), managedTextCategory);
            titleText.text = recordValue.GetBefore(separatorLiteral)?.Trim() ?? recordValue;
            numberText.text = clickedItem.Number.ToString();
            categoryText.text = recordValue.GetBetween(separatorLiteral)?.Trim() ?? string.Empty;
            descriptionText.text = recordValue.GetAfter(separatorLiteral)?.Replace("\\n", "\n")?.Trim() ?? string.Empty;
        }

        protected override void HandleVisibilityChanged (bool visible)
        {
            base.HandleVisibilityChanged(visible);

            if (visible) return;

            stateManager?.GlobalState.SetState(tipsSelectedState);
            stateManager?.SaveGlobalStateAsync().Forget();
        }

        protected virtual void HandleUnlockableItemUpdated (UnlockableItemUpdatedArgs args)
        {
            if (!args.Id.StartsWithFast(unlockableIdPrefix)) return;

            var unlockedItem = listItems.Find(i => i.UnlockableId.EqualsFast(args.Id));
            if (unlockedItem)
                unlockedItem.SetUnlocked(args.Unlocked);
        }
    }
}
