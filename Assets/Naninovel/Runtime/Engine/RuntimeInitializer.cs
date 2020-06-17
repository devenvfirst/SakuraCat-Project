// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using UniRx.Async;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Default engine initializer for runtime environment.
    /// </summary>
    public class RuntimeInitializer : MonoBehaviour
    {
        private readonly struct ServiceInitData : IEquatable<ServiceInitData>
        { 
            public readonly Type Type; 
            public readonly int Priority;
            public readonly Type[] CtorArgs;

            public ServiceInitData (Type type, InitializeAtRuntimeAttribute attr)
            {
                Type = type;
                Priority = attr.InitializationPriority;
                CtorArgs = Type.GetConstructors().First().GetParameters().Select(p => p.ParameterType).ToArray();
            }

            public override bool Equals (object obj) => obj is ServiceInitData data && Equals(data);
            public bool Equals (ServiceInitData other) => EqualityComparer<Type>.Default.Equals(Type, other.Type);
            public override int GetHashCode () => 2049151605 + EqualityComparer<Type>.Default.GetHashCode(Type);
            public static bool operator == (ServiceInitData left, ServiceInitData right) => left.Equals(right);
            public static bool operator != (ServiceInitData left, ServiceInitData right) => !(left == right);
        }

        [SerializeField] private bool initializeOnAwake = true;

        private const string defaultInitUIResourcesPath = "Naninovel/EngineInitializationUI";

        /// <summary>
        /// Invokes default engine initialization routine.
        /// </summary>
        /// <param name="configurationProvider">Configuration provider to use for engine intialization.</param>
        public static async UniTask InitializeAsync (IConfigurationProvider configurationProvider = null)
        {
            if (Engine.Initialized) return;

            if (configurationProvider is null)
                configurationProvider = new ProjectConfigurationProvider();
            var engineConfig = configurationProvider.GetConfiguration<EngineConfiguration>();

            UniTaskScheduler.UnobservedExceptionWriteLogType = engineConfig.AsyncExceptionLogType;

            var initializationUI = default(ScriptableUIBehaviour);
            if (engineConfig.ShowInitializationUI)
            {
                var initUIPrefab = ObjectUtils.IsValid(engineConfig.CustomInitializationUI) ? engineConfig.CustomInitializationUI : Resources.Load<ScriptableUIBehaviour>(defaultInitUIResourcesPath);
                initializationUI = Instantiate(initUIPrefab);
                initializationUI.Show();
            }

            var initData = new List<ServiceInitData>();
            var overridenTypes = new List<Type>();
            foreach (var type in ReflectionUtils.ExportedDomainTypes)
            {
                var initAttribute = Attribute.GetCustomAttribute(type, typeof(InitializeAtRuntimeAttribute), false) as InitializeAtRuntimeAttribute;
                if (initAttribute is null) continue;
                initData.Add(new ServiceInitData(type, initAttribute));
                if (initAttribute.Override != null)
                    overridenTypes.Add(initAttribute.Override);
            }
            initData = initData.Where(d => !overridenTypes.Contains(d.Type)).ToList(); // Exclude services overriden by user.

            bool IsService (Type t) => typeof(IEngineService).IsAssignableFrom(t);
            bool IsBehaviour (Type t) => typeof(IEngineBehaviour).IsAssignableFrom(t);
            bool IsConfig (Type t) => typeof(Configuration).IsAssignableFrom(t);

            // Order by initialization priority and then perform topological order to make sure ctor references initialized before they're used.
            IEnumerable<ServiceInitData> GetDependencies (ServiceInitData d) => d.CtorArgs.Where(IsService).SelectMany(argType => initData.Where(dd => d != dd && argType.IsAssignableFrom(dd.Type)));
            initData = initData.OrderBy(d => d.Priority).TopologicalOrder(GetDependencies).ToList();

            var behaviour = RuntimeBehaviour.Create();
            var services = new List<IEngineService>();
            var ctorParams = new List<object>();
            foreach (var data in initData)
            {
                foreach (var argType in data.CtorArgs)
                    if (IsService(argType)) ctorParams.Add(services.First(s => argType.IsAssignableFrom(s.GetType())));
                    else if (IsBehaviour(argType)) ctorParams.Add(behaviour);
                    else if (IsConfig(argType)) ctorParams.Add(configurationProvider.GetConfiguration(argType));
                    else Debug.LogError($"Only `{nameof(Configuration)}`, `{nameof(IEngineBehaviour)}` and `{nameof(IEngineService)}` with an `{nameof(InitializeAtRuntimeAttribute)}` can be requested in an engine service constructor.");
                var service = Activator.CreateInstance(data.Type, ctorParams.ToArray()) as IEngineService;
                services.Add(service);
                ctorParams.Clear();
            }

            await Engine.InitializeAsync(configurationProvider, behaviour, services);
            if (!Engine.Initialized) // In case terminated in the midst of initialization.
            {
                if (ObjectUtils.IsValid(initializationUI))
                    ObjectUtils.DestroyOrImmediate(initializationUI.gameObject);
                return;
            } 

            if (ObjectUtils.IsValid(initializationUI))
            {
                await initializationUI.ChangeVisibilityAsync(false);
                ObjectUtils.DestroyOrImmediate(initializationUI.gameObject);
            }

            var moviePlayer = Engine.GetService<IMoviePlayer>();
            if (moviePlayer.Configuration.PlayIntroMovie)
                await moviePlayer.PlayAsync(moviePlayer.Configuration.IntroMovieName);

            var scriptPlayer = Engine.GetService<IScriptPlayer>();
            var scriptManager = Engine.GetService<IScriptManager>();
            if (!string.IsNullOrEmpty(scriptManager.Configuration.InitializationScript))
            {
                await scriptPlayer.PreloadAndPlayAsync(scriptManager.Configuration.InitializationScript);
                while (scriptPlayer.Playing) await AsyncUtils.WaitEndOfFrame;
            }

            if (engineConfig.ShowTitleUI)
                Engine.GetService<IUIManager>().GetUI<UI.ITitleUI>()?.Show();

            if (scriptManager.Configuration.ShowNavigatorOnInit && scriptManager.ScriptNavigator)
                scriptManager.ScriptNavigator.Show();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnApplicationLoaded ()
        {
            var configProvider = new ProjectConfigurationProvider();
            var engineConfig = configProvider.GetConfiguration<EngineConfiguration>();
            if (engineConfig.InitializeOnApplicationLoad)
                InitializeAsync(configProvider).Forget();
        }

        private void Awake ()
        {
            if (!initializeOnAwake) return;

            var configProvider = new ProjectConfigurationProvider();
            InitializeAsync(configProvider).Forget();
        }
    }
}
