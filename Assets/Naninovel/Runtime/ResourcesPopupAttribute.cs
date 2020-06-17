// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Draws a dropdown selection list of the resource paths, which are added via editor managers (aka `EditorResources`).
    /// </summary>
    public class ResourcesPopupAttribute : PropertyAttribute
    {
        public readonly string Category;
        public readonly string PathPrefix;
        public readonly string EmptyOption;

        /// <param name="category">When specified, will only fetch resources under the category.</param>
        /// <param name="pathPrefix">When specified, will only fetch resources under the path prefix and trim the prefix from the values.</param>
        /// <param name="emptyOption">When specified, will include an additional option with the provided name and <see cref="string.Empty"/> value to the list.</param>
        public ResourcesPopupAttribute (string category = null, string pathPrefix = null, string emptyOption = null)
        {
            Category = category;
            PathPrefix = pathPrefix;
            EmptyOption = emptyOption;
        }
    }
}
