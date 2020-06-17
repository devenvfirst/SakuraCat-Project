// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using System.Linq;

namespace Naninovel.UI
{
    public class GameSettingsLanguageDropdown : ScriptableDropdown
    {
        private const string tempSaveSlotId = "TEMP_LOCALE_CHANGE";

        private readonly Dictionary<int, string> optionToLocaleMap = new Dictionary<int, string>();
        private ILocalizationManager localizationManager;

        protected override void Awake ()
        {
            base.Awake();

            localizationManager = Engine.GetService<ILocalizationManager>();
            var availableLocales = localizationManager.GetAvailableLocales().ToList();
            InitializeOptions(availableLocales);
        }

        protected override void OnValueChanged (int value)
        {
            var selectedLocale = optionToLocaleMap[value];
            HandleLocaleChangedAsync(selectedLocale);
        }

        private void InitializeOptions (List<string> availableLocales)
        {
            optionToLocaleMap.Clear();
            for (int i = 0; i < availableLocales.Count; i++)
                optionToLocaleMap.Add(i, availableLocales[i]);

            UIComponent.ClearOptions();
            UIComponent.AddOptions(availableLocales.Select(l => LanguageTags.GetLanguageByTag(l)).ToList());
            UIComponent.value = availableLocales.IndexOf(localizationManager.SelectedLocale);
            UIComponent.RefreshShownValue();
        }

        private async void HandleLocaleChangedAsync (string locale)
        {
            var clickThroughPanel = Engine.GetService<IUIManager>()?.GetUI<ClickThroughPanel>();
            if (clickThroughPanel) clickThroughPanel.Show(false, null);

            await localizationManager.SelectLocaleAsync(locale);

            var player = Engine.GetService<IScriptPlayer>();
            if (player.PlayedScript != null)
            {
                var stateManager = Engine.GetService<IStateManager>();
                var snapshot = stateManager.PeekRollbackStack();
                if (snapshot?.PlaybackSpot.InlineIndex > 0) // Compensate potential difference in inlined commands count of the localization docs.
                {
                    var success = await stateManager.RollbackAsync(s => s.PlaybackSpot.InlineIndex == 0);
                    if (!success) UnityEngine.Debug.LogWarning($"Failed to find a suitable state snapshot to rollback when changing locale.");
                }
                await stateManager.SaveGameAsync(tempSaveSlotId);
                await stateManager.ResetStateAsync();
                await Engine.GetService<IScriptManager>().ReloadAllScriptsAsync();
                await stateManager.LoadGameAsync(tempSaveSlotId);
                stateManager.GameStateSlotManager.DeleteSaveSlot(tempSaveSlotId);
            }
            else await Engine.GetService<IScriptManager>().ReloadAllScriptsAsync();

            if (clickThroughPanel) clickThroughPanel.Hide();
        }
    }
}
