using System.Collections.Generic;
using UnityEngine;

namespace TRnK.Utilities
{
    public static partial class Utils
    {
        // Dictionary caching all WaitForSeconds to avoid unnecessary new memory allocations.
        private static readonly Dictionary<float, WaitForSeconds> s_waitForSecondsDictionary = new();

        /// <summary>Retrieve a cached WaitForSeconds stored in a dictionary to avoid new allocation of memory.</summary>
        public static WaitForSeconds GetWaitForSeconds(float waitSeconds)
        {
            if (s_waitForSecondsDictionary.TryGetValue(waitSeconds, out var wait))
            {
                return wait;
            }

            s_waitForSecondsDictionary.Add(waitSeconds, new WaitForSeconds(waitSeconds));

            return s_waitForSecondsDictionary[waitSeconds];
        }
    }
}
