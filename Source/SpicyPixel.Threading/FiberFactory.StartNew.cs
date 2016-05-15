using System;
using System.Collections;
using System.Threading;

namespace SpicyPixel.Threading
{
    public partial class FiberFactory
    {
        /// <summary>
        /// Start executing a new fiber using the default scheduler on the thread.
        /// </summary>
        /// <returns>
        /// Returns a <see cref="Fiber"/>
        /// that can be yielded against to wait for the fiber to complete.
        /// </returns>
        /// <param name='coroutine'>
        /// A couroutine to execute on the fiber.
        /// </param>
        public Fiber StartNew(IEnumerator coroutine)
        {
            return StartNew(coroutine, cancellationToken);
        }

        /// <summary>
        /// Start executing a new fiber using the default scheduler on the thread.
        /// </summary>
        /// <returns>
        /// Returns a <see cref="Fiber"/>
        /// that can be yielded against to wait for the fiber to complete.
        /// </returns>
        /// <param name='coroutine'>
        /// A couroutine to execute on the fiber.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Fiber StartNew(IEnumerator coroutine, CancellationToken cancellationToken)
        {
            return StartNew(coroutine, cancellationToken, GetScheduler());
        }

        /// <summary>
        /// Start executing a new fiber using the specified scheduler.
        /// </summary>
        /// <returns>
        /// Returns a <see cref="Fiber"/>
        /// that can be yielded against to wait for the fiber to complete.
        /// </returns>
        /// <param name='coroutine'>
        /// A couroutine to execute on the fiber.
        /// </param>
        /// <param name='scheduler'>
        /// A scheduler to execute the fiber on.
        /// </param>
        public Fiber StartNew(IEnumerator coroutine, FiberScheduler scheduler)
        {
            return StartNew(coroutine, cancellationToken, scheduler);
        }

        /// <summary>
        /// Start executing a new fiber using the specified scheduler.
        /// </summary>
        /// <returns>
        /// Returns a <see cref="Fiber"/>
        /// that can be yielded against to wait for the fiber to complete.
        /// </returns>
        /// <param name='coroutine'>
        /// A couroutine to execute on the fiber.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name='scheduler'>
        /// A scheduler to execute the fiber on.
        /// </param>
        public Fiber StartNew(IEnumerator coroutine, CancellationToken cancellationToken, FiberScheduler scheduler)
        {
            var fiber = new Fiber(coroutine, cancellationToken);
            fiber.Start(scheduler);
            return fiber;
        }

        /// <summary>
        /// Start executing a new fiber using the default scheduler on the thread.
        /// </summary>
        /// <returns>
        /// Returns a <see cref="Fiber"/>
        /// that can be yielded against to wait for the fiber to complete.
        /// </returns>
        /// <param name='action'>
        /// A non-blocking action to execute on the fiber.
        /// </param>
        public Fiber StartNew(Action action)
        {
            return StartNew(action, cancellationToken);
        }

        /// <summary>
        /// Start executing a new fiber using the default scheduler on the thread.
        /// </summary>
        /// <returns>
        /// Returns a <see cref="Fiber"/>
        /// that can be yielded against to wait for the fiber to complete.
        /// </returns>
        /// <param name='action'>
        /// A non-blocking action to execute on the fiber.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Fiber StartNew(Action action, CancellationToken cancellationToken)
        {
            return StartNew(action, cancellationToken, GetScheduler());
        }

        /// <summary>
        /// Start executing a new fiber using the default scheduler on the thread.
        /// </summary>
        /// <returns>
        /// Returns a <see cref="Fiber"/>
        /// that can be yielded against to wait for the fiber to complete.
        /// </returns>
        /// <param name='action'>
        /// A non-blocking action to execute on the fiber.
        /// </param>
        /// <param name='scheduler'>
        /// A scheduler to execute the fiber on.
        /// </param>
        public Fiber StartNew(Action action, FiberScheduler scheduler)
        {
            return StartNew(action, cancellationToken, scheduler);
        }

        /// <summary>
        /// Start executing a new fiber using the default scheduler on the thread.
        /// </summary>
        /// <returns>
        /// Returns a <see cref="Fiber"/>
        /// that can be yielded against to wait for the fiber to complete.
        /// </returns>
        /// <param name='action'>
        /// A non-blocking action to execute on the fiber.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name='scheduler'>
        /// A scheduler to execute the fiber on.
        /// </param>
        public Fiber StartNew(Action action, CancellationToken cancellationToken, FiberScheduler scheduler)
        {
            var fiber = new Fiber(action, cancellationToken);
            fiber.Start(scheduler);
            return fiber;
        }

        /// <summary>
        /// Start executing a new fiber using the default scheduler on the thread.
        /// </summary>
        /// <returns>
        /// Returns a <see cref="Fiber"/>
        /// that can be yielded against to wait for the fiber to complete.
        /// </returns>
        /// <param name='action'>
        /// A non-blocking action to execute on the fiber.
        /// </param>
        /// <param name='state'>
        /// State to pass to the action.
        /// </param>
        public Fiber StartNew(Action<object> action, object state)
        {
            return StartNew(action, state, cancellationToken);
        }

