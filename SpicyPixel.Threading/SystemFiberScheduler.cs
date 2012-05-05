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
using System.Collections.Concurrent;
using System.Threading;

namespace SpicyPixel.Threading
{		
	/// <summary>
	/// This class is the system default implementation of a <see cref="FiberScheduler"/>
	/// and is capable of scheduling and executing fibers on the current thread.
	/// </summary>
	/// <remarks>
	/// Although no fibers execute after the scheduler is shutdown, none of the
	/// fibers are transitioned to a <see cref="FiberState.Stopped"/> state and therefore
	/// it's not safe for a fiber to wait on a fiber outside of its own scheduler.
	/// This is currently enforced by <see cref="FiberScheduler.ExecuteFiber"/> for now although it would be
	/// possible to support fiber waits across schedulers in the future.
	/// </remarks>
	public class SystemFiberScheduler : FiberScheduler
	{
		/// <summary>
		/// Starts a new thread, creates a scheduler, starts it running, and returns it to the calling thread.
		/// </summary>
		/// <returns>
		/// The scheduler from the spawned thread.
		/// </returns>
		public static SystemFiberScheduler StartNew()
		{
			return StartNew(null, CancellationToken.None, 0f);
		}
		
		/// <summary>
		/// Starts a new thread, creates a scheduler, starts it running, and returns it to the calling thread.
		/// </summary>
		/// <returns>
		/// The scheduler from the spawned thread.
		/// </returns>
		/// <param name='token'>
		/// A token to cancel the thread.
		/// </param>
		/// <param name='updatesPerSecond'>
		/// Updates to run per second.
		/// </param>
		public static SystemFiberScheduler StartNew(CancellationToken token, float updatesPerSecond = 0f)
		{
			return StartNew(null, token, updatesPerSecond);
		}
		
		/// <summary>
		/// Starts a new thread, creates a scheduler, starts it running, and returns it to the calling thread.
		/// </summary>
		/// <returns>
		/// The scheduler from the spawned thread.
		/// </returns>
		/// <param name='fiber'>
		/// A fiber to start execution from.
		/// </param>
		public static SystemFiberScheduler StartNew(Fiber fiber)
		{
			return StartNew(fiber, CancellationToken.None, 0f);
		}
		
		/// <summary>
		/// Starts a new thread, creates a scheduler, starts it running, and returns it to the calling thread.
		/// </summary>
		/// <returns>
		/// The scheduler from the spawned thread.
		/// </returns>
		/// <param name='fiber'>
		/// A fiber to start execution from.
		/// </param>
		/// <param name='updatesPerSecond'>
		/// Updates to run per second.
		/// </param>
		/// <param name='token'>
		/// A token to cancel the thread.
		/// </param>
		public static SystemFiberScheduler StartNew(Fiber fiber, CancellationToken token, float updatesPerSecond = 0f)
		{
			SystemFiberScheduler backgroundScheduler = null;
			
			// Setup a thread to run the scheduler
			var wait = new ManualResetEvent(false);
			var thread = new Thread(() => {
				backgroundScheduler = (SystemFiberScheduler)FiberScheduler.Current;
				wait.Set();
				FiberScheduler.Current.Run(fiber, token, updatesPerSecond);
			});
			thread.Start();
			wait.WaitOne();
			
			return backgroundScheduler;
		}
		
		/// <summary>
		/// The max stack depth before breaking out of recursion.
		/// </summary>
		private const int MaxStackDepth = 10;
		
		/// <summary>
		/// Tracks the number of times QueueFiber is called to avoid
		/// inlining too much
		/// </summary>
		[ThreadStatic]
		private static int stackDepthQueueFiber = 0;

		/// <summary>
		/// Currently executing fibers
		/// </summary>
		private ConcurrentQueue<Fiber> executingFibers = new ConcurrentQueue<Fiber>();
		
		/// <summary>
		/// Fibers sleeping until a timeout
		/// </summary>
		private ConcurrentQueue<Tuple<Fiber, float>> sleepingFibers = new ConcurrentQueue<Tuple<Fiber, float>>();
				
		// A future queue may include waitingFibers (e.g. waiting on a signal or timeout)
		
