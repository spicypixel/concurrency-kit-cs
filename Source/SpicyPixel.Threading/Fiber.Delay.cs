using System;
using System.Threading;
using System.Collections;

namespace SpicyPixel.Threading
{
    public partial class Fiber
    {
        /// <summary>
        /// Crates a Fiber that waits for a delay before completing.
        /// </summary>
        /// <param name="millisecondsDelay">Milliseconds to delay.</param>
        public static Fiber Delay (int millisecondsDelay)
        {
            return Delay (millisecondsDelay, CancellationToken.None);
        }

        /// <summary>
        /// Crates a Fiber that waits for a delay before completing.
        /// </summary>
        /// <param name="delay">Time span to delay.</param>
        public static Fiber Delay (TimeSpan delay)
        {
            return Delay (CheckTimeout (delay), CancellationToken.None);
        }

        /// <summary>
        /// Crates a Fiber that waits for a delay before completing.
        /// </summary>
        /// <param name="delay">Time span to delay.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static Fiber Delay (TimeSpan delay, CancellationToken cancellationToken)
        {
            return Delay (CheckTimeout (delay), cancellationToken);
        }

        /// <summary>
        /// Crates a Fiber that waits for a delay before completing.
        /// </summary>
        /// <param name="millisecondsDelay">Milliseconds to delay.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static Fiber Delay (int millisecondsDelay, CancellationToken cancellationToken)
        {
            return Delay(millisecondsDelay, cancellationToken, FiberScheduler.Current);
        }

        /// <summary>
        /// Crates a Fiber that waits for a delay before completing.
        /// </summary>
        /// <param name="millisecondsDelay">Milliseconds to delay.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="scheduler">Scheduler.</param>
        public static Fiber Delay (int millisecondsDelay, CancellationToken cancellationToken, FiberScheduler scheduler)
        {
            if (millisecondsDelay < -1)
                throw new ArgumentOutOfRangeException ("millisecondsDelay");

            return Fiber.Factory.StartNew(DelayCoroutine(millisecondsDelay, cancellationToken), scheduler);
        }

        static IEnumerator DelayCoroutine(int millisecondsDelay, CancellationToken cancellationToken)
        {
            var startWait = DateTime.Now;
            while (true) {
                // Stop if cancellation requested
                if (cancellationToken.IsCancellationRequested) {
                    yield break;
                }

                // Stop if delay is passed
                if (millisecondsDelay != Timeout.Infinite && (DateTime.Now - startWait).TotalMilliseconds >= millisecondsDelay) {
                    yield break;
                }

                // FIXME: This would be preferable to above because it would let some
                // schedulers sleep. It requires support for cancellation to be added, however.
                //yield return new YieldForSeconds((float)millisecondsDelay / 1000f);

                yield return FiberInstruction.YieldToAnyFiber;
            }
        }
    }
}