        /// <summary>
        /// Start executing a new fiber using the default scheduler on the thread.
        /// </summary>
        /// <returns>
        /// Returns a <see cref="Fiber"/>
        /// that can be yielded against to wait for the fiber to complete.
        /// </returns>
        /// <param name='action'>
        /// A non-blocking action to execute on the fiber.
        /// </param>
        /// <param name='state'>
        /// State to pass to the action.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Fiber StartNew(Action<object> action, object state, CancellationToken cancellationToken)
        {
            return StartNew(action, state, cancellationToken, GetScheduler());
        }

        /// <summary>
        /// Start executing a new fiber using the default scheduler on the thread.
        /// </summary>
        /// <returns>
        /// Returns a <see cref="Fiber"/>
        /// that can be yielded against to wait for the fiber to complete.
        /// </returns>
        /// <param name='action'>
        /// A non-blocking action to execute on the fiber.
        /// </param>
        /// <param name='state'>
        /// State to pass to the action.
        /// </param>
        /// <param name='scheduler'>
        /// A scheduler to execute the fiber on.
        /// </param>
        public Fiber StartNew(Action<object> action, object state, FiberScheduler scheduler)
        {
            return StartNew(action, state, cancellationToken, scheduler);
        }

        /// <summary>
        /// Start executing a new fiber using the default scheduler on the thread.
        /// </summary>
        /// <returns>
        /// Returns a <see cref="Fiber"/>
        /// that can be yielded against to wait for the fiber to complete.
        /// </returns>
        /// <param name='action'>
        /// A non-blocking action to execute on the fiber.
        /// </param>
        /// <param name='state'>
        /// State to pass to the action.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name='scheduler'>
        /// A scheduler to execute the fiber on.
        /// </param>
        public Fiber StartNew(Action<object> action, object state, CancellationToken cancellationToken, FiberScheduler scheduler)
        {
            var fiber = new Fiber(action, state, cancellationToken);
            fiber.Start(scheduler);
            return fiber;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.Fiber"/> class.
        /// </summary>
        /// <param name='func'>
        /// A non-blocking function that returns a <see cref="Fiber"/> when complete.
        /// </param>
        public Fiber StartNew(Func<FiberInstruction> func)
        {
            return StartNew(func, cancellationToken);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.Fiber"/> class.
        /// </summary>
        /// <param name='func'>
        /// A non-blocking function that returns a <see cref="Fiber"/> when complete.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Fiber StartNew(Func<FiberInstruction> func, CancellationToken cancellationToken)
        {
            return StartNew(func, cancellationToken, GetScheduler());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.Fiber"/> class.
        /// </summary>
        /// <param name='func'>
        /// A non-blocking function that returns a <see cref="Fiber"/> when complete.
        /// </param>
        /// <param name='scheduler'>
        /// A scheduler to execute the fiber on.
        /// </param>
        public Fiber StartNew(Func<FiberInstruction> func, FiberScheduler scheduler)
        {
            return StartNew(func, cancellationToken, scheduler);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.Fiber"/> class.
        /// </summary>
        /// <param name='func'>
        /// A non-blocking function that returns a <see cref="Fiber"/> when complete.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name='scheduler'>
        /// A scheduler to execute the fiber on.
        /// </param>
        public Fiber StartNew(Func<FiberInstruction> func, CancellationToken cancellationToken, FiberScheduler scheduler)
        {
            var fiber = new Fiber(func, cancellationToken);
            fiber.Start(scheduler);
            return fiber;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.Fiber"/> class.
        /// </summary>
        /// <param name='func'>
        /// A non-blocking function that returns a <see cref="Fiber"/> when complete.
        /// </param>
        /// <param name='state'>
        /// State to pass to the function.
        /// </param>
        public Fiber StartNew(Func<object, FiberInstruction> func, object state)
        {
            return StartNew(func, state, cancellationToken);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.Fiber"/> class.
        /// </summary>
        /// <param name='func'>
        /// A non-blocking function that returns a <see cref="Fiber"/> when complete.
        /// </param>
        /// <param name='state'>
        /// State to pass to the function.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Fiber StartNew(Func<object, FiberInstruction> func, object state, CancellationToken cancellationToken)
        {
            return StartNew(func, state, cancellationToken, GetScheduler());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.Fiber"/> class.
        /// </summary>
        /// <param name='func'>
        /// A non-blocking function that returns a <see cref="Fiber"/> when complete.
        /// </param>
        /// <param name='state'>
        /// State to pass to the function.
        /// </param>
        /// <param name='scheduler'>
        /// A scheduler to execute the fiber on.
        /// </param>
        public Fiber StartNew(Func<object, FiberInstruction> func, object state, FiberScheduler scheduler)
        {
            return StartNew(func, state, cancellationToken, scheduler);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.Fiber"/> class.
        /// </summary>
        /// <param name='func'>
        /// A non-blocking function that returns a <see cref="Fiber"/> when complete.
        /// </param>
        /// <param name='state'>
        /// State to pass to the function.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name='scheduler'>
        /// A scheduler to execute the fiber on.
        /// </param>
        public Fiber StartNew(Func<object, FiberInstruction> func, object state, CancellationToken cancellationToken, FiberScheduler scheduler)
        {
            var fiber = new Fiber(func, state, cancellationToken);
            fiber.Start(scheduler);
            return fiber;
        }

        FiberScheduler GetScheduler()
        {
            return scheduler ?? FiberScheduler.Current;
        }
    }
}