		/// <summary>
		/// The current time since the start of the run loop.
		/// </summary>
		private float currentTime;
		
		/// <summary>
		/// The run wait handle is set when not running and used to coordinate Dispose and Run
		/// </summary>
		private ManualResetEvent runWaitHandle = new ManualResetEvent(true);
			
		/// <summary>
		/// Set when disposed
		/// </summary>
		private ManualResetEvent disposeWaitHandle = new ManualResetEvent(false);
		
		/// <summary>
		/// Run() waits on this handle when it has nothing to do.
		/// Signaling wakes up the thread.
		/// </summary>
		private AutoResetEvent schedulerEventWaitHandle = new AutoResetEvent(false);
		
		/// <summary>
		/// Gets the run wait handle.
		/// </summary>
		/// <remarks>
		/// The run wait handle is set when not running and used to coordinate Dispose and Run.
		/// </remarks>
		/// <value>
		/// The run wait handle.
		/// </value>
		protected ManualResetEvent RunWaitHandle
		{
			get { return runWaitHandle; }
		}
		
		/// <summary>
		/// Gets the dispose wait handle.
		/// </summary>
		/// <remarks>
		/// Run monitors this handle for a Dispose to know when to terminate.
		/// </remarks>
		/// <value>
		/// The dispose wait handle.
		/// </value>
		protected WaitHandle DisposeWaitHandle
		{
			get { return disposeWaitHandle; }
		}
		
		/// <summary>
		/// Gets a wait handle which can be used to wait for a scheduler
		/// event to occur.
		/// </summary>
		/// <remarks>
		/// Scheduler events trigger this handle to be signaled.
	 	/// Events occur any time a fiber is added to an execution queue,
		/// whether because it is new or because it is moving from
		/// a wait queue to an execution queue.
		/// 
		/// This handle is used to sleep in Run() when there are
		/// no events to process, and a scheduler with a custom
		/// run loop may do the same.
		/// </remarks>
		/// <value>
		/// The scheduler event wait handle.
		/// </value>
		protected WaitHandle SchedulerEventWaitHandle
		{
			get { return schedulerEventWaitHandle; }
		}
		
		/// <summary>
		/// Gets the executing fiber count.
		/// </summary>
		/// <remarks>
		/// This can be used to optimize a custom run loop.
		/// </remarks>
		/// <value>
		/// The executing fiber count.
		/// </value>
		protected int ExecutingFiberCount
		{
			get { return executingFibers.Count; }
		}
		
