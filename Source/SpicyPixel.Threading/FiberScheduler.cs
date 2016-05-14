/*

Author: Aaron Oneal, http://aarononeal.info

Copyright (c) 2012 Spicy Pixel, http://spicypixel.com

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

*/
using System;
using System.Threading;

namespace SpicyPixel.Threading
{
	/// <summary>
	/// Schedules fibers for execution.
	/// </summary>
	/// <remarks>
	/// Schedulers are bound to the thread they are created on
	/// and they install a SynchronizationContext which is active during
	/// execution. Schedulers have an implementation specific update
	/// method or run loop. The interface is otherwise thin since schedulers
	/// are generally not intended to be used directly.
	/// Scheduling work is accomplished by invoking methods on
	/// <see cref="Fiber"/> or <see cref="SynchronizationContext"/>.
	/// </remarks>
	public abstract class FiberScheduler : IFiberScheduler, IDisposable
	{
        /// <summary>
        /// Default scheduler for the thread to support Fiber.Start()
        /// </summary>
        [ThreadStatic]
        private static FiberScheduler currentScheduler;

        CancellationTokenSource cancelSource = new CancellationTokenSource();

        /// <summary>
        /// Gets the cancellation token set when the scheduler is destroyed.
        /// </summary>
        /// <value>
        /// The cancellation token.
        /// </value>
        public CancellationToken CancellationToken
        {
            get { return cancelSource.Token; }
        }

        /// <summary>
        /// Gets the default fiber scheduler for the thread.
        /// </summary>
        /// <remarks>
        /// Every thread has a default scheduler which Fiber.Start() uses.
        /// </remarks>
        /// <value>
        /// The default fiber scheduler for the thread.
        /// </value>
        public static FiberScheduler Current 
        {
            get 
            { 
                if(currentScheduler == null)
                {
                    // Make a new scheduler
                    SetCurrentScheduler(new SystemFiberScheduler(), true);
                }

                return currentScheduler;
            }
			
			// Private for now. Better not to let the caller set this and
			// instead let it be set during execution. This way any
			// new fibers inside a running fiber will use the same scheduler,
			// and there is no risk of the caller changing it out from
			// under the framework or wind up with a scheduler they didn't
			// expect. For example, a caller in Unity might think they
			// could set the current scheduler in Awake() and use the
			// same scheduler in Update() even though another MonoBehaviour
			// Awake() set it to something else because they all share
			// the same thread. The current scheduler on a thread will
			// get changed out automatically during Task.Execute()
			// invoked by Update() invoked by Run().
            /*private set
            {
                SetCurrentScheduler(value, false);
            }*/
        }

        static internal void SetCurrentScheduler(FiberScheduler scheduler, bool internalInvoke)
        {
            // Only allow scheduler changes on a thread when a fiber is not executing.
            // Ignore this check if this is an internal invocation such as from
            // Fiber.Execute() which needs to switch schedulers on demand.
            if(!internalInvoke && Fiber.CurrentFiber != null)
                throw new InvalidOperationException("The current scheduler for the thread cannot be changed from inside an executing fiber.");
         
            // Skip work if nothing to change
            if(currentScheduler == scheduler)
                return;

            // Assign the scheduler
            currentScheduler = scheduler;
            
            // Update the synchronization context if it changed
            if(currentScheduler != null && SynchronizationContext.Current != currentScheduler.SynchronizationContext)
                SynchronizationContext.SetSynchronizationContext(currentScheduler.SynchronizationContext);
            else if(currentScheduler == null && SynchronizationContext.Current != null)
                SynchronizationContext.SetSynchronizationContext(null);
        }

		/// <summary>
		/// The thread the scheduler was started on to know when inlining can be allowed.
		/// </summary>
		/// <value>
		/// The scheduler thread.
		/// </value>
		private Thread schedulerThread;
		
