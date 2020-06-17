// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Naninovel.UI
{
    public class GameStateSlot : ScriptableGridSlot
    {
        public class Constructor : Constructor<GameStateSlot>
        {
            public Constructor (GameStateSlot prototype, string id, GameStateMap state = null, 
                OnClicked onClicked = null, OnDeleteClicked onDeleteClicked = null)
                : base(prototype, id, onClicked)
            {
                ConstructedSlot.State = state;
                ConstructedSlot.onDeleteClickedAction = onDeleteClicked;
            }
        }

        /// <summary>
        /// Command to invoke when the delete button of the slot is clicked by the user.
        /// </summary>
        /// <param name="slotId">ID of the slot, which delete button was clicked.</param>
        public delegate void OnDeleteClicked (string slotId);

        public virtual GameStateMap State { get; private set; }

        [ManagedText("DefaultUI")]
        protected static string EmptySlotLabel = "Empty";

        protected ScriptableButton DeleteButton => deleteButton;
        protected Text TitleText => titleText;
        protected RawImage ThumbnailImage => thumbnailImage;
        protected Texture2D EmptySlotThambnail => emptySlotThambnail;

        [SerializeField] private ScriptableButton deleteButton = default;
        [SerializeField] private Text titleText = default;
        [SerializeField] private RawImage thumbnailImage = default;
        [SerializeField] private Texture2D emptySlotThambnail = default;

        private OnDeleteClicked onDeleteClickedAction;

        public virtual void SetEmptyState ()
        {
            deleteButton.gameObject.SetActive(false);
            titleText.text = $"{NumberInGrid}. {EmptySlotLabel}";
            thumbnailImage.texture = emptySlotThambnail;

            State = null;
        }

        public virtual void SetState (GameStateMap state)
        {
            if (state is null) { SetEmptyState(); return; }

            deleteButton.gameObject.SetActive(true);
            titleText.text = $"{NumberInGrid}. {state.SaveDateTime:yyyy-MM-dd HH:mm:ss}";
            thumbnailImage.texture = state.Thumbnail;

            State = state;
        }

        public override void OnPointerEnter (PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);

            deleteButton.Show();
        }

        public override void OnPointerExit (PointerEventData eventData)
        {
            base.OnPointerExit(eventData);

            deleteButton.Hide();
        }

        protected override void Awake ()
        {
            base.Awake();
            this.AssertRequiredObjects(deleteButton, titleText, thumbnailImage);

            if (!emptySlotThambnail)
                emptySlotThambnail = Texture2D.whiteTexture;
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            deleteButton.OnButtonClicked += HandleDeleteButtonClicked;
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            deleteButton.OnButtonClicked -= HandleDeleteButtonClicked;
        }

        protected override void Start ()
        {
            base.Start();

            SetState(State);
        }

        protected virtual void HandleDeleteButtonClicked ()
        {
            onDeleteClickedAction?.Invoke(Id);
        }
    }
}
