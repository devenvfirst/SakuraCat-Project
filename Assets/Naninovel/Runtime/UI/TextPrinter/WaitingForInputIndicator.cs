// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.UI
{
    public class WaitingForInputIndicator : ScriptableUIComponent<RawImage>
    {
        protected bool TintPingPong => tintPingPong;
        protected Color PingColor => pingColor;
        protected Color PongColor => pongColor;
        protected float PingPongTime => pingPongTime;
        protected float RevealTime => revealTime;

        [Tooltip("Whether to tint the image in ping and pong colors when visible.")]
        [SerializeField] private bool tintPingPong = true;
        [SerializeField] private Color pingColor = ColorUtils.ClearWhite;
        [SerializeField] private Color pongColor = Color.white;
        [SerializeField] private float pingPongTime = 1.5f;
        [SerializeField] private float revealTime = 0.5f;

        private float showTime;

        public override void Show ()
        {
            showTime = Time.time;
            ChangeVisibilityAsync(true, revealTime).Forget();
        }

        public override void Hide () => Visible = false;

        protected override void Update ()
        {
            base.Update();

            if (Visible && tintPingPong)
                UIComponent.color = Color.Lerp(pingColor, pongColor, Mathf.PingPong(Time.time - showTime, pingPongTime));
        }
    } 
}
