// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Linq;

namespace Naninovel.UI
{
    public class GameStateSlotsGrid : ScriptableGrid<GameStateSlot>
    {
        public virtual DateTime? LastSaveDateTime => SlotsMap?.Values?.Max(s => ObjectUtils.IsValid(s) ? s.State?.SaveDateTime : default);
    }
}
