using UnityEngine;
using UnityEngine.Events;

namespace TRnK.Components
{
    [DisallowMultipleComponent]
    [AddComponentMenu("TRnK/Auto Destroy")]
    public sealed class AutoDestroy : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("Time in seconds before the object is destroyed")]
        private float _destroyAfter = 5f;

        [SerializeField, Space(6), Tooltip("Event triggered just before destruction")]
        private UnityEvent _onBeforeDestroy;

        private void Start()
        {
            Invoke(nameof(DestroyNow), _destroyAfter);
        }

        /// <summary> Binds a callback to be invoked just before the object is destroyed. </summary>
        public void Bind(UnityAction action)
        {
            _onBeforeDestroy.AddListener(action);
        }

        private void DestroyNow()
        {
            _onBeforeDestroy.Invoke();
            Destroy(gameObject);
        }
    }
}
