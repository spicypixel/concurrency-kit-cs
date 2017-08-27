using System;
using System.Collections;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        public static Fiber WhenAll (params Fiber [] fibers)
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
        public static Fiber WhenAll (Fiber [] fibers, CancellationToken cancellationToken)
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
        public static Fiber WhenAll (Fiber [] fibers, TimeSpan timeout)
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
        public static Fiber WhenAll (Fiber [] fibers, int millisecondsTimeout)
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
        public static Fiber WhenAll (Fiber [] fibers, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            return WhenAll (fibers, millisecondsTimeout, cancellationToken, FiberScheduler.Current);
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
        public static Fiber WhenAll (Fiber [] fibers, int millisecondsTimeout, CancellationToken cancellationToken, FiberScheduler scheduler)
        {
            if (fibers == null)
                throw new ArgumentNullException ("fibers");

            foreach (var fiber in fibers) {
                if (fiber == null)
                    throw new ArgumentException ("fibers", "the fibers argument contains a null element");
            }

            return Fiber.Factory.StartNew (WhenAllFibersCoroutine (fibers, millisecondsTimeout, cancellationToken), cancellationToken, scheduler);
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
            return WhenAll (fibers.ToArray (), millisecondsTimeout, cancellationToken, scheduler);
        }

        static IEnumerator WhenAllFibersCoroutine (IEnumerable<Fiber> fibers, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            var startWait = DateTime.Now;
            while (true) {
                if ((millisecondsTimeout != Timeout.Infinite 
                     && (DateTime.Now - startWait).TotalMilliseconds >= millisecondsTimeout)) {
                    throw new TimeoutException ();
                }

                cancellationToken.ThrowIfCancellationRequested ();

                if (fibers.All (f => f.IsCompleted)) {
                    if (fibers.Any (f => f.IsCanceled)) {
                        throw new System.Threading.OperationCanceledException ();
                    }
                    if (fibers.Any (f => f.IsFaulted)) {
                        throw new AggregateException (
                            fibers.Where (f => f.IsFaulted).Select (f => f.Exception));
                    }
                    yield break;
                }

                yield return FiberInstruction.YieldToAnyFiber;
            }
        }

        /// <summary>
        /// Returns a fiber that waits on all tasks to complete.
        /// </summary>
        /// <remarks>
        /// `Fiber.ResultAsObject` will be `true` if all tasks complete
        /// successfully or `false` if cancelled or timeout.
        /// </remarks>
        /// <returns>A fiber that waits on all tasks to complete.</returns>
        /// <param name="tasks">Tasks to wait for completion.</param>
        public static Fiber WhenAll (params Task [] tasks)
        {
            return WhenAll (tasks, Timeout.Infinite, CancellationToken.None);
        }

        /// <summary>
        /// Returns a fiber that waits on all tasks to complete.
        /// </summary>
        /// <remarks>
        /// `Fiber.ResultAsObject` will be `true` if all tasks complete
        /// successfully or `false` if cancelled or timeout.
        /// </remarks>
        /// <returns>A fiber that waits on all tasks to complete.</returns>
        /// <param name="tasks">Tasks to wait for completion.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static Fiber WhenAll (Task [] tasks, CancellationToken cancellationToken)
        {
            return WhenAll (tasks, Timeout.Infinite, cancellationToken);
        }

        /// <summary>
        /// Returns a fiber that waits on all tasks to complete.
        /// </summary>
        /// <remarks>
        /// `Fiber.ResultAsObject` will be `true` if all tasks complete
        /// successfully or `false` if cancelled or timeout.
        /// </remarks>
        /// <returns>A fiber that waits on all tasks to complete.</returns>
        /// <param name="tasks">Tasks to wait for completion.</param>
        /// <param name="timeout">Timeout.</param>
        public static Fiber WhenAll (Task [] tasks, TimeSpan timeout)
        {
            return WhenAll (tasks, CheckTimeout (timeout), CancellationToken.None);
        }

        /// <summary>
        /// Returns a fiber that waits on all tasks to complete.
        /// </summary>
        /// <remarks>
        /// `Fiber.ResultAsObject` will be `true` if all tasks complete
        /// successfully or `false` if cancelled or timeout.
        /// </remarks>
        /// <returns>A fiber that waits on all tasks to complete.</returns>
        /// <param name="tasks">Tasks to wait for completion.</param>
        /// <param name="millisecondsTimeout">Milliseconds timeout.</param>
        public static Fiber WhenAll (Task [] tasks, int millisecondsTimeout)
        {
            return WhenAll (tasks, millisecondsTimeout, CancellationToken.None);
        }

        /// <summary>
        /// Returns a fiber that waits on all tasks to complete.
        /// </summary>
        /// <remarks>
        /// `Fiber.ResultAsObject` will be `true` if all tasks complete
        /// successfully or `false` if cancelled or timeout.
        /// </remarks>
        /// <returns>A fiber that waits on all tasks to complete.</returns>
        /// <param name="tasks">Tasks to wait for completion.</param>
        /// <param name="millisecondsTimeout">Milliseconds timeout.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static Fiber WhenAll (Task [] tasks, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            return WhenAll (tasks, millisecondsTimeout, cancellationToken, FiberScheduler.Current);
        }

        /// <summary>
        /// Returns a fiber that waits on all tasks to complete.
        /// </summary>
        /// <remarks>
        /// `Fiber.ResultAsObject` will be `true` if all tasks complete
        /// successfully or `false` if cancelled or timeout.
        /// </remarks>
        /// <returns>A fiber that waits on all tasks to complete.</returns>
        /// <param name="tasks">Tasks to wait for completion.</param>
        /// <param name="millisecondsTimeout">Milliseconds timeout.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="scheduler">Scheduler.</param>
        public static Fiber WhenAll (Task [] tasks, int millisecondsTimeout, CancellationToken cancellationToken, FiberScheduler scheduler)
        {
            if (tasks == null)
                throw new ArgumentNullException ("tasks");

            foreach (var fiber in tasks) {
                if (fiber == null)
                    throw new ArgumentException ("tasks", "the tasks argument contains a null element");
            }

            return Fiber.Factory.StartNew (WhenAllTasksCoroutine (tasks, millisecondsTimeout, cancellationToken), cancellationToken, scheduler);
        }

        /// <summary>
        /// Returns a fiber that waits on all tasks to complete.
        /// </summary>
        /// <remarks>
        /// `Fiber.ResultAsObject` will be `true` if all tasks complete
        /// successfully or `false` if cancelled or timeout.
        /// </remarks>
        /// <returns>A fiber that waits on all tasks to complete.</returns>
        /// <param name="tasks">Tasks to wait for completion.</param>
        /// <param name="millisecondsTimeout">Milliseconds timeout.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="scheduler">Scheduler.</param>
        public static Fiber WhenAll (IEnumerable<Task> tasks, int millisecondsTimeout, CancellationToken cancellationToken, FiberScheduler scheduler)
        {
            return WhenAll (tasks.ToArray (), millisecondsTimeout, cancellationToken, scheduler);
        }

        static IEnumerator WhenAllTasksCoroutine (IEnumerable<Task> tasks, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            var startWait = DateTime.Now;
            while (true) {
                if ((millisecondsTimeout != Timeout.Infinite
                     && (DateTime.Now - startWait).TotalMilliseconds >= millisecondsTimeout)) {
                    throw new TimeoutException ();
                }

                cancellationToken.ThrowIfCancellationRequested ();

                if (tasks.All (t => t.IsCompleted)) {
                    if (tasks.Any (t => t.IsCanceled)) {
                        throw new System.Threading.OperationCanceledException ();
                    }
                    if (tasks.Any (t => t.IsFaulted)) {
                        throw new AggregateException (
                            tasks.Where (t => t.IsFaulted).SelectMany (t => t.Exception.InnerExceptions));
                    }
                    yield break;
                }

                yield return FiberInstruction.YieldToAnyFiber;
            }
        }
    }
}