        /// <summary>
        /// Gets the synchronization context to dispatch work items to the scheduler.
        /// </summary>
        /// <remarks>
        /// Schedulers and consumers don't need to worry about this. They are expected
        /// to access the sync context from within an executing fiber by using
        /// SynchronizationContext.Current. This is here so that Fiber.Execute() can
        /// automatically setup the proper context as needed.
        /// </remarks>
        /// <value>
        /// The synchronization context.
        /// </value>
        internal SynchronizationContext SynchronizationContext { get; private set; }

		/*
		/// <summary>
		/// Gets a value indicating whether the current thread is the fiber scheduler thread.
		/// </summary>
		/// <value>
		/// <c>true</c> if this instance is the scheduler thread; otherwise, <c>false</c>.
		/// </value>
		public bool IsSchedulerThread
		{
			get { return Thread.CurrentThread == schedulerThread; }
		}
		*/
		
		/// <summary>
		/// Gets the thread the scheduler is running on.
		/// </summary>
		/// <value>
		/// The scheduler thread.
		/// </value>
		public Thread SchedulerThread
		{
			get { return schedulerThread; }
		}
		
		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="SpicyPixel.Threading.FiberScheduler"/> allows inlining.
		/// </summary>
		/// <value>
		/// <c>true</c> if inlining of tasks on the scheduler thread is allowed; otherwise, <c>false</c>. Default is <c>true</c>.
		/// </value>
		public bool AllowInlining { get; set; }
		
		/// <summary>
		/// Initializes a new instance of the <see cref="SpicyPixel.Threading.FiberScheduler"/> class.
		/// </summary>
		protected FiberScheduler()
		{
			AllowInlining = true;
			schedulerThread = Thread.CurrentThread;
			SynchronizationContext = new FiberSchedulerSynchronizationContext(this);
		}
				
		// Used by Fiber to invoke the protected methods
		void IFiberScheduler.QueueFiber(Fiber fiber)
		{
			this.QueueFiber(fiber);
		}
		
		// Used by Fiber to invoke the protected methods
		void IFiberScheduler.AbortRequested(Fiber fiber)
		{
			this.AbortRequested(fiber);
		}
		
		/// <summary>
		/// Run the blocking scheduler loop and perform the specified number of updates per second.
		/// </summary>
		/// <remarks>
		/// Not all schedulers support a blocking run loop that can be invoked by the caller.
		/// </remarks>
		/// <param name='fiber'>
		/// The initial fiber to start on the scheduler.
		/// </param>
		public void Run(Fiber fiber)
		{
			Run (fiber, CancellationToken.None, 0f);
		}

		/// <summary>
		/// Run the blocking scheduler loop and perform the specified number of updates per second.
		/// </summary>
		/// <remarks>
		/// Not all schedulers support a blocking run loop that can be invoked by the caller.
		/// </remarks>
		/// <param name='fiber'>
		/// The optional fiber to start execution from. If this is <c>null</c>, the loop
		/// will continue to execute until cancelled. Otherwise, the loop will terminate
		/// when the fiber terminates.
		/// </param>
		/// <param name='updatesPerSecond'>
		/// Updates to all fibers per second. A value of <c>0</c> (the default) will execute fibers
		/// any time they are ready to do work instead of waiting to execute on a specific frequency.
		/// </param>
		/// <param name='token'>
		/// A cancellation token that can be used to stop execution.
		/// </param>
		public virtual void Run(Fiber fiber, CancellationToken token, float updatesPerSecond = 0f)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Run the blocking scheduler loop and perform the specified number of updates per second.
		/// </summary>
		/// <remarks>
		/// Not all schedulers support a blocking run loop that can be invoked by the caller.
		/// </remarks>
		public void Run()
		{
			Run (CancellationToken.None);
		}
		
		/// <summary>
		/// Run the blocking scheduler loop and perform the specified number of updates per second.
		/// </summary>
		/// <remarks>
		/// Not all schedulers support a blocking run loop that can be invoked by the caller.
		/// </remarks>
		/// <param name='updatesPerSecond'>
		/// Updates to all fibers per second. A value of <c>0</c> (the default) will execute fibers
		/// any time they are ready to do work instead of waiting to execute on a specific frequency.
		/// </param>
		/// <param name="token">
		/// A cancellation token that can be used to stop execution.
		/// </param>
		public virtual void Run(CancellationToken token, float updatesPerSecond = 0f)
		{
			Run (null, token, updatesPerSecond);
		}
				
		/// <summary>
		/// Queues the fiber for execution on the scheduler.
		/// </summary>
		/// <remarks>
		/// Fibers queued from the scheduler thread will generally be executed
		/// inline whenever possible on most schedulers. 
		/// </remarks>
		/// <returns>
		/// Returns <c>true</c> if the fiber was executed inline <c>false</c> if it was queued.
		/// </returns>
		/// <param name='fiber'>
		/// The fiber to queue.
		/// </param>
		protected abstract void QueueFiber(Fiber fiber);
		
		/// <summary>
		/// Invoked when an abort has been requested.
		/// </summary>
		/// <remarks>
		/// This call will only arrive from another thread and it's possible
		/// the scheduler may have already dealt with the abort because the
		/// state was already changed to AbortRequested before this method
		/// is fired. Schedulers must handle that condition.
		/// </remarks>
		/// <param name='fiber'>
		/// The fiber to be aborted.
		/// </param>
		protected abstract void AbortRequested(Fiber fiber);
		
		/// <summary>
		/// Executes the fiber until it ends or yields.
		/// </summary>
		/// <remarks>
		/// Custom schedulers will need to invoke this method in order
		/// to actually perform the work of the fiber and cause the correct
		/// state transitions to occur.
		/// </remarks>
		/// <returns>
		/// A fiber instruction to be processed by the scheduler.
		/// </returns>
		/// <param name='fiber'>
		/// The fiber to execute.
		/// </param>
		protected FiberInstruction ExecuteFiber(Fiber fiber)
		{
			return fiber.Execute();
		}
		
		#region IDisposable implementation
		
		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// <see cref="SpicyPixel.Threading.FiberScheduler"/> is reclaimed by garbage collection.
		/// </summary>
		~FiberScheduler()
		{
			Dispose(false);
		}
		
		/// <summary>
		/// Tracks whether the object has been disposed already
		/// </summary>
		private bool isDisposed = false;
		
		/// <summary>
		/// Releases all resource used by the <see cref="SpicyPixel.Threading.FiberScheduler"/> object.
		/// </summary>
		/// <remarks>
		/// Call the method when you are finished using the
		/// <see cref="SpicyPixel.Threading.FiberScheduler"/>. The method leaves the
		/// <see cref="SpicyPixel.Threading.FiberScheduler"/> in an unusable state. After calling
		/// the method, you must release all references to the
		/// <see cref="SpicyPixel.Threading.FiberScheduler"/> so the garbage collector can reclaim the memory that
		/// the <see cref="SpicyPixel.Threading.FiberScheduler"/> was occupying.
		/// </remarks>
		public void Dispose() 
		{			
		 	Dispose(true);
		  	GC.SuppressFinalize(this); 
		}
		
		/// <summary>
		/// Dispose the scheduler.
		/// </summary>
		/// <remarks>
		/// When the scheduler is disposed, the <see cref="CancellationToken"/> is set.
		/// </remarks>
		/// <param name="disposing">
		/// Disposing is true when called manually,
		/// false when called by the finalizer.
		/// </param>
		protected virtual void Dispose(bool disposing) 
		{
			// Do nothing if already called
			if(isDisposed)
				return;
						
			if(disposing) 
			{
				// Free other state (managed objects).
                cancelSource.Cancel();
			}
			
			// Free your own state (unmanaged objects).
			// Set large fields to null.
			
			// Mark disposed
			isDisposed = true;			
		}
		#endregion
	}
}

