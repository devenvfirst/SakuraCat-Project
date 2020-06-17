// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;

namespace Naninovel
{
    /// <summary>
    /// When applied to a static string field, the field will be assigned
    /// by <see cref="ITextManager"/> service based on the managed text documents.
    /// The property will also be included to the generated managed text documents.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class ManagedTextAttribute : Attribute
    {
        /// <summary>
        /// Category of the generated text document.
        /// </summary>
        public string Category { get; }
        /// <summary>
        /// Commentary to put before the field record in the generated managed text document.
        /// </summary>
        public string Comment { get; }

        /// <param name="category">Category of the generated text resource.</param>
        /// <param name="comment">Commentary to put before the field record in the generated managed text document.</param>
        public ManagedTextAttribute (string category = ManagedTextRecord.DefaultCategoryName, string comment = null)
        {
            Category = category;
            Comment = comment;
        }
    }
}
