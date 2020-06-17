// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel.FX
{
    /// <summary>
    /// Shakes a <see cref="ITextPrinterActor"/> with provided ID or an active one.
    /// </summary>
    public class ShakePrinter : ShakeTransform
    {
        protected override Transform GetShakedTransform ()
        {
            var mngr = Engine.GetService<ITextPrinterManager>();
            var id = string.IsNullOrEmpty(ObjectName) ? mngr.DefaultPrinterId : ObjectName;
            var uiRoot = GameObject.Find(id);
            if (!ObjectUtils.IsValid(uiRoot)) return null;
            // Changing transform of the UI root won't work; use the content instead.
            return uiRoot.transform.FindRecursive("Content");
        }
    }
}
