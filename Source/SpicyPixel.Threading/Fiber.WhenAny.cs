using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;

namespace SpicyPixel.Threading
{
    public partial class Fiber
    {
        /// <summary>
        /// Returns a fiber that completes when any fiber finishes.
        /// </summary>
        /// <remarks>
        /// `Fiber.ResultAsObject` will be the `Fiber` that completed.
        /// </remarks>
        /// <returns>A fiber that completes when any fiber finishes.</returns>
        /// <param name="fibers">Fibers to wait for completion.</param>
        public static Fiber WhenAny (params Fiber[] fibers)
        {
            return WhenAny (fibers, Timeout.Infinite, CancellationToken.None);
        }

        /// <summary>
        /// Returns a fiber that completes when any fiber finishes.
        /// </summary>
        /// <remarks>
        /// `Fiber.ResultAsObject` will be the `Fiber` that completed.
        /// </remarks>
        /// <returns>A fiber that completes when any fiber finishes.</returns>
        /// <param name="fibers">Fibers to wait for completion.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static Fiber WhenAny (Fiber[] fibers, CancellationToken cancellationToken)
        {
            return WhenAny (fibers, Timeout.Infinite, cancellationToken);
        }

        /// <summary>
        /// Returns a fiber that completes when any fiber finishes.
        /// </summary>
        /// <remarks>
        /// `Fiber.ResultAsObject` will be the `Fiber` that completed.
        /// </remarks>
        /// <returns>A fiber that completes when any fiber finishes.</returns>
        /// <param name="fibers">Fibers to wait for completion.</param>
        /// <param name="timeout">Timeout.</param>
        public static Fiber WhenAny (Fiber[] fibers, TimeSpan timeout)
        {
            return WhenAny (fibers, CheckTimeout (timeout), CancellationToken.None);
        }

        /// <summary>
        /// Returns a fiber that completes when any fiber finishes.
        /// </summary>
        /// <remarks>
        /// `Fiber.ResultAsObject` will be the `Fiber` that completed.
        /// </remarks>
        /// <returns>A fiber that completes when any fiber finishes.</returns>
        /// <param name="fibers">Fibers to wait for completion.</param>
        /// <param name="millisecondsTimeout">Milliseconds timeout.</param>
        public static Fiber WhenAny (Fiber[] fibers, int millisecondsTimeout)
        {
            return WhenAny (fibers, millisecondsTimeout, CancellationToken.None);
        }

        /// <summary>
        /// Returns a fiber that completes when any fiber finishes.
        /// </summary>
        /// <remarks>
        /// `Fiber.ResultAsObject` will be the `Fiber` that completed.
        /// </remarks>
        /// <returns>A fiber that completes when any fiber finishes.</returns>
        /// <param name="fibers">Fibers to wait for completion.</param>
        /// <param name="millisecondsTimeout">Milliseconds timeout.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static Fiber WhenAny (Fiber[] fibers, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            return WhenAny(fibers, millisecondsTimeout, cancellationToken, FiberScheduler.Current);
        }

        /// <summary>
        /// Returns a fiber that completes when any fiber finishes.
        /// </summary>
        /// <remarks>
        /// `Fiber.ResultAsObject` will be the `Fiber` that completed.
        /// </remarks>
        /// <returns>A fiber that completes when any fiber finishes.</returns>
        /// <param name="fibers">Fibers to wait for completion.</param>
        /// <param name="millisecondsTimeout">Milliseconds timeout.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="scheduler">Scheduler.</param>
        public static Fiber WhenAny (Fiber[] fibers, int millisecondsTimeout, CancellationToken cancellationToken, FiberScheduler scheduler)
        {
            if (fibers == null)
                throw new ArgumentNullException ("fibers");

            foreach (var fiber in fibers) {
                if (fiber == null)
                    throw new ArgumentException ("fibers", "the fibers argument contains a null element");              
            }

            return Fiber.Factory.StartNew(WhenAnyFibersCoroutine(fibers, millisecondsTimeout, cancellationToken), scheduler);
        }
            
        /// <summary>
        /// Returns a fiber that completes when any fiber finishes.
        /// </summary>
        /// <remarks>
        /// `Fiber.ResultAsObject` will be the `Fiber` that completed.
        /// </remarks>
        /// <returns>A fiber that completes when any fiber finishes.</returns>
        /// <param name="fibers">Fibers to wait for completion.</param>
        /// <param name="millisecondsTimeout">Milliseconds timeout.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="scheduler">Scheduler.</param>
        public static Fiber WhenAny (IEnumerable<Fiber> fibers, int millisecondsTimeout, CancellationToken cancellationToken, FiberScheduler scheduler)
        {
            return WhenAny(fibers.ToArray(), millisecondsTimeout, cancellationToken, scheduler);
        }

        static IEnumerator WhenAnyFibersCoroutine(IEnumerable<Fiber> fibers, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            var startWait = DateTime.Now;
            while (true) {
                if ((millisecondsTimeout != Timeout.Infinite 
                    && (DateTime.Now - startWait).TotalMilliseconds >= millisecondsTimeout) ||
                    cancellationToken.IsCancellationRequested) {
                    yield return new FiberResult(null);
                }

                var fiber = fibers.FirstOrDefault(f => f.IsCompleted);

                if (fiber != null) {
                    yield return new FiberResult(fiber);
                }

                yield return FiberInstruction.YieldToAnyFiber;
            }
        }

