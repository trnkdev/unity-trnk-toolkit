using System;
using System.Collections.Generic;
using TRnK.Logger;
using TRnK.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TRnK.Extensions
{
    public static class TransformExtensions
    {
        /// <summary>Destroys all child objects of the specified transform.</summary>
        public static void Clear(this Transform transform)
        {
            if (transform.childCount == 0) return;

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
#if UNITY_EDITOR
                if (Application.isPlaying)
                    Object.Destroy(child.gameObject);
                else
                    Object.DestroyImmediate(child.gameObject);
#else
                Object.Destroy(child.gameObject);
#endif
            }
        }

        /// <summary>Gets all direct children of this transform. Inactive means the child itself is disabled (activeSelf).</summary>
        public static Transform[] GetChildren(this Transform transform, bool includeInactive = false)
        {
            var children = new List<Transform>(transform.childCount);
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (includeInactive || child.gameObject.activeSelf)
                    children.Add(child);
            }
            return children.ToArray();
        }

        /// <summary>Makes transform look at target in 2D (Z-axis rotation only).</summary>
        public static void LookAt2D(this Transform transform, Vector2 target)
        {
            Vector2 direction = ((Vector2)transform.position).DirectionTo(target);
            float angle = Utils.GetAngleFromVector(direction);
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        /// <summary>Makes transform look at target in 2D with offset angle.</summary>
        public static void LookAt2D(this Transform transform, Vector2 target, float angleOffset)
        {
            var direction = ((Vector2)transform.position).DirectionTo(target);
            float angle = Utils.GetAngleFromVector(direction) + angleOffset;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        /// <summary>Makes transform look at another transform in 2D.</summary>
        public static void LookAt2D(this Transform transform, Transform target)
        {
            if (target == null)
            {
                Log.Warn($"Target transform is null for {transform.name}");
                return;
            }
            transform.LookAt2D(target.position);
        }

        /// <summary>Makes transform look at another transform in 2D with offset angle.</summary>
        public static void LookAt2D(this Transform transform, Transform target, float angleOffset)
        {
            if (target == null)
            {
                Log.Warn($"Target transform is null for {transform.name}");
                return;
            }
            transform.LookAt2D(target.position, angleOffset);
        }

        /// <summary>Gets distance to another transform.</summary>
        public static float DistanceTo(this Transform transform, Transform other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other), $"DistanceTo: Target transform is null for {transform.name}");
            }
            return transform.position.DistanceTo(other.position);
        }

        /// <summary>Gets direction to another transform.</summary>
        public static Vector3 DirectionTo(this Transform transform, Transform other)
        {
            if (other == null)
            {
                Log.Warn($"Target transform is null for {transform.name}");
                return Vector3.zero;
            }
            return transform.position.DirectionTo(other.position);
        }

        /// <summary>Checks if transform is within range of another transform.</summary>
        public static bool InRangeOf(this Transform transform, Transform other, float range)
        {
            if (other == null)
            {
                Log.Warn($"Target transform is null for {transform.name}");
                return false;
            }
            return transform.position.InRangeOf(other.position, range);
        }

        /// <summary>Resets transform to default values (position: zero, rotation: identity, scale: one).</summary>
        public static void ResetTransform(this Transform transform)
        {
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            transform.localScale = Vector3.one;
        }

        /// <summary>Resets local transform to default values.</summary>
        public static void ResetLocalTransform(this Transform transform)
        {
            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            transform.localScale = Vector3.one;
        }

        /// <summary>Gets all child GameObjects that are in the specified layer mask.</summary>
        public static GameObject[] GetChildrenInLayer(this Transform transform, LayerMask layerMask, bool includeInactive = false)
        {
            var results = new List<GameObject>();
            var childCount = transform.childCount;

            for (int i = 0; i < childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if ((includeInactive || child.gameObject.activeSelf) && child.gameObject.IsInLayer(layerMask))
                {
                    results.Add(child.gameObject);
                }
            }

            return results.ToArray();
        }

        /// <summary>Gets all child GameObjects in the specified layer mask, including nested children. Without includeInactive, disabled children and everything under them are skipped.</summary>
        public static GameObject[] GetChildrenInLayerRecursive(this Transform transform, LayerMask layerMask, bool includeInactive = false)
        {
            var results = new List<GameObject>();
            GetChildrenInLayerRecursiveInternal(transform, layerMask, includeInactive, results);
            return results.ToArray();
        }

        /// <summary>Gets all child GameObjects in the specified layer mask, including nested children.</summary>
        private static void GetChildrenInLayerRecursiveInternal(Transform transform, LayerMask layerMask, bool includeInactive, List<GameObject> results)
        {
            int childCount = transform.childCount;

            for (int i = 0; i < childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (!includeInactive && !child.gameObject.activeSelf)
                    continue;
                if (child.gameObject.IsInLayer(layerMask))
                    results.Add(child.gameObject);
                GetChildrenInLayerRecursiveInternal(child, layerMask, includeInactive, results);
            }
        }
    }
}

