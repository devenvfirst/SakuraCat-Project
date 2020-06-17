// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using System.Linq;
using UniRx.Async;
using UnityEngine;

namespace Naninovel
{
    /// <inheritdoc cref="ITextManager"/>
    [InitializeAtRuntime]
    public class TextManager : ITextManager
    {
        public ManagedTextConfiguration Configuration { get; }

        private readonly IResourceProviderManager providersManager;
        private readonly ILocalizationManager localizationManager;
        private readonly HashSet<ManagedTextRecord> records = new HashSet<ManagedTextRecord>();
        private ResourceLoader<TextAsset> documentLoader;

        public TextManager (ManagedTextConfiguration config, IResourceProviderManager providersManager, ILocalizationManager localizationManager)
        {
            Configuration = config;
            this.providersManager = providersManager;
            this.localizationManager = localizationManager;
        }

        public UniTask InitializeServiceAsync ()
        {
            localizationManager.AddChangeLocaleTask(ApplyManagedTextAsync);
            documentLoader = Configuration.Loader.CreateLocalizableFor<TextAsset>(providersManager, localizationManager);
            return UniTask.CompletedTask;
        }

        public void ResetService () { }

        public void DestroyService ()
        {
            localizationManager?.RemoveChangeLocaleTask(ApplyManagedTextAsync);
            documentLoader?.UnloadAll();
        }

        public string GetRecordValue (string key, string category = ManagedTextRecord.DefaultCategoryName)
        {
            foreach (var record in records)
                if (record.Category.EqualsFast(category) && record.Key.EqualsFast(key))
                    return record.Value;
            return null;
        }

        public IEnumerable<ManagedTextRecord> GetAllRecords (params string[] categoryFilter)
        {
            if (categoryFilter is null || categoryFilter.Length == 0)
                return records.ToList();

            var result = new List<ManagedTextRecord>();
            foreach (var record in records)
                if (categoryFilter.Contains(record.Category))
                    result.Add(record);
            return result;
        }

        public async UniTask ApplyManagedTextAsync ()
        {
            records.Clear();
            documentLoader.UnloadAll();
            var documentResources = await documentLoader.LoadAllAsync();
            foreach (var documentResource in documentResources)
            {
                if (!documentResource.IsValid)
                {
                    Debug.LogWarning($"Failed to load `{documentResource.Path}` managed text document.");
                    continue;
                }
                var managedTextSet = ManagedTextUtils.ParseDocument(documentResource.Object.text, documentLoader.BuildLocalPath(documentResource.Path));

                foreach (var text in managedTextSet)
                    records.Add(new ManagedTextRecord(text.Key, text.Value, text.Category));

                ManagedTextUtils.ApplyRecords(managedTextSet);
            }
        }
    }
}
