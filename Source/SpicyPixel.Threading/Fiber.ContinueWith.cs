using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections;

namespace SpicyPixel.Threading
{
    public partial class Fiber
    {
        /// <summary>
        /// Creates a continuation that executes asynchronously when the target fiber completes.
        /// </summary>
        /// <returns>A fiber that executes when the target fiber completes.</returns>
        /// <param name="continuationCoroutine">Continuation coroutine.</param>
        public Fiber ContinueWith(IEnumerator continuationCoroutine)
        {
            return ContinueWith(continuationCoroutine, CancellationToken.None, FiberContinuationOptions.None, FiberScheduler.Current);
        }

        /// <summary>
        /// Creates a continuation that executes asynchronously when the target fiber completes.
        /// </summary>
        /// <returns>A fiber that executes when the target fiber completes.</returns>
        /// <param name="continuationCoroutine">Continuation coroutine.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Fiber ContinueWith(IEnumerator continuationCoroutine, CancellationToken cancellationToken)
        {
            return ContinueWith(continuationCoroutine, cancellationToken, FiberContinuationOptions.None, FiberScheduler.Current);
        }

        /// <summary>
        /// Creates a continuation that executes asynchronously when the target fiber completes.
        /// </summary>
        /// <returns>A fiber that executes when the target fiber completes.</returns>
        /// <param name="continuationCoroutine">Continuation coroutine.</param>
        /// <param name="continuationOptions">Continuation options.</param>
        public Fiber ContinueWith(IEnumerator continuationCoroutine, FiberContinuationOptions continuationOptions)
        {
            return ContinueWith(continuationCoroutine, CancellationToken.None, continuationOptions, FiberScheduler.Current);
        }

        /// <summary>
        /// Creates a continuation that executes asynchronously when the target fiber completes.
        /// </summary>
        /// <returns>A fiber that executes when the target fiber completes.</returns>
        /// <param name="continuationCoroutine">Continuation coroutine.</param>
        /// <param name="scheduler">Scheduler.</param>
        public Fiber ContinueWith(IEnumerator continuationCoroutine, FiberScheduler scheduler)
        {
            return ContinueWith(continuationCoroutine, CancellationToken.None, FiberContinuationOptions.None, scheduler);
        }

        /// <summary>
        /// Creates a continuation that executes asynchronously when the target fiber completes.
        /// </summary>
        /// <returns>A fiber that executes when the target fiber completes.</returns>
        /// <param name="continuationCoroutine">Continuation coroutine.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="continuationOptions">Continuation options.</param>
        /// <param name="scheduler">Scheduler.</param>
        public Fiber ContinueWith(IEnumerator continuationCoroutine, CancellationToken cancellationToken,
            FiberContinuationOptions continuationOptions, FiberScheduler scheduler)
        {
            if (continuationCoroutine == null)
                throw new ArgumentNullException("continuationCoroutine");

            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            var fiber = new Fiber(continuationCoroutine, cancellationToken);

            fiber.antecedent = this;

            // Lazy create queue
            if (continuations == null)
                continuations = new Queue<FiberContinuation>();

            continuations.Enqueue(new FiberContinuation(fiber, continuationOptions, scheduler));

            return fiber;
        }

        /// <summary>
        /// Creates a continuation that executes asynchronously when the target fiber completes.
        /// </summary>
        /// <returns>A fiber that executes when the target fiber completes.</returns>
        /// <param name="continuationAction">Continuation action.</param>
        public Fiber ContinueWith(Action<Fiber> continuationAction)
        {
            return ContinueWith(continuationAction, CancellationToken.None, FiberContinuationOptions.None, FiberScheduler.Current);
        }

        /// <summary>
        /// Creates a continuation that executes asynchronously when the target fiber completes.
        /// </summary>
        /// <returns>A fiber that executes when the target fiber completes.</returns>
        /// <param name="continuationAction">Continuation action.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Fiber ContinueWith(Action<Fiber> continuationAction, CancellationToken cancellationToken)
        {
            return ContinueWith(continuationAction, cancellationToken, FiberContinuationOptions.None, FiberScheduler.Current);
        }

        /// <summary>
        /// Creates a continuation that executes asynchronously when the target fiber completes.
        /// </summary>
        /// <returns>A fiber that executes when the target fiber completes.</returns>
        /// <param name="continuationAction">Continuation action.</param>
        /// <param name="continuationOptions">Continuation options.</param>
        public Fiber ContinueWith(Action<Fiber> continuationAction, FiberContinuationOptions continuationOptions)
        {
            return ContinueWith(continuationAction, CancellationToken.None, continuationOptions, FiberScheduler.Current);
        }

