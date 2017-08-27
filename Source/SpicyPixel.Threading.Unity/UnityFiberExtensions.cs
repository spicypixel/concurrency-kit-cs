using System;
using UnityEngine;

namespace SpicyPixel.Threading
{
    /// <summary>
    /// Extends Fiber for Unity
    /// </summary>
    public static class UnityFiberExtensions
    {
        /// <summary>
        /// Gets the fiber as a Unity coroutine that can be yielded against.
        /// </summary>
        /// <returns>The fiber as Unity coroutine.</returns>
        /// <param name="fiber">Fiber.</param>
        public static Coroutine GetAsUnityCoroutine(this Fiber fiber)
        {
            object coroutine;
            if (fiber.Properties.TryGetValue(UnityFiberScheduler.UnityCoroutineKey, out coroutine))
                return coroutine as Coroutine;
            
            throw new InvalidOperationException("You must start the Fiber on a scheduler that allows inlining "
                + "or otherwise wait for it to start before yielding against it as a Unity Coroutine. " 
                + " Fiber coroutines can directly yield to other Fibers without this restriction.");
        }
    }
}

