using UnityEngine;
using Utilla.Tools;

namespace Utilla.Behaviours
{
    internal class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance { get; protected set; }

        public static bool HasInstance => Instance;

        private T GenericComponent => gameObject.GetComponent<T>();

        public void Awake()
        {
            if (HasInstance && Instance != GenericComponent)
            {
                Logging.Warning($"Singleton for {typeof(T).Name} already has an instance when another component exists");
                Destroy(GenericComponent);
                return;
            }

            Instance = GenericComponent;

            Initialize();
        }

        public virtual void Initialize()
        {
            Logging.Info($"Initializing singleton for {typeof(T).Name}");
        }
    }
}