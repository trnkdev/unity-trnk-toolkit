using System;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TRnK.Pooling
{
#if ODIN_INSPECTOR
    public abstract class PoolableObject : SerializedMonoBehaviour
#else
    public abstract class PoolableObject : MonoBehaviour
#endif
    {
        private Action<PoolableObject> _releaseCallback;
        private bool _releasing;

        internal bool IsActive { get; private set; }

        /// <summary> Releases this instance back to the pool. If the instance is not managed by a pool, it will be destroyed. </summary>
        public void Release()
        {
            if (_releasing) return;
            _releasing = true;

            if (_releaseCallback != null)
                _releaseCallback(this);
            else
                Destroy(gameObject);
        }

        internal void Bind(Action<PoolableObject> callback) => _releaseCallback = callback;
        internal void MarkActive() { _releasing = false; IsActive = true; }
        internal void MarkInactive() { IsActive = false; }
    }
}