		/// <summary>
		/// Gets the sleeping fiber count.
		/// </summary>
		/// <remarks>
		/// This can be used to optimize a custom run loop.
		/// </remarks>
		/// <value>
		/// The sleeping fiber count.
		/// </value>
		protected int SleepingFiberCount
		{
			get { return sleepingFibers.Count; }
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="SpicyPixel.Threading.FiberScheduler"/> class.
		/// </summary>
		public SystemFiberScheduler ()
		{
		}
				
		/// <summary>
		/// Queues the fiber for execution on the scheduler.
		/// </summary>
		/// <remarks>
		/// Fibers queued from the scheduler thread will generally be executed
		/// inline whenever possible on most schedulers.
		/// </remarks>
		/// <param name='fiber'>
		/// The fiber to queue.
		/// </param>
		protected sealed override void QueueFiber(Fiber fiber)
		{
			// Queueing can happen from completion callbacks
			// which may happen once the fiber has already
			// executed and changed state. It would be fine
			// if the queue did happen because non-running
			// fibers are skipped, but it's better to
			// shortcut here.
			if(fiber.FiberState != FiberState.Running)
				return;
				
			// Entering queue fiber where recursion might matter
			Interlocked.Increment(ref stackDepthQueueFiber);
			
			try
			{				
				// Execute immediately to inline as much as possible
				//
				// Note: Some applications may want to always queue to control
				// performance more strictly by the run loop.
				if(AllowInlining && SchedulerThread == Thread.CurrentThread && stackDepthQueueFiber < MaxStackDepth)
				{
					ExecuteFiberInternal(fiber);
					return;
				}
				else 
				{
					QueueFiberForExecution(fiber);
					return;
				}
			}
			finally
			{
				// Exiting queue fiber
				Interlocked.Decrement(ref stackDepthQueueFiber);
			}
		}

		/// <summary>
		/// Adds a fiber to the execution queue without inlining and sets the wait handle.
		/// </summary>
		/// <param name='fiber'>
		/// The fiber to queue.
		/// </param>
		private void QueueFiberForExecution(Fiber fiber)
		{
			executingFibers.Enqueue(fiber);
			
			// Queueing a new execution fiber needs to trigger re-evaluation of the
			// next update time
			schedulerEventWaitHandle.Set();
		}
		
		/// <summary>
		/// Adds a fiber to the sleep queue and sets the wait handle.
		/// </summary>
		/// <param name='fiber'>
		/// The fiber to queue.
		/// </param>
		/// <param name='timeToWake'>
		/// The future time to wake.
		/// </param>
		private void QueueFiberForSleep(Fiber fiber, float timeToWake)
		{
			var tuple = new Tuple<Fiber, float>(fiber, timeToWake);
			sleepingFibers.Enqueue(tuple);
				
			// Fibers can only be queued for sleep when they return
			// a yield instruction. This can only happen when executing
			// on the main thread and therefore we will never be in
			// a wait loop with a need to signal the scheduler event handle.
		}
		
		/// <summary>
		/// Update the scheduler which causes all queued tasks to run
		/// for a cycle.
		/// </summary>
		/// <remarks>
		/// This method is useful when updating the scheduler manually
		/// with a custom run loop instead of calling <see cref="Run()"/>.
		/// </remarks>
		/// <param name='time'>
		/// Time in seconds since the scheduler or application began running.
		/// This value is used to determine when to wake sleeping fibers.
		/// Using float instead of TimeSpan spares the GC.
		/// </param>
		protected void Update(float time)
		{
			currentTime = time;

			UpdateExecutingFibers();
			UpdateSleepingFibers();
		}
		
		private void UpdateExecutingFibers()
		{
			////////////////////////////
			// Run executing fibers
			
			// Add null to know when to stop
			executingFibers.Enqueue(null);
			
			Fiber item;
			while(executingFibers.TryDequeue(out item))
			{
				// If we reached the marker for this update then stop
				if(item == null)
					break;
				
				// Skip items that have been aborted or completed
				if(item.FiberState == FiberState.Stopped)
					continue;
				
				ExecuteFiberInternal(item);
			}
		}
		
		private void UpdateSleepingFibers()
		{
			////////////////////////////
			// Wake sleeping fibers that it's time for
		
			// Add null to know when to stop
			sleepingFibers.Enqueue(null);
			
			Tuple<Fiber, float> item;
			while(sleepingFibers.TryDequeue(out item))
			{
				// If we reached the marker for this update then stop
				if(item == null)
					break;
				
				Fiber fiber = item.Item1;
				
				// Skip items that have been aborted or completed
				if(fiber.FiberState == FiberState.Stopped)
					continue;
				
				// Run if time otherwise re-enqueue
				if(item.Item2 <= currentTime || fiber.FiberState == FiberState.AbortRequested)
					ExecuteFiberInternal(item.Item1);
				else
					sleepingFibers.Enqueue(item);
			}
		}
		
		/// <summary>
		/// Invoked when an abort has been requested.
		/// </summary>
		/// <param name='fiber'>
		/// The fiber to be aborted.
		/// </param>
		protected sealed override void AbortRequested(Fiber fiber)
		{
			schedulerEventWaitHandle.Set();
		}
						
		/// <summary>
		/// Gets the time of the first fiber wake up.
		/// </summary>
		/// <remarks>
		/// This method is primarily useful when manually calling Update()
		/// instead of Run() to know how long the thread can sleep for.
		/// </remarks>
		/// <returns>
		/// True if there was a sleeping fiber, false otherwise.
		/// </returns>
		/// <param name='fiberWakeTime'>
		/// The time marker in seconds the first sleeping fiber needs to wake up.
		/// This is based on a previously passed time value to Update().
		/// This value may be 0 if a sleeping fiber was aborted and
		/// therefore an update should process immediately.
		/// </param>
		protected bool GetNextFiberWakeTime(out float fiberWakeTime)
		{
			fiberWakeTime = -1f;
			
			// Nothig to do if there are no sleeping fibers
			if(sleepingFibers.Count == 0)
				return false;

			// Find the earliest wake time
			foreach(var fiber in sleepingFibers)
			{
				if(fiber.Item1.FiberState == FiberState.AbortRequested)
				{
					fiberWakeTime = 0f; // wake immediately
					break;
				}
				
				if(fiberWakeTime == -1f || fiber.Item2 < fiberWakeTime)
					fiberWakeTime = fiber.Item2;
			}
			
			return true;
		}
		
		private IEnumerator CancelWhenComplete(YieldUntilComplete waitOnFiber, CancellationTokenSource cancelSource)
		{
			yield return waitOnFiber;
			cancelSource.Cancel();
		}
				
		/// <summary>
		/// Run the blocking scheduler loop and perform the specified number of updates per second.
		/// </summary>
		/// <remarks>
		/// Not all schedulers support a blocking run loop that can be invoked by the caller.
		/// The system scheduler is designed so that a custom run loop could be implemented
		/// by a derived type. Everything used to execute Run() is available to a derived scheduler.
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
		public override void Run(Fiber fiber, CancellationToken token, float updatesPerSecond)
		{
			long frequencyTicks = (long)(updatesPerSecond * (float)TimeSpan.TicksPerSecond); // min time between updates (duration)
			long startTicks = 0; 		// start of update time (marker)
			long endTicks = 0; 			// end of update time (marker)
			long sleepTicks; 			// time to sleep (duration)
			long wakeTicks;				// ticks before wake (duration)
			int sleepMilliseconds;		// ms to sleep (duration)
			int wakeMilliseconds;		// ms before wake (duration)
			float wakeMarkerInSeconds;	// time of wake in seconds (marker)
			var mainFiberCompleteCancelSource = new CancellationTokenSource();
						
			if(isDisposed)
				throw new ObjectDisposedException(GetType().FullName);
			
			// Run is not re-entrant, make sure we're not running
			if(!runWaitHandle.WaitOne(0))
				throw new InvalidOperationException("Run is already executing and is not re-entrant");
			
			// Verify arguments
			if(updatesPerSecond < 0f)
				throw new ArgumentOutOfRangeException("updatesPerSecond", "The updatesPerSecond must be >= 0");
			
			// Get a base time for better precision
			long baseTicks = DateTime.Now.Ticks;

			// Build wait list to terminate execution
			var waitHandleList = new List<WaitHandle>(4);
			waitHandleList.Add(schedulerEventWaitHandle);
			waitHandleList.Add(disposeWaitHandle);
			
			if(token.CanBeCanceled)
				waitHandleList.Add(token.WaitHandle);
						
			try
			{
				if(fiber != null)
				{				
					// Add the main fiber to the wait list so when it completes
					// the wait handle falls through.
					waitHandleList.Add(mainFiberCompleteCancelSource.Token.WaitHandle);

					// Start the main fiber if it isn't running yet
					YieldUntilComplete waitOnFiber;				
					if(fiber.FiberState == FiberState.Unstarted)
						waitOnFiber = fiber.Start(this);
					else
						waitOnFiber = new YieldUntilComplete(fiber);
					
					// Start another fiber that waits on the main fiber to complete.
					// When it does, it raises a cancellation.
					Fiber.StartNew(CancelWhenComplete(waitOnFiber, mainFiberCompleteCancelSource), this);
				}
				
				WaitHandle[] waitHandles = waitHandleList.ToArray();
				waitHandleList.Remove(schedulerEventWaitHandle);
				WaitHandle[] sleepWaitHandles = waitHandleList.ToArray();
				
				runWaitHandle.Reset();
				
				while(true)
				{					
					// Stop executing if cancelled
					if((token.CanBeCanceled && token.IsCancellationRequested) || mainFiberCompleteCancelSource.IsCancellationRequested || disposeWaitHandle.WaitOne(0))
						return;
					
					// Snap current time
					startTicks = DateTime.Now.Ticks;
	
					// Update using this time marker (and convert ticks to s)
					Update((float)((double)(startTicks - baseTicks) / (double)TimeSpan.TicksPerSecond));
	
					// Only sleep to next frequency cycle if one was specified
					if(updatesPerSecond > 0f)
					{						
						// Snap end time
						endTicks = DateTime.Now.Ticks;
						
						// Sleep at least until next update
						sleepTicks = frequencyTicks - (endTicks - startTicks);
						if(sleepTicks > 0)
						{
							sleepMilliseconds = (int)(sleepTicks / TimeSpan.TicksPerMillisecond);
							
							WaitHandle.WaitAny(sleepWaitHandles, sleepMilliseconds);
							
							// Stop executing if cancelled
							if((token.CanBeCanceled && token.IsCancellationRequested) || mainFiberCompleteCancelSource.IsCancellationRequested || disposeWaitHandle.WaitOne(0))
								return;
						}
					}
									
					// Now keep sleeping until it's time to update
					while(ExecutingFiberCount == 0)
					{						
						// Assume we wait forever (e.g. until a signal)
						wakeMilliseconds = -1;
	
						// If there are sleeping fibers, then set a wake time
						if(GetNextFiberWakeTime(out wakeMarkerInSeconds))
						{
							wakeTicks = baseTicks;
							wakeTicks += (long)((double)wakeMarkerInSeconds * (double)TimeSpan.TicksPerSecond);						
							wakeTicks -= DateTime.Now.Ticks;
								
							// If there was a waiting fiber and it's already past time to awake then stop waiting
							if(wakeTicks <= 0)
								break;
							
							wakeMilliseconds = (int)(wakeTicks / TimeSpan.TicksPerMillisecond);
						}
												
						// There was no waiting fiber and we will wait for another signal,
						// or there was a waiting fiber and we wait until that time.
						WaitHandle.WaitAny(waitHandles, wakeMilliseconds);
						
						// Stop executing if cancelled
						if((token.CanBeCanceled && token.IsCancellationRequested) || mainFiberCompleteCancelSource.IsCancellationRequested || disposeWaitHandle.WaitOne(0))
							return;
					}
				}	
			}
			finally
			{					
				// Clear queues
				Fiber deqeueFiber;
				while(executingFibers.TryDequeue(out deqeueFiber));
				
				Tuple<Fiber, float> dequeueSleepingFiber;
				while(sleepingFibers.TryDequeue(out dequeueSleepingFiber));
				
				// Reset time
				currentTime = 0f;
				
				// Set for dispose
				runWaitHandle.Set();
			}
		}
						
		/// <summary>
		/// Executes the fiber.
		/// </summary>
		/// <remarks>
		/// Fibers executed by this method do not belong to a queue
		/// and must be added to one by method end if the fiber
	    /// execution did not complete this invocation. Otherwise,
		/// the fiber would fall off the scheduler.
		/// </remarks>
		/// <param name='fiber'>
		/// The unqueued fiber to execute.
		/// </param>
		private void ExecuteFiberInternal(Fiber fiber)
		{
			Fiber currentFiber = fiber;
			try
			{
				Fiber nextFiber;
				while(currentFiber != null)
				{
					// Execute the fiber
					var fiberInstruction = ExecuteFiber(currentFiber);
		
					// Nothing more to do if stopped
					if(!currentFiber.IsAlive)
						return;
		
					// Handle special fiber instructions or queue for another update
					bool fiberQueued = false;
					OnFiberInstruction(currentFiber, fiberInstruction, out fiberQueued, out nextFiber);
					
					// If the fiber is still running but wasn't added to a special queue by
					// an instruction then it needs to be added to the execution queue
					// to run in the next Update().
					//
					// Check alive state again in case an instruction resulted
					// in an inline execution and altered state.
					if(!fiberQueued && currentFiber.IsAlive) {
						// Send the fiber to the queue and don't execute inline
						// since we're done this update
						QueueFiberForExecution(currentFiber);
					}
					
					// Switch to the next fiber if an instruction says to do so
					currentFiber = nextFiber;
				}
			}
			catch(Exception ex)
			{
				// Although this exception must result in the fiber
				// being terminated, it does not have to result in the
				// scheduler being brought down unless the exception
				// handler rethrows the exception
				if(!OnUnhandledException(currentFiber, ex))
					throw ex;
			}
		}
		
		private void OnFiberInstruction(Fiber fiber, FiberInstruction instruction, out bool fiberQueued, out Fiber nextFiber)
		{
			fiberQueued = false;
			nextFiber = null;
			
			YieldUntilComplete yieldUntilComplete = instruction as YieldUntilComplete;
			if(yieldUntilComplete != null)
			{
				// The additional complexity below is because this was going
				// to handle waiting for completions for fibers from other threads.
				// Currently fibers must belong to the same thread and this is enforced
				// by the instructions themselves for now.
				
				int completeOnce = 0;
				
				// FIXME: If we support multiple schedulers in the future
				// this callback could occur from another thread and
				// therefore after Dispose(). Would probably need a lock.
				
				// Watch for completion
				EventHandler<EventArgs> completed;
				completed = (sender, e) => 
				{
					var originalCompleteOnce = Interlocked.CompareExchange(ref completeOnce, 1, 0);
					if(originalCompleteOnce != 0)
						return;
					
					yieldUntilComplete.Fiber.Completed -= completed;
					//QueueFiberForExecution(fiber);
					QueueFiber(fiber); // optionally execute inline when the completion occurs
				};
				yieldUntilComplete.Fiber.Completed += completed;
				
				// If the watched fiber is already complete then continue immediately
				if(yieldUntilComplete.Fiber.FiberState == FiberState.Stopped)
					completed(yieldUntilComplete.Fiber, EventArgs.Empty);

				fiberQueued = true;
				return;
			}
			
			YieldForSeconds yieldForSeconds = instruction as YieldForSeconds;
			if(yieldForSeconds != null)
			{
				QueueFiberForSleep(fiber, currentTime + yieldForSeconds.Seconds);
				fiberQueued = true;
				return;
			}
			
			YieldToFiber yieldToFiber = instruction as YieldToFiber;
			if(yieldToFiber != null)
			{
				RemoveFiberFromQueues(yieldToFiber.Fiber);
				nextFiber = yieldToFiber.Fiber;
				fiberQueued = false;
				return;
			}
		}
		
		/// <summary>
		/// Removes a fiber from the current queues.
		/// </summary>
		/// <remarks>
		/// The fiber being yielded to needs to be removed from the queues
		/// because it's about to be processed directly.
		/// </remarks>
		/// <param name='fiber'>
		/// Fiber.
		/// </param>
		private void RemoveFiberFromQueues(Fiber fiber)
		{
			bool found = false;
			
			if(executingFibers.Count > 0)
			{
				Fiber markerItem = new Fiber(() => {});
				executingFibers.Enqueue(markerItem);
				
				Fiber item;
				while(executingFibers.TryDequeue(out item))
				{
					if(item == markerItem)
						break;
					
					if(item == fiber)
						found = true;
					else
						executingFibers.Enqueue(item);
				}
				
				if(found)
					return;
			}
			
			if(sleepingFibers.Count > 0)
			{
				Tuple<Fiber, float> markerTuple = new Tuple<Fiber, float>(null, 0f);
				sleepingFibers.Enqueue(markerTuple);
				
				Tuple<Fiber, float> itemTuple;
				while(sleepingFibers.TryDequeue(out itemTuple))
				{
					if(itemTuple == markerTuple)
						break;
					
					if(itemTuple != null && itemTuple.Item1 == fiber)
						found = true;
					else
						sleepingFibers.Enqueue(itemTuple);
				}
			}
		}
		
		#region IDisposable implementation
		
		/// <summary>
		/// Tracks whether the object has been disposed already
		/// </summary>
		private bool isDisposed = false;
		
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
		protected override void Dispose(bool disposing) 
		{
			// Do nothing if already called
			if(isDisposed)
				return;
						
			if(disposing) 
			{
				// Free other state (managed objects).
				disposeWaitHandle.Set();
				runWaitHandle.WaitOne();
			}
			
			// Free your own state (unmanaged objects).
			// Set large fields to null.
			
			// Mark disposed
			isDisposed = true;
			
			base.Dispose(disposing);
		}
		#endregion
	}
}

