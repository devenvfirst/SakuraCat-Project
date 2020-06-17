// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.


namespace Naninovel
{
    public class PlayerPrefsSettingsSlotManager : PlayerPrefsSaveSlotManager<SettingsStateMap>
    {
        protected override string KeyPrefix => base.KeyPrefix + savesFolderPath;
        protected override bool Binary => false;

        private readonly string savesFolderPath;

        public PlayerPrefsSettingsSlotManager (StateConfiguration config, string savesFolderPath) 
        {
            this.savesFolderPath = savesFolderPath;
        }
    }
}
