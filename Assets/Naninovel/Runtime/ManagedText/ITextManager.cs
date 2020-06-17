// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using UniRx.Async;

namespace Naninovel
{
    /// <summary>
    /// Implementation is able to provide managed text documents and automatically replace static string values, marked with <see cref="ManagedTextAttribute"/>.
    /// </summary>
    public interface ITextManager : IEngineService<ManagedTextConfiguration>
    {
        /// <summary>
        /// Attempts to retrieve a managed text record value with the provided key and category (document name).
        /// Will return null when no records found.
        /// </summary>
        string GetRecordValue (string key, string category = ManagedTextRecord.DefaultCategoryName);
        /// <summary>
        /// Returns all the currently available managed text records.
        /// </summary>
        IEnumerable<ManagedTextRecord> GetAllRecords (params string[] categoryFilter);
        /// <summary>
        /// Applies the managed text records replacing static string values marked with <see cref="ManagedTextAttribute"/>.
        /// </summary>
        UniTask ApplyManagedTextAsync ();
    }
}
