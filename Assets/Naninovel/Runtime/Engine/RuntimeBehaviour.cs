// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="IEngineBehaviour"/> implementation using <see cref="MonoBehaviour"/> for runtime environment.
    /// Behaviour lifetime is independent of the Unity scenes, but will be destroyed when exiting editor play mode.
    /// </summary>
    public class RuntimeBehaviour : MonoBehaviour, IEngineBehaviour
    {
        public event Action OnBehaviourUpdate;
        public event Action OnBehaviourLateUpdate;
        public event Action OnBehaviourDestroy;

        private GameObject rootObject;
        private MonoBehaviour monoBehaivour;

        public static RuntimeBehaviour Create ()
        {
            var go = new GameObject("Naninovel<Runtime>");
            DontDestroyOnLoad(go);
            var behaivourComp = go.AddComponent<RuntimeBehaviour>();
            behaivourComp.rootObject = go;
            behaivourComp.monoBehaivour = behaivourComp;
            return behaivourComp;
        }

        public GameObject GetRootObject () => rootObject;

        public void AddChildObject (GameObject obj)
        {
            if (ObjectUtils.IsValid(obj))
                obj.transform.SetParent(transform);
        }

        public void Destroy ()
        {
            if (monoBehaivour && monoBehaivour.gameObject)
                Destroy(monoBehaivour.gameObject);
        }

        private void Update ()
        {
            OnBehaviourUpdate?.Invoke();
        }

        private void LateUpdate ()
        {
            OnBehaviourLateUpdate?.Invoke();
        }

        private void OnDestroy ()
        {
            OnBehaviourDestroy?.Invoke();
        }
    }
}
