// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Represents a directory in the project assets.
    /// </summary>
    [System.Serializable]
    public class Folder
    {
        public string Path => path;
        public string Name => Path.Contains("/") ? Path.GetAfter("/") : Path;

        [SerializeField] string path = null;

        public Folder (string path)
        {
            this.path = path;
        }
    }
}
