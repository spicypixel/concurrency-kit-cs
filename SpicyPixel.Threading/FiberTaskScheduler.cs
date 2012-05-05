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
using System.Threading.Tasks;

namespace SpicyPixel.Threading.Tasks
{
	/// <summary>
	/// <see cref="TaskScheduler"/> that can execute fibers (yieldable coroutines).
	/// Regular non-blocking tasks can also be scheduled on a <see cref="FiberTaskScheduler"/>,
	/// but <see cref="YieldableTask"/> have the distinct ability to yield execution.
	/// </summary>
	public sealed class FiberTaskScheduler : TaskScheduler, IDisposable
	{	
        FiberScheduler scheduler;
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
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.Tasks.FiberTaskScheduler"/> class.
        /// </summary>
        /// <remarks>
        /// Derived classes should invoke EnableQueueTask() in their constructor when ready to begin executing
        /// tasks.
        /// </remarks>
        public FiberTaskScheduler () : this(FiberScheduler.Current)
        {     
        }
		
		/// <summary>
		/// Initializes a new instance of the <see cref="SpicyPixel.Threading.Tasks.FiberTaskScheduler"/> class.
		/// </summary>
		/// <remarks>
		/// Derived classes should invoke EnableQueueTask() in their constructor when ready to begin executing
		/// tasks.
		/// </remarks>
		public FiberTaskScheduler (FiberScheduler scheduler)
		{             
  			this.scheduler = scheduler;			
		}
		
		/// <summary>
		/// Gets the fiber scheduler associated with this task scheduler.
		/// </summary>
		/// <value>
		/// The fiber scheduler.
		/// </value>
		public FiberScheduler FiberScheduler
		{
			get { return scheduler; }
		}
		
		/// <summary>
		/// Queues a non-blocking task.
		/// </summary>
		/// <remarks>
		/// If the task is queued from the scheduler thread it will begin executing to its
		/// first yield immediately.
		/// </remarks>
		/// <param name="task">
		/// The non-blocking task to queue.
		/// </param>
		protected override void QueueTask (Task task)
		{	
            // Start a fiber to run the task
            Fiber.StartNew(ExecuteTask(task), scheduler);
		}
				
		/// <summary>
		/// Tries to execute the task inline.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Tasks executed on a fiber scheduler have thread affinity and must run on
		/// the thread the scheduler was created online. Inline execution will therefore
		/// fail if attempted from another thread besides the scheduler thread.
		/// </para>
		/// <para>
		/// A <see cref="YieldableTask"/> cannot run inline because yieldable tasks
		/// can only be processed by a <see cref="FiberTaskScheduler"/> when queued.
		/// </para>
		/// <para>
		/// Because of these restrictions, only standard non-blocking actions invoked 
		/// on the scheduler thread are eligible for inlining.
		/// </para>
		/// </remarks>
		/// <returns>
		/// Returns <c>true</c> if the task was executed inline, <c>false</c> otherwise.
		/// </returns>
		/// <param name="task">
		/// The task to execute.
		/// </param>
		/// <param name="taskWasPreviouslyQueued">
		/// Set to <c>true</c> if the task was previously queued, <c>false</c> otherwise.
		/// </param>
		protected override bool TryExecuteTaskInline (Task task, bool taskWasPreviouslyQueued)
		{
			// 1. Tasks using fiber schedulers have thread affinity and must run on the thread the scheduler was created on.
			// 2. Fiber tasks cannot run inline here because they may yield which is only handled when queued.
            //
            // That being said, the fiber scheduler may choose to inline anyway when
            // queueing occurs on the scheduler thread, it just doesn't happen in this method.
			if(scheduler.SchedulerThread != Thread.CurrentThread || task is YieldableTask)
				return false;
			
			return TryExecuteTask(task);
		}

        #region implemented abstract members of System.Threading.Tasks.TaskScheduler                
        
        /// <summary>
        /// Tries to dequeue a task.
        /// </summary>
        /// <remarks>
        /// Only delay start tasks can be dequeued. Although the Fiber scheduler does
        /// delay start tasks queued from a non-scheduler thread, de-queuing is not
        /// supported right now and so this method always returns <c>false</c>.
        /// </remarks>
        /// <returns>
        /// Returns <c>true</c> if the task was dequeued, <c>false</c> otherwise.
        /// </returns>
        /// <param name="task">
        /// The task to dequeue.
        /// </param>
        protected override bool TryDequeue (Task task)
        {
            return false;
        }
        
