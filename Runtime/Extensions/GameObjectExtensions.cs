using System;
using TRnK.ColorPalette;
using TRnK.Logger;
using UnityEngine;

namespace TRnK.Extensions
{
    public static class GameObjectExtensions
    {
        private const int UnityLayerCount = 32;

        /// <summary>Gets or adds a component of type T to the specified MonoBehaviour.</summary>
        public static T GetOrAdd<T>(this MonoBehaviour monoBehaviour) where T : Component
        {
            return monoBehaviour.gameObject.GetOrAdd<T>();
        }

        /// <summary>Gets or adds a component of type T to the specified GameObject.</summary>
        public static T GetOrAdd<T>(this GameObject gameObject) where T : Component
        {
            if (gameObject.TryGetComponent<T>(out var requestedComponent))
            {
                return requestedComponent;
            }

            return gameObject.AddComponent<T>();
        }

        /// <summary>Checks if the specified GameObject is in the given LayerMask.</summary>
        public static bool IsInLayer(this GameObject gameObject, LayerMask layer)
        {
            return (layer & (1 << gameObject.layer)) != 0;
        }

        /// <summary>Clears all child transforms of the specified GameObject.</summary>
        public static void ClearChildTransforms(this GameObject gameObject)
        {
            gameObject.transform.Clear();
        }

        /// <summary>Set GameObject to a specific layer.</summary>
        public static void SetLayer(this GameObject gameObject, int newLayer)
        {
            gameObject.layer = newLayer;
        }

        /// <summary>Set GameObject to a layer using LayerMask (uses the lowest-numbered layer found in the mask).</summary>
        public static void SetLayer(this GameObject gameObject, LayerMask layerMask)
        {
            int mask = layerMask.value;
            for (int i = 0; i < UnityLayerCount; i++)
            {
                if ((mask & (1 << i)) != 0)
                {
                    gameObject.layer = i;
                    return;
                }
            }
        }

        /// <summary>Set GameObject to a layer by name.</summary>
        public static void SetLayer(this GameObject gameObject, string layerName)
        {
            int layerNumber = LayerMask.NameToLayer(layerName);
            if (layerNumber != -1)
            {
                gameObject.layer = layerNumber;
            }
            else
            {
                Log.Warn($"Layer '{layerName.Colorize(Swatch.GA)}' not found!");
            }
        }

        /// <summary>Get all child GameObjects in a specific layer.</summary>
        public static GameObject[] GetChildrenInLayer(this MonoBehaviour monoBehaviour, LayerMask layer, bool includeInactive = false)
        {
            return monoBehaviour.gameObject.GetChildrenInLayer(layer, includeInactive);
        }

        /// <summary>Get all child GameObjects in a specific layer, including nested children.</summary>
        public static GameObject[] GetChildrenInLayerRecursive(this MonoBehaviour monoBehaviour, LayerMask layer, bool includeInactive = false)
        {
            return monoBehaviour.gameObject.GetChildrenInLayerRecursive(layer, includeInactive);
        }

        /// <summary>Get all child GameObjects in a specific layer.</summary>
        public static GameObject[] GetChildrenInLayer(this GameObject gameObject, LayerMask layer, bool includeInactive = false)
        {
            return gameObject.transform.GetChildrenInLayer(layer, includeInactive);
        }

        /// <summary>Get all child GameObjects in a specific layer, including nested children.</summary>
        public static GameObject[] GetChildrenInLayerRecursive(this GameObject gameObject, LayerMask layer, bool includeInactive = false)
        {
            return gameObject.transform.GetChildrenInLayerRecursive(layer, includeInactive);
        }

        /// <summary>Calls the specified action on the next frame.</summary>
        public static async void CallDeferred(this MonoBehaviour owner, Action action)
        {
            try
            {
                await Awaitable.NextFrameAsync(owner.destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            action?.Invoke();
        }
    }
}

