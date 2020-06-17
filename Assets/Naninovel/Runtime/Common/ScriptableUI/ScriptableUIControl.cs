// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine.EventSystems;

namespace Naninovel
{
    public abstract class ScriptableUIControl<T> : ScriptableUIComponent<T> where T : UIBehaviour
    {
        protected override void OnEnable ()
        {
            base.OnEnable();
            BindUIEvents();
        }

        protected override void OnDisable ()
        {
            base.OnDisable();
            UnbindUIEvents();
        }

        protected abstract void BindUIEvents ();
        protected abstract void UnbindUIEvents ();
    }
}
