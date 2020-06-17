// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel
{
    public class TipsListItem : MonoBehaviour
    {
        public virtual string UnlockableId { get; private set; }
        public virtual int Number => transform.GetSiblingIndex() + 1;

        protected Button Button => button;
        protected Text Label => label;
        protected GameObject SelectedIndicator => selectedIndicator;

        [SerializeField] private Button button = default;
        [SerializeField] private Text label = default;
        [SerializeField] private GameObject selectedIndicator = default;

        private Action<TipsListItem> onClick;
        private string title;
        private bool selectedOnce;

        public static TipsListItem Instantiate (TipsListItem prototype, string unlockableId, string title, bool selectedOnce, Action<TipsListItem> onClick)
        {
            var item = Instantiate(prototype);

            item.onClick = onClick;
            item.UnlockableId = unlockableId;
            item.title = title;
            item.selectedOnce = selectedOnce;

            return item;
        }

        public virtual void SetSelected (bool selected)
        {
            if (selected)
            {
                selectedOnce = true;
                label.fontStyle = FontStyle.Normal;
            }
            selectedIndicator.SetActive(selected);
        }

        public virtual void SetUnlocked (bool unlocked)
        {
            label.text = $"{Number}. {(unlocked ? title : "???")}";
            label.fontStyle = !unlocked || selectedOnce ? FontStyle.Normal : FontStyle.Bold;
            button.interactable = unlocked;
        }

        protected virtual void Awake ()
        {
            this.AssertRequiredObjects(button, label, selectedIndicator);
            selectedIndicator.SetActive(false);
        }

        protected virtual void OnEnable ()
        {
            button.onClick.AddListener(HandleButtonClicked);
        }

        protected virtual void OnDisable ()
        {
            button.onClick.RemoveListener(HandleButtonClicked);
        }

        protected virtual void HandleButtonClicked ()
        {
            onClick?.Invoke(this);
        }
    }
}
