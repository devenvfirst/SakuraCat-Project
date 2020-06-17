// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.UI
{
    public class NavigatorSyncButton : ScriptableButton
    {
        private Image syncImage;
        private IScriptManager scriptManager;
        private bool loadingScripts;

        protected override void Awake ()
        {
            base.Awake();

            syncImage = GetComponentInChildren<Image>();
            this.AssertRequiredObjects(syncImage);

            scriptManager = Engine.GetService<IScriptManager>();
            UIComponent.interactable = false;
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            scriptManager.OnScriptLoadStarted += HandleScriptLoadStarted;
            scriptManager.OnScriptLoadCompleted += HandleScriptLoadFinished;
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            scriptManager.OnScriptLoadStarted -= HandleScriptLoadStarted;
            scriptManager.OnScriptLoadCompleted -= HandleScriptLoadFinished;
        }

        protected override void Update ()
        {
            base.Update();

            if (scriptManager is null || !scriptManager.ScriptNavigator || !scriptManager.ScriptNavigator.Visible) return;
            if (loadingScripts) syncImage.rectTransform.Rotate(new Vector3(0, 0, -99) * Time.unscaledDeltaTime);
            else syncImage.rectTransform.rotation = Quaternion.identity;
        }

        protected override void OnButtonClick ()
        {
            scriptManager.ReloadAllScriptsAsync().Forget();
        }

        private void HandleScriptLoadStarted ()
        {
            loadingScripts = true;
            UIComponent.interactable = false;
        }

        private void HandleScriptLoadFinished ()
        {
            loadingScripts = false;
            UIComponent.interactable = true;
        }
    } 
}
