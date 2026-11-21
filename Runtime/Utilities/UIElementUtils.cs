using System.Collections.Generic;
using TRnK.Extensions;
using TRnK.Logger;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TRnK.Utilities
{
    public static partial class Utils
    {
        /// <summary>Returns 'true' if the pointer is over any Unity UI element.</summary>
        public static bool IsPointerOverUI()
        {
            if (EventSystem.current == null) return false;
            return EventSystem.current.IsPointerOverGameObject();
        }

        /// <summary>Returns 'true' if the pointer is over a Unity UI element on the given layer.</summary>
        public static bool IsPointerOverUI(LayerMask layer)
        {
            var eventSystemRaycastResults = GetEventSystemRaycastResults();
            if (eventSystemRaycastResults.IsNullOrEmpty())
                return false;

            for (int i = 0; i < eventSystemRaycastResults.Count; i++)
            {
                RaycastResult curRaycastResult = eventSystemRaycastResults[i];
                if (curRaycastResult.gameObject.IsInLayer(layer))
                    return true;
            }

            return false;
        }

        /// <summary>Gets all event system raycast results of current mouse or touch position.</summary>
        public static List<RaycastResult> GetEventSystemRaycastResults()
        {
            if (EventSystem.current == null)
            {
                Log.Warn("EventSystem is null. Returning empty raycast results.");
                return new List<RaycastResult>();
            }

            PointerEventData eventData = new(EventSystem.current) { position = GetPointerScreenPosition() };
            List<RaycastResult> raycastResults = new();
            EventSystem.current.RaycastAll(eventData, raycastResults);
            return raycastResults;
        }
    }
}
