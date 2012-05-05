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
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace SpicyPixel.Threading
{
	/// <summary>
	/// A Fiber is a lightweight means of scheduling work that enables multiple
	/// units of processing to execute concurrently by co-operatively
	/// sharing execution time on a single thread. Fibers are also known
	/// as "micro-threads" and can be implemented using programming language
	/// facilities such as "coroutines".
	/// </summary>
	/// <remarks>
	/// <para>
	/// Fibers simplify many concurrency issues generally associated with
	/// multithreading because a given fiber has complete control over
	/// when it yields execution to another fiber. A fiber does not need
	/// to manage resource locking or handle changing data in the same way as a 
	/// thread does because access to a resource is never preempted by 
	/// another fiber without co-operation.
	/// </para>
	/// <para>
	/// Fibers can improve performance in certain applications with concurrency
	/// requirements. Because many fibers can run on a thread, this can relieve 
	/// pressure on precious resources in the thread pool and reduce latency.
	/// Additionally, some applications have concurrent, interdependent processes 
	/// that naturally lend themselves to co-operative scheduling which can
	/// result in greater efficiency when the application manages the context
	/// switch instead of a pre-emptive scheduler.
	/// </para>
	/// <para>
	/// Fibers can also be a convenient way to express a state machine. The 
	/// master fiber implementing the machine can test state conditions, start 
	/// new fibers for state actions, yield to an action fiber until it completes,
	/// and then handle the transition out of the state and into a new state.
	/// </para>
	/// </remarks>
	public class Fiber
	{		
		/// <summary>
		/// The currently executing fiber on the thread.
		/// </summary>
		[ThreadStatic]
		private static Fiber currentFiber;
		
		/// <summary>
		/// This flag gets set when a request is made to reset abort.
		/// </summary>
		[ThreadStatic]
		private static bool resetAbortRequested;
		
		/// <summary>
		/// The next unique identifier.
		/// </summary>
		private static int nextId = 0;
		
		private IEnumerator coroutine;
		
		private Action action;
        private Action<object> actionObject;
        private object objectState;
		
		private Func<FiberInstruction> func;
		private Func<object, FiberInstruction> funcObject;
		
		private FiberScheduler scheduler;
		private int fiberState = (int)FiberState.Unstarted; // must be int to work with Interlocked on Mono
        private IDictionary<string, object> properties;

		private EventHandler<EventArgs> completed;
		private EventHandler<FiberUnhandledExceptionEventArgs> unhandledException;

		/// <summary>
		/// Occurs when the fiber completes with or without exception.
		/// </summary>
		public event EventHandler<EventArgs> Completed
		{
			add { completed += value; }
			remove { completed -= value; }
		}
		
		/// <summary>
		/// Raised when a fiber throws an unhandled exception.
		/// </summary>
		/// <remarks>
		/// By processing the exception and marking <see cref="FiberUnhandledExceptionEventArgs.Handled"/>
		/// as <c>true</c>, the Fiber will still terminate because it is not possible
		/// to recover from an unhandled exception in a coroutine. However, the scheduler
		/// will receive a fiber <see cref="StopInstruction"/> instead of an exception
		/// and will therefore continue to execute other fibers.
		/// </remarks>
		public event EventHandler<FiberUnhandledExceptionEventArgs> UnhandledException
		{
			add { unhandledException += value; }
			remove { unhandledException -= value; }
		}
		
		/// <summary>
		/// Gets the currently executing fiber on this thread.
		/// </summary>
		/// <value>
		/// The currently executing fiber on this thread.
		/// </value>
		public static Fiber CurrentFiber { 
			get {
				return currentFiber;
			}
			private set {
				currentFiber = value;
			}
		}
		
		/// <summary>
		/// Start executing a new fiber using the default scheduler on the thread.
		/// </summary>
		/// <returns>
		/// Returns a <see cref="YieldUntilComplete"/> fiber instruction
		/// that can be yielded against to wait for the fiber to complete.
		/// </returns>
		/// <param name='coroutine'>
		/// A couroutine to execute on the fiber.
		/// </param>
		public static YieldUntilComplete StartNew(IEnumerator coroutine)
		{
			return new Fiber(coroutine).Start();
		}
		
		/// <summary>
		/// Start executing a new fiber using the specified scheduler.
		/// </summary>
		/// <returns>
		/// Returns a <see cref="YieldUntilComplete"/> fiber instruction
		/// that can be yielded against to wait for the fiber to complete.
		/// </returns>
		/// <param name='coroutine'>
		/// A couroutine to execute on the fiber.
		/// </param>
		/// <param name='scheduler'>
		/// A scheduler to execute the fiber on.
		/// </param>
		public static YieldUntilComplete StartNew(IEnumerator coroutine, FiberScheduler scheduler)
		{
			return new Fiber(coroutine).Start(scheduler);
		}
		
		/// <summary>
		/// Start executing a new fiber using the default scheduler on the thread.
		/// </summary>
		/// <returns>
		/// Returns a <see cref="YieldUntilComplete"/> fiber instruction
		/// that can be yielded against to wait for the fiber to complete.
		/// </returns>
		/// <param name='action'>
		/// A non-blocking action to execute on the fiber.
		/// </param>
		public static YieldUntilComplete StartNew(Action action)
		{
			return new Fiber(action).Start();
		}
		
		/// <summary>
		/// Start executing a new fiber using the default scheduler on the thread.
		/// </summary>
		/// <returns>
		/// Returns a <see cref="YieldUntilComplete"/> fiber instruction
		/// that can be yielded against to wait for the fiber to complete.
		/// </returns>
		/// <param name='action'>
		/// A non-blocking action to execute on the fiber.
		/// </param>
		/// <param name='scheduler'>
		/// A scheduler to execute the fiber on.
		/// </param>
		public static YieldUntilComplete StartNew(Action action, FiberScheduler scheduler)
		{
			return new Fiber(action).Start(scheduler);
		}

        /// <summary>
        /// Start executing a new fiber using the default scheduler on the thread.
        /// </summary>
        /// <returns>
        /// Returns a <see cref="YieldUntilComplete"/> fiber instruction
        /// that can be yielded against to wait for the fiber to complete.
        /// </returns>
        /// <param name='action'>
        /// A non-blocking action to execute on the fiber.
        /// </param>
        /// <param name='state'>
        /// State to pass to the action.
        /// </param>
        public static YieldUntilComplete StartNew(Action<object> action, object state)
        {
            return new Fiber(action, state).Start();
        }
        
        /// <summary>
        /// Start executing a new fiber using the default scheduler on the thread.
        /// </summary>
        /// <returns>
        /// Returns a <see cref="YieldUntilComplete"/> fiber instruction
        /// that can be yielded against to wait for the fiber to complete.
        /// </returns>
        /// <param name='action'>
        /// A non-blocking action to execute on the fiber.
        /// </param>
        /// <param name='scheduler'>
        /// A scheduler to execute the fiber on.
        /// </param>
        /// <param name='state'>
        /// State to pass to the action.
        /// </param>
        public static YieldUntilComplete StartNew(Action<object> action, object state, FiberScheduler scheduler)
        {
            return new Fiber(action, state).Start(scheduler);
        }
		
		/// <summary>
		/// Initializes a new instance of the <see cref="SpicyPixel.Threading.Fiber"/> class.
		/// </summary>
		/// <param name='func'>
		/// A non-blocking function that returns a <see cref="FiberInstruction"/> when complete.
		/// </param>
		public static YieldUntilComplete StartNew(Func<FiberInstruction> func)
		{
			return new Fiber(func).Start();
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="SpicyPixel.Threading.Fiber"/> class.
		/// </summary>
		/// <param name='func'>
		/// A non-blocking function that returns a <see cref="FiberInstruction"/> when complete.
		/// </param>
		/// <param name='state'>
		/// State to pass to the function.
		/// </param>
		public static YieldUntilComplete StartNew(Func<object, FiberInstruction> func, object state)
		{
			return new Fiber(func, state).Start();
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="SpicyPixel.Threading.Fiber"/> class.
		/// </summary>
		/// <param name='func'>
		/// A non-blocking function that returns a <see cref="FiberInstruction"/> when complete.
		/// </param>
		/// <param name='scheduler'>
        /// A scheduler to execute the fiber on.
        /// </param>
		public static YieldUntilComplete StartNew(Func<FiberInstruction> func, FiberScheduler scheduler)
		{
			return new Fiber(func).Start(scheduler);
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="SpicyPixel.Threading.Fiber"/> class.
		/// </summary>
		/// <param name='func'>
		/// A non-blocking function that returns a <see cref="FiberInstruction"/> when complete.
		/// </param>
		/// <param name='state'>
		/// State to pass to the function.
		/// </param>
		/// <param name='scheduler'>
        /// A scheduler to execute the fiber on.
        /// </param>
		public static YieldUntilComplete StartNew(Func<object, FiberInstruction> func, object state, FiberScheduler scheduler)
		{
			return new Fiber(func, state).Start(scheduler);
		}

        /// <summary>
        /// Gets user-defined properties associated with the fiber.
        /// </summary>
        /// <remarks>
        /// Similar to thread local storage, callers may associate
        /// data with a fiber. A FiberStorage&lt;T&gt; class
        /// could retrieve data from the this property collection
        /// on the <see cref="CurrentFiber"/>.
        /// 
        /// Schedulers may also use this storage to associate
        /// additional data needed to perform scheduling operations.
        /// </remarks>
        /// <value>
        /// The properties.
        /// </value>
        public IDictionary<string, object> Properties
        {
            get { 
                if(properties == null)
                    properties = new Dictionary<string, object>();

                return properties; 
            }
        }
		
		/// <summary>
		/// Gets the scheduler used to start the fiber.
		/// </summary>
		/// <value>
		/// The scheduler that was used to start the fiber.
		/// </value>
		internal FiberScheduler Scheduler { 
			get { return scheduler; } 
			set { scheduler = value; }
		}
		
		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		/// <value>
		/// The name.
		/// </value>
		public string Name { get; set; }
		
		/// <summary>
		/// Gets a value indicating whether this instance is alive (running).
		/// </summary>
		/// <value>
		/// <c>true</c> if this instance is alive; otherwise, <c>false</c>.
		/// </value>
		public bool IsAlive {
			get {
				return FiberState != FiberState.Unstarted && FiberState != FiberState.Stopped;
			}
		}
		
		/// <summary>
		/// Gets or sets the state of the fiber.
		/// </summary>
		/// <value>
		/// The state of the fiber (Unstarted, Running, Stopped).
		/// </value>
		public FiberState FiberState 
		{ 
			get { return (FiberState)fiberState; }
			set { fiberState = (int)value; }
		}
		
		/// <summary>
		/// Raises the completed event.
		/// </summary>
		private void RaiseCompleted()
		{
			var completedCopy = completed; // copy for null check thread safety
			if(completedCopy != null)
				completedCopy(this, EventArgs.Empty);
		}
		
		/// <summary>
		/// Gets the thread unique identifier for the fiber.
		/// </summary>
		/// <value>
		/// The identifier.
		/// </value>
		public int Id { get; private set; }	

		/// <summary>
		/// Initializes a new instance of the <see cref="SpicyPixel.Threading.Fiber"/> class.
		/// </summary>
		/// <param name='coroutine'>
		/// A couroutine to execute on the fiber.
		/// </param>
		public Fiber (IEnumerator coroutine)
		{			
			Id = nextId++;
			this.coroutine = coroutine;
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="SpicyPixel.Threading.Fiber"/> class.
		/// </summary>
		/// <param name='action'>
		/// A non-blocking action to execute on the fiber.
		/// </param>
		public Fiber (Action action)
		{
			Id = nextId++;
			this.action = action;
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.Fiber"/> class.
        /// </summary>
        /// <param name='action'>
        /// A non-blocking action to execute on the fiber.
        /// </param>
        /// <param name='state'>
        /// State to pass to the action.
        /// </param>
        public Fiber (Action<object> action, object state)
        {
            Id = nextId++;
            this.actionObject = action;
            this.objectState = state;
        }
		
		/// <summary>
		/// Initializes a new instance of the <see cref="SpicyPixel.Threading.Fiber"/> class.
		/// </summary>
		/// <param name='func'>
		/// A non-blocking function that returns a <see cref="FiberInstruction"/> when complete.
		/// </param>
		public Fiber(Func<FiberInstruction> func)
		{
			Id = nextId++;
			this.func = func;
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="SpicyPixel.Threading.Fiber"/> class.
		/// </summary>
		/// <param name='func'>
		/// A non-blocking function that returns a <see cref="FiberInstruction"/> when complete.
		/// </param>
		/// <param name='state'>
		/// State to pass to the function.
		/// </param>
		public Fiber(Func<object, FiberInstruction> func, object state)
		{
			Id = nextId++;
			this.funcObject = func;
			this.objectState = state;
		}
		
		/// <summary>
		/// Start executing the fiber using the default scheduler on the thread.
		/// </summary>
		/// <returns>
		/// Returns a <see cref="YieldUntilComplete"/> fiber instruction
		/// that can be yielded against to wait for the fiber to complete.
		/// </returns>
		public YieldUntilComplete Start()
		{
			return Start(FiberScheduler.Current);
		}
				
		/// <summary>
		/// Start executing the fiber using the specified scheduler.
		/// </summary>
		/// <remarks>
		/// This method is safe to call from any thread even if different
		/// than the scheduler execution thread.
		/// </remarks>
		/// <returns>
		/// Returns a <see cref="YieldUntilComplete"/> fiber instruction
		/// that can be yielded against to wait for the fiber to complete.
		/// </returns>
		/// <param name='scheduler'>
		/// The scheduler to start the fiber on.
		/// </param>
		public YieldUntilComplete Start(FiberScheduler scheduler)
		{	
			// It would be unusual to attempt to start a Fiber more than once,
			// but to be safe and to support calling from any thread
			// use Interlocked with boxing on the enum.
			
			var originalState = (FiberState)Interlocked.CompareExchange(ref fiberState, (int)FiberState.Running, (int)FiberState.Unstarted);
				
			if(originalState != FiberState.Unstarted)
				throw new InvalidOperationException("A fiber cannot be started again once it has begun running or has completed.");
			
			this.scheduler = scheduler;
			((IFiberScheduler)this.scheduler).QueueFiber(this);
			return new YieldUntilComplete(this);
		}
		
		/// <summary>
		/// Request abort of this fiber instance.
		/// </summary>
		/// <remarks>
		/// This method is safe to call from any thread.
		/// </remarks>
		public void Abort()
		{				
			// If fiber matches then throw immediately (this is safe because
			// the fiber is thread static and will only match if Abort() was 
			// called from the scheduler thread during execution).
			if(CurrentFiber == this)
			{
				// If the scheduler is implemented correctly, it should not be possible
				// to enter this section of code without state being Running.
				if(FiberState == FiberState.Unstarted || FiberState == FiberState.Running)
				{
					fiberState = (int)FiberState.AbortRequested;
					throw new FiberAbortException();
				}
			}
			else
			{			
				// Abort is called from a different thread than
				// the scheduler. 
				
				// Request abort if unstarted or running, otherwise there is nothing
				// special to do (fiber is already processing an abort or is stopped). 
				Interlocked.CompareExchange(ref fiberState, (int)FiberState.AbortRequested, (int)FiberState.Unstarted);
				Interlocked.CompareExchange(ref fiberState, (int)FiberState.AbortRequested, (int)FiberState.Running);

				// Inform the scheduler an abort was requested (in case it was sleeping).
				((IFiberScheduler)this.scheduler).AbortRequested(this);
			}
		}
		
		/// <summary>
		/// Resets an abort request to allow continued execution.
		/// </summary>
		/// <remarks>
		/// Any thread or fiber may request an abort. During the next
		/// update of the fiber, a FiberAbortException will be thrown. If a handler was set by
		/// <see cref="UnhandledException"/>, it may call <see cref="ResetAbort"/> in order
		/// to ignore the reset and continue execution.
		/// 
		/// This method only modifies thread local properties and therefore is
		/// guaranteed to be threadsafe.
		/// </remarks>
		public static void ResetAbort()
		{
			Fiber currentFiber = Fiber.CurrentFiber;
			
			if(currentFiber == null || (currentFiber != null && currentFiber.FiberState != FiberState.AbortRequested))
				throw new InvalidOperationException("ResetAbort() can only be called inside an exception handler after a fiber has been aborted.");
			
			resetAbortRequested = true;			
		}
				
		/// <summary>
		/// Executes the fiber until it ends or yields.
		/// </summary>
		/// <returns>
		/// A fiber instruction to be processed by the scheduler.
		/// </returns>
		internal FiberInstruction Execute()
		{
			// Sanity check the scheduler. Since this is a scheduler
			// only issue, this test happens first before execution
			// and before allowing an exception handler to take over.
			if(FiberState == FiberState.Stopped)
				throw new InvalidOperationException("An attempt was made to execute a stopped Fiber. This indicates a logic error in the scheduler.");

			// Setup thread globals with this scheduler as the owner.
			// The sync context is also setup when the scheduler changes.
			// Setting the sync context isn't a simple assign in .NET
			// so don't do this until needed.
			//
			// Doing this setup in Execute() frees derived schedulers from the 
			// burden of setting up the sync context or scheduler. It also allows the
			// current scheduler to change on demand when there is more than one
			// scheduler running per thread. 
			// 
			// For example, each MonoBehaviour in Unity is assigned its own
			// scheduler since the behavior determines the lifetime of tasks.
			// The active scheduler then can vary depending on which behavior is 
			// currently executing tasks. Unity will decide outside of this framework 
			// which behaviour to execute in which order and therefore which 
			// scheduler is active. The active scheduler in this case can 
			// only be determined at the time of fiber execution.
			//
			// Unlike CurrentFiber below, this does not need to be stacked
			// once set because the scheduler will never change during fiber
			// execution. Only something outside of the scheduler would
			// change the scheduler.
			if(FiberScheduler.Current != scheduler)
				FiberScheduler.SetCurrentScheduler(scheduler, true);

			// Push the current fiber onto the stack and pop it in finally.
			// This must be stacked because the fiber may spawn another
			// fiber which the scheduler may choose to inline which would result
			// in a new fiber temporarily taking its place as current.
			var lastFiber = Fiber.CurrentFiber;
			Fiber.CurrentFiber = this;
			
			try
			{	
				// Process an abort if pending
				if(FiberState == FiberState.AbortRequested)
					throw new FiberAbortException();

				object result;
				
				// Execute the coroutine or action
				if(coroutine != null)
				{
					// Execute coroutine
					if(coroutine.MoveNext())
					{
						// Get result of execution
						result = coroutine.Current;
					}
					else
					{
						// Coroutine finished executing
						result = Stop();
					}
				}
				else if(action != null)
				{
					// Execute action
                    action();
					
					// Action finished executing
					result = Stop();
				}
                else if(actionObject != null)
                {
                    // Execute action
                    actionObject(objectState);
                    
                    // Action finished executing
                    result = Stop();
                }
				else if(func != null)
				{
					result = func();
					func = null;
				}
				else if(funcObject != null)
				{
					result = funcObject(objectState);
					funcObject = null;
				}
				else
				{
					// Func execution nulls out the function
					// so the scheduler will return to here
					// when complete and then stop.
					result = Stop ();
				}
				
				// Treat null as a special case
				if(result == null)
					return FiberInstruction.YieldToAnyFiber;

				// Return instructions or throw if invalid
				var instruction = result as FiberInstruction;
				if(instruction == null)
                    return new ObjectInstruction(result);
				else
				{
					// Verify same scheduler	
					if(instruction is YieldUntilComplete && ((YieldUntilComplete)instruction).Fiber.Scheduler != FiberScheduler.Current)
						throw new InvalidOperationException("Currently only fibers belonging to the same scheduler may be yielded to. FiberScheduler.Current = " 
                                            + (FiberScheduler.Current == null ? "null" : FiberScheduler.Current.ToString()) 
                                            + ", Fiber.Scheduler = " + (((YieldUntilComplete)instruction).Fiber.Scheduler == null ? "null" : ((YieldUntilComplete)instruction).Fiber.Scheduler.ToString()));
					
					var yieldToFiberInstruction = instruction as YieldToFiber;									
					if(yieldToFiberInstruction != null)
					{
						// Start fibers yielded to that aren't running yet
						var originalState = (FiberState)Interlocked.CompareExchange(ref yieldToFiberInstruction.Fiber.fiberState, (int)FiberState.Running, (int)FiberState.Unstarted);				
						if(originalState == FiberState.Unstarted)			
							yieldToFiberInstruction.Fiber.scheduler = scheduler;
					
						// Can't switch to stopped fibers
						if(yieldToFiberInstruction.Fiber.FiberState == FiberState.Stopped)
							throw new InvalidOperationException("An attempt was made to yield to a stopped fiber.");
						
						// Verify scheduler
						if(yieldToFiberInstruction.Fiber.Scheduler != FiberScheduler.Current)
							throw new InvalidOperationException("Currently only fibers belonging to the same scheduler may be yielded to. FiberScheduler.Current = " 
                                            + (FiberScheduler.Current == null ? "null" : FiberScheduler.Current.ToString()) 
                                            + ", Fiber.Scheduler = " + (yieldToFiberInstruction.Fiber.Scheduler == null ? "null" : yieldToFiberInstruction.Fiber.Scheduler.ToString()));
					}
					
					return instruction;
				}
			}
			catch(Exception fiberException)
			{
				// If an exception occurs allow the handler to run.
				// Fiber execution will terminate even with a handler
				// because it's not possible to resume an action or a 
				// coroutine that throws an exception.
				//
				// The only exception that could result in a contiunation
				// is FiberAbortException. Handlers may choose to deny
				// the abort by calling Fiber.ResetAbort().
				try
				{					
					// Invoke the exception handler (which could throw)
					var unhandledException = this.unhandledException;
					var eventArgs = new FiberUnhandledExceptionEventArgs(this, fiberException);
					if(unhandledException != null)
					{
						foreach(var subscriber in unhandledException.GetInvocationList())
						{
							subscriber.DynamicInvoke(this, eventArgs);
							if(eventArgs.Handled)
								break;
						}
					}
					
					// See if an abort was requested. The flag is thread safe because
					// it is only ever touched by the scheduler thread.
					if(resetAbortRequested) 
					{
						resetAbortRequested = false;
						fiberState = (int)FiberState.Running;
						return FiberInstruction.YieldToAnyFiber; // signal the scheduler to continue
					}
					else
					{
						if(!eventArgs.Handled)
							throw fiberException;
						else
							return Stop ();
					}
				}
				catch(Exception fiberOrHandlerException)
				{					
					// The exception wasn't from an abort or it wasn't reset
					// and so the exception needs to happen.
				
					// Clear reset requests (if any). This is needed here in case
					// the exceptionHandler above threw after ResetAbort() was
					// called.
					//
					// The flag is thread safe because
					// it is only ever touched by the scheduler thread.
					resetAbortRequested = false;

					// Stop the fiber. This will invoke completion handlers
					// but they won't know this fiber failed due to an exception
					// unless they handled the exception above or wait for
					// the scheduler to do something after the throw below.
					Stop();
					
					// Throw the exception back to the scheduler.
					throw fiberOrHandlerException;
				}
			}
			finally
			{	
				// Pop the current fiber
				Fiber.CurrentFiber = lastFiber;
			}
		}
					
		private StopInstruction Stop()
		{
			// Interlocked isn't necessary because we don't need
			// the initial value (stops are final). They are also
			// only called by the scheduler thread during Execute().
			fiberState = (int)FiberState.Stopped;
			RaiseCompleted();
			return FiberInstruction.Stop;
		}
	}
}