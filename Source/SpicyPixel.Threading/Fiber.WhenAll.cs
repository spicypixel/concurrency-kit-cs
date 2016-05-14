using System;
using System.Collections;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace SpicyPixel.Threading
{
    public partial class Fiber
    {
        /// <summary>
        /// Returns a fiber that waits on all fibers to complete.
        /// </summary>
        /// <remarks>
        /// `Fiber.ResultAsObject` will be `true` if all fibers complete
        /// successfully or `false` if cancelled or timeout.
        /// </remarks>
        /// <returns>A fiber that waits on all fibers to complete.</returns>
        /// <param name="fibers">Fibers to wait for completion.</param>
        public static Fiber WhenAll (params Fiber[] fibers)
        {
            return WhenAll (fibers, Timeout.Infinite, CancellationToken.None);
        }

        /// <summary>
        /// Returns a fiber that waits on all fibers to complete.
        /// </summary>
        /// <remarks>
        /// `Fiber.ResultAsObject` will be `true` if all fibers complete
        /// successfully or `false` if cancelled or timeout.
        /// </remarks>
        /// <returns>A fiber that waits on all fibers to complete.</returns>
        /// <param name="fibers">Fibers to wait for completion.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static Fiber WhenAll (Fiber[] fibers, CancellationToken cancellationToken)
        {
            return WhenAll (fibers, Timeout.Infinite, cancellationToken);
        }

        /// <summary>
        /// Returns a fiber that waits on all fibers to complete.
        /// </summary>
        /// <remarks>
        /// `Fiber.ResultAsObject` will be `true` if all fibers complete
        /// successfully or `false` if cancelled or timeout.
        /// </remarks>
        /// <returns>A fiber that waits on all fibers to complete.</returns>
        /// <param name="fibers">Fibers to wait for completion.</param>
        /// <param name="timeout">Timeout.</param>
        public static Fiber WhenAll (Fiber[] fibers, TimeSpan timeout)
        {
            return WhenAll (fibers, CheckTimeout (timeout), CancellationToken.None);
        }

        /// <summary>
        /// Returns a fiber that waits on all fibers to complete.
        /// </summary>
        /// <remarks>
        /// `Fiber.ResultAsObject` will be `true` if all fibers complete
        /// successfully or `false` if cancelled or timeout.
        /// </remarks>
        /// <returns>A fiber that waits on all fibers to complete.</returns>
        /// <param name="fibers">Fibers to wait for completion.</param>
        /// <param name="millisecondsTimeout">Milliseconds timeout.</param>
        public static Fiber WhenAll (Fiber[] fibers, int millisecondsTimeout)
        {
            return WhenAll (fibers, millisecondsTimeout, CancellationToken.None);
        }

        /// <summary>
        /// Returns a fiber that waits on all fibers to complete.
        /// </summary>
        /// <remarks>
        /// `Fiber.ResultAsObject` will be `true` if all fibers complete
        /// successfully or `false` if cancelled or timeout.
        /// </remarks>
        /// <returns>A fiber that waits on all fibers to complete.</returns>
        /// <param name="fibers">Fibers to wait for completion.</param>
        /// <param name="millisecondsTimeout">Milliseconds timeout.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static Fiber WhenAll (Fiber[] fibers, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            return WhenAll(fibers, millisecondsTimeout, cancellationToken, FiberScheduler.Current);
        }

        /// <summary>
        /// Returns a fiber that waits on all fibers to complete.
        /// </summary>
        /// <remarks>
        /// `Fiber.ResultAsObject` will be `true` if all fibers complete
        /// successfully or `false` if cancelled or timeout.
        /// </remarks>
        /// <returns>A fiber that waits on all fibers to complete.</returns>
        /// <param name="fibers">Fibers to wait for completion.</param>
        /// <param name="millisecondsTimeout">Milliseconds timeout.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="scheduler">Scheduler.</param>
        public static Fiber WhenAll (Fiber[] fibers, int millisecondsTimeout, CancellationToken cancellationToken, FiberScheduler scheduler)
        {
            if (fibers == null)
                throw new ArgumentNullException ("fibers");

            foreach (var fiber in fibers) {
                if (fiber == null)
                    throw new ArgumentException ("fibers", "the fibers argument contains a null element");              
            }

            return Fiber.Factory.StartNew(WhenAllCoroutine(fibers, millisecondsTimeout, cancellationToken), scheduler);
        }

        /// <summary>
        /// Returns a fiber that waits on all fibers to complete.
        /// </summary>
        /// <remarks>
        /// `Fiber.ResultAsObject` will be `true` if all fibers complete
        /// successfully or `false` if cancelled or timeout.
        /// </remarks>
        /// <returns>A fiber that waits on all fibers to complete.</returns>
        /// <param name="fibers">Fibers to wait for completion.</param>
        /// <param name="millisecondsTimeout">Milliseconds timeout.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="scheduler">Scheduler.</param>
        public static Fiber WhenAll (IEnumerable<Fiber> fibers, int millisecondsTimeout, CancellationToken cancellationToken, FiberScheduler scheduler)
        {
            return WhenAll(fibers.ToArray(), millisecondsTimeout, cancellationToken, scheduler);
        }

        static IEnumerator WhenAllCoroutine(IEnumerable<Fiber> fibers, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            var startWait = DateTime.Now;
            while (true) {
                if ((millisecondsTimeout != Timeout.Infinite 
                    && (DateTime.Now - startWait).TotalMilliseconds >= millisecondsTimeout) ||
                    cancellationToken.IsCancellationRequested) {
                    yield return new FiberResult(false);
                }

                if (fibers.All(f => f.IsCompleted)) {
                    yield return new FiberResult(true);
                }

                yield return FiberInstruction.YieldToAnyFiber;
            }
        }
    }
}

