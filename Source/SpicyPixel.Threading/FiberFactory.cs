using System;
using System.Threading;

namespace SpicyPixel.Threading
{
    /// <summary>
    /// A Fiber Factory for creating fibers with the same options.
    /// </summary>
    public partial class FiberFactory
    {
        readonly FiberScheduler scheduler;
        FiberContinuationOptions continuationOptions;
        CancellationToken cancellationToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.FiberFactory"/> class.
        /// </summary>
        public FiberFactory ()
            : this (CancellationToken.None, FiberContinuationOptions.None, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.FiberFactory"/> class.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        public FiberFactory (CancellationToken cancellationToken)
            : this (cancellationToken, FiberContinuationOptions.None, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.FiberFactory"/> class.
        /// </summary>
        /// <param name="scheduler">Scheduler.</param>
        public FiberFactory (FiberScheduler scheduler)
            : this (CancellationToken.None, FiberContinuationOptions.None, scheduler)
        {   
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.FiberFactory"/> class.
        /// </summary>
        /// <param name="continuationOptions">Continuation options.</param>
        public FiberFactory (FiberContinuationOptions continuationOptions)
            : this (CancellationToken.None, continuationOptions, null)
        {   
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.FiberFactory"/> class.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="continuationOptions">Continuation options.</param>
        /// <param name="scheduler">Scheduler.</param>
        public FiberFactory (CancellationToken cancellationToken, FiberContinuationOptions continuationOptions,
            FiberScheduler scheduler)
        {
            this.cancellationToken = cancellationToken;
            this.continuationOptions = continuationOptions;
            this.scheduler = scheduler;

            CheckContinuationOptions (continuationOptions);
        }

        internal static void CheckContinuationOptions (FiberContinuationOptions continuationOptions)
        {
            if ((continuationOptions & (FiberContinuationOptions.OnlyOnRanToCompletion | FiberContinuationOptions.NotOnRanToCompletion)) != 0)
                throw new ArgumentOutOfRangeException ("continuationOptions");
        }

        /// <summary>
        /// Gets the scheduler.
        /// </summary>
        /// <value>The scheduler.</value>
        public FiberScheduler Scheduler {
            get {
                return scheduler;
            }
        }

        /// <summary>
        /// Gets the continuation options.
        /// </summary>
        /// <value>The continuation options.</value>
        public FiberContinuationOptions ContinuationOptions {
            get {
                return continuationOptions;
            }
        }

        /// <summary>
        /// Gets the cancellation token.
        /// </summary>
        /// <value>The cancellation token.</value>
        public CancellationToken CancellationToken {
            get {
                return cancellationToken;
            }
        }
    }
}