        /// <summary>
        /// Creates a continuation that executes asynchronously when the target fiber completes.
        /// </summary>
        /// <returns>A fiber that executes when the target fiber completes.</returns>
        /// <param name="continuationAction">Continuation action.</param>
        /// <param name="scheduler">Scheduler.</param>
        public Fiber ContinueWith(Action<Fiber> continuationAction, FiberScheduler scheduler)
        {
            return ContinueWith(continuationAction, CancellationToken.None, FiberContinuationOptions.None, scheduler);
        }

        /// <summary>
        /// Creates a continuation that executes asynchronously when the target fiber completes.
        /// </summary>
        /// <returns>A fiber that executes when the target fiber completes.</returns>
        /// <param name="continuationAction">Continuation action.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="continuationOptions">Continuation options.</param>
        /// <param name="scheduler">Scheduler.</param>
        public Fiber ContinueWith(Action<Fiber> continuationAction, CancellationToken cancellationToken,
                                   FiberContinuationOptions continuationOptions, FiberScheduler scheduler)
        {
            return ContinueWith((fiber, state) => continuationAction(fiber), null, cancellationToken, continuationOptions, scheduler);
        }

        /// <summary>
        /// Creates a continuation that executes asynchronously when the target fiber completes.
        /// </summary>
        /// <returns>A fiber that executes when the target fiber completes.</returns>
        /// <param name="continuationAction">Continuation action.</param>
        /// <param name="state">State.</param>
        public Fiber ContinueWith(Action<Fiber, object> continuationAction, object state)
        {
            return ContinueWith(continuationAction, state, CancellationToken.None, FiberContinuationOptions.None, FiberScheduler.Current);
        }

        /// <summary>
        /// Creates a continuation that executes asynchronously when the target fiber completes.
        /// </summary>
        /// <returns>A fiber that executes when the target fiber completes.</returns>
        /// <param name="continuationAction">Continuation action.</param>
        /// <param name="state">State.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Fiber ContinueWith(Action<Fiber, object> continuationAction, object state, CancellationToken cancellationToken)
        {
            return ContinueWith(continuationAction, state, cancellationToken, FiberContinuationOptions.None, FiberScheduler.Current);
        }

        /// <summary>
        /// Creates a continuation that executes asynchronously when the target fiber completes.
        /// </summary>
        /// <returns>A fiber that executes when the target fiber completes.</returns>
        /// <param name="continuationAction">Continuation action.</param>
        /// <param name="state">State.</param>
        /// <param name="continuationOptions">Continuation options.</param>
        public Fiber ContinueWith(Action<Fiber, object> continuationAction, object state, FiberContinuationOptions continuationOptions)
        {
            return ContinueWith(continuationAction, state, CancellationToken.None, continuationOptions, FiberScheduler.Current);
        }

        /// <summary>
        /// Creates a continuation that executes asynchronously when the target fiber completes.
        /// </summary>
        /// <returns>A fiber that executes when the target fiber completes.</returns>
        /// <param name="continuationAction">Continuation action.</param>
        /// <param name="state">State.</param>
        /// <param name="scheduler">Scheduler.</param>
        public Fiber ContinueWith(Action<Fiber, object> continuationAction, object state, FiberScheduler scheduler)
        {
            return ContinueWith(continuationAction, state, CancellationToken.None, FiberContinuationOptions.None, scheduler);
        }

        /// <summary>
        /// Creates a continuation that executes asynchronously when the target fiber completes.
        /// </summary>
        /// <returns>A fiber that executes when the target fiber completes.</returns>
        /// <param name="continuationAction">Continuation action.</param>
        /// <param name="state">State.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="continuationOptions">Continuation options.</param>
        /// <param name="scheduler">Scheduler.</param>
        public Fiber ContinueWith(Action<Fiber, object> continuationAction, object state, CancellationToken cancellationToken,
                                   FiberContinuationOptions continuationOptions, FiberScheduler scheduler)
        {
            if (continuationAction == null)
                throw new ArgumentNullException("continuationAction");
            
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            var fiber = new Fiber((obj) => continuationAction(this, obj),
                            state, cancellationToken);
            
            fiber.antecedent = this;

            // Lazy create queue
            if (continuations == null)
                continuations = new Queue<FiberContinuation>();

            continuations.Enqueue(new FiberContinuation(fiber, continuationOptions, scheduler));

            return fiber;
        }
    }
}