        /// <summary>
        /// For debugger support only, generates an enumerable of Task instances 
        /// currently queued to the scheduler waiting to be executed.
        /// </summary>
        /// <remarks>
        /// This is not supported and will always return null.
        /// </remarks>
        /// <returns>
        /// The scheduled tasks.
        /// </returns>
        protected override IEnumerable<Task> GetScheduledTasks ()
        {
            return null;
        }
        
        #endregion
		
		/// <summary>
		/// Execute the specified task as a coroutine.
		/// </summary>
        /// <param name="task">
		/// The task to be executed.
		/// </param>
        /// <returns>
		/// <see cref="FiberInstruction"/> or other scheduler specific instruction.
		/// </returns>
		private IEnumerator ExecuteTask (Task task)
		{
			// Execute yieldable actions using the MonoBehavior.
			//
			// The continuation will execute the task itself 
			// via TryExecuteTask(). That task action will
			// rethrow any exceptions caught during the yieldable
			// action invocation and TryExecuteTask() will set
			// the final state.
			//
			// The downside to this approach is that yieldable tasks
			// aren't considered running until they have already
			// completed because TryExecuteTask() is what sets state.
			var yieldableTask = task as YieldableTask;
			if(yieldableTask != null)
				yield return Fiber.StartNew(ExecuteYieldableTask(yieldableTask), scheduler);
			
			// Run the action
			TryExecuteTask(task);
		}
		
		/// <summary>
		/// Execute the specified coroutine associated with a yieldable task.
		/// </summary>
		/// <remarks>
		/// Any exceptions that occur while executing the fiber will be
		/// associated with the specified task and rethrown by the framework.
		/// </remarks>
        /// <param name="task">
		/// The task associated with the executing fiber.
		/// </param>
        /// <returns>
		/// <see cref="FiberInstruction"/> or other scheduler specific instruction.
		/// </returns>
		private IEnumerator ExecuteYieldableTask(YieldableTask task)
		{
			object fiberResult = null;

			// TODO: Cleanup
			//
			// This uses internal setters to fake starting the fiber
			// and then uses internal Execute(). It should be possible to
			// use public APIs instead to:
			//   1. Set a fiber property to associate the task
			//   2. Set an exception handler on the FiberScheduler
			//   3. Retrieve the task in the handler from the fiber and set
			//      the exeption
			//   4. Return from the handler without rethrowing
			//
			// This method could be removed then and the internal setters
			// removed as well.
			task.Fiber.Scheduler = scheduler;
			task.Fiber.FiberState = FiberState.Running;
			
			while(true)
			{
				try
				{
					cancelSource.Token.ThrowIfCancellationRequested();
					
					fiberResult = task.Fiber.Execute();
					
					if(fiberResult is StopInstruction)
						yield break;
				}
				catch(Exception ex)
				{
					task.FiberException = ex;
					yield break;
				}

                yield return fiberResult;
			}
		}
				
		#region IDisposable implementation
		
		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// <see cref="SpicyPixel.Threading.Tasks.FiberTaskScheduler"/> is reclaimed by garbage collection.
		/// </summary>
		~FiberTaskScheduler()
		{
			Dispose(false);
		}
		
		/// <summary>
		/// Tracks whether the object has been disposed already
		/// </summary>
		private bool isDisposed = false;
		
		/// <summary>
		/// Releases all resource used by the <see cref="SpicyPixel.Threading.Tasks.FiberTaskScheduler"/> object.
		/// </summary>
		/// <remarks>
		/// Call the method when you are finished using the
		/// <see cref="SpicyPixel.Threading.Tasks.FiberTaskScheduler"/>. The method leaves the
		/// <see cref="SpicyPixel.Threading.Tasks.FiberTaskScheduler"/> in an unusable state. After calling
		/// the method, you must release all references to the
		/// <see cref="SpicyPixel.Threading.Tasks.FiberTaskScheduler"/> so the garbage collector can reclaim the memory that
		/// the <see cref="SpicyPixel.Threading.Tasks.FiberTaskScheduler"/> was occupying.
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
		private /*protected virtual*/ void Dispose(bool disposing) 
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

