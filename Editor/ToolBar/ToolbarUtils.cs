#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TRnK.Toolkit
{
    internal static class ToolbarUtils
    {
        /// <summary>Returns the largest of the named editor icons that exists.</summary>
        public static Texture2D GetBestIcon(params string[] names)
        {
            Texture2D best = null;
            int bestArea = 0;
            foreach (var n in names)
            {
                if (string.IsNullOrEmpty(n)) continue;
                var tex = EditorGUIUtility.IconContent(n).image as Texture2D;
                if (tex == null) continue;
                int area = tex.width * tex.height;
                if (area > bestArea)
                {
                    best = tex; bestArea = area;
                }
            }
            return best;
        }
    }
}
#endif