        /// <summary>
        /// Returns a fiber that completes when any task finishes.
        /// </summary>
        /// <remarks>
        /// `Fiber.ResultAsObject` will be the `Task` that completed.
        /// </remarks>
        /// <returns>A fiber that completes when any task finishes.</returns>
        /// <param name="tasks">Tasks to wait for completion.</param>
        public static Fiber WhenAny (params Task [] tasks)
        {
            return WhenAny (tasks, Timeout.Infinite, CancellationToken.None);
        }

        /// <summary>
        /// Returns a fiber that completes when any task finishes.
        /// </summary>
        /// <remarks>
        /// `Fiber.ResultAsObject` will be the `Task` that completed.
        /// </remarks>
        /// <returns>A fiber that completes when any task finishes.</returns>
        /// <param name="tasks">Tasks to wait for completion.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static Fiber WhenAny (Task [] tasks, CancellationToken cancellationToken)
        {
            return WhenAny (tasks, Timeout.Infinite, cancellationToken);
        }

        /// <summary>
        /// Returns a fiber that completes when any task finishes.
        /// </summary>
        /// <remarks>
        /// `Fiber.ResultAsObject` will be the `Task` that completed.
        /// </remarks>
        /// <returns>A fiber that completes when any task finishes.</returns>
        /// <param name="tasks">Tasks to wait for completion.</param>
        /// <param name="timeout">Timeout.</param>
        public static Fiber WhenAny (Task [] tasks, TimeSpan timeout)
        {
            return WhenAny (tasks, CheckTimeout (timeout), CancellationToken.None);
        }

        /// <summary>
        /// Returns a fiber that completes when any task finishes.
        /// </summary>
        /// <remarks>
        /// `Fiber.ResultAsObject` will be the `Task` that completed.
        /// </remarks>
        /// <returns>A fiber that completes when any task finishes.</returns>
        /// <param name="tasks">Tasks to wait for completion.</param>
        /// <param name="millisecondsTimeout">Milliseconds timeout.</param>
        public static Fiber WhenAny (Task [] tasks, int millisecondsTimeout)
        {
            return WhenAny (tasks, millisecondsTimeout, CancellationToken.None);
        }

        /// <summary>
        /// Returns a fiber that completes when any task finishes.
        /// </summary>
        /// <remarks>
        /// `Fiber.ResultAsObject` will be the `Task` that completed.
        /// </remarks>
        /// <returns>A fiber that completes when any task finishes.</returns>
        /// <param name="tasks">Tasks to wait for completion.</param>
        /// <param name="millisecondsTimeout">Milliseconds timeout.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static Fiber WhenAny (Task [] tasks, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            return WhenAny (tasks, millisecondsTimeout, cancellationToken, FiberScheduler.Current);
        }

        /// <summary>
        /// Returns a fiber that completes when any task finishes.
        /// </summary>
        /// <remarks>
        /// `Fiber.ResultAsObject` will be the `Task` that completed.
        /// </remarks>
        /// <returns>A fiber that completes when any task finishes.</returns>
        /// <param name="tasks">Tasks to wait for completion.</param>
        /// <param name="millisecondsTimeout">Milliseconds timeout.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="scheduler">Scheduler.</param>
        public static Fiber WhenAny (Task [] tasks, int millisecondsTimeout, CancellationToken cancellationToken, FiberScheduler scheduler)
        {
            if (tasks == null)
                throw new ArgumentNullException ("tasks");

            foreach (var fiber in tasks) {
                if (fiber == null)
                    throw new ArgumentException ("tasks", "the tasks argument contains a null element");
            }

            return Fiber.Factory.StartNew (WhenAnyTasksCoroutine (tasks, millisecondsTimeout, cancellationToken), scheduler);
        }

        /// <summary>
        /// Returns a fiber that completes when any task finishes.
        /// </summary>
        /// <remarks>
        /// `Fiber.ResultAsObject` will be the `Task` that completed.
        /// </remarks>
        /// <returns>A fiber that completes when any task finishes.</returns>
        /// <param name="tasks">Tasks to wait for completion.</param>
        /// <param name="millisecondsTimeout">Milliseconds timeout.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="scheduler">Scheduler.</param>
        public static Fiber WhenAny (IEnumerable<Task> tasks, int millisecondsTimeout, CancellationToken cancellationToken, FiberScheduler scheduler)
        {
            return WhenAny (tasks.ToArray (), millisecondsTimeout, cancellationToken, scheduler);
        }

        static IEnumerator WhenAnyTasksCoroutine (IEnumerable<Task> tasks, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            var startWait = DateTime.Now;
            while (true) {
                if ((millisecondsTimeout != Timeout.Infinite
                    && (DateTime.Now - startWait).TotalMilliseconds >= millisecondsTimeout) ||
                    cancellationToken.IsCancellationRequested) {
                    yield return new FiberResult (null);
                }

                var fiber = tasks.FirstOrDefault (f => f.IsCompleted);

                if (fiber != null) {
                    yield return new FiberResult (fiber);
                }

                yield return FiberInstruction.YieldToAnyFiber;
            }
        }
    }
}

