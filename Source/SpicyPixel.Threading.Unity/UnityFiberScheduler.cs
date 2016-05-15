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
using UnityEngine;

namespace SpicyPixel.Threading
{
	/// <summary>
    /// <see cref="FiberScheduler"/> that can execute fibers (yieldable coroutines)
    /// during the update cycle of a MonoBehaviour.
    /// </summary>
	public sealed class UnityFiberScheduler : FiberScheduler
	{	
        internal const string UnityCoroutineKey = "spicypixel.threading.unity.coroutine";

		private static readonly UnityFiberScheduler instance = new UnityFiberScheduler(ConcurrentBehaviour.SharedInstance);

		/// <summary>
		/// Gets the shared fiber scheduler instance.
		/// </summary>
		/// <value>The shared fiber scheduler instance bound to the shared <see cref="SpicyPixel.Threading.ConcurrentBehaviour"/> .</value>
		public static UnityFiberScheduler Default {
			get {
				return instance;
			}
		}

        /// <summary>
        /// The behaviour to use for scheduling with Unity.
        /// </summary>
        private MonoBehaviour behaviour;

        /// <summary>
        /// When tasks are queued from another thread they must be added to
        /// a queue for processing on the scheduler thread.
        /// </summary>
        private ConcurrentQueue<Fiber> fiberQueue = new ConcurrentQueue<Fiber>();
		
		/// <summary>
		/// The cancel source for the fiber queue. Used by Dispose().
		/// </summary>
		private CancellationTokenSource fiberQueueCancelSource = new CancellationTokenSource();

        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.UnityFiberScheduler"/> class.
        /// </summary>
        /// <param name='behaviour'>
        /// The behaviour to use for scheduling with Unity.
        /// </param>
        public UnityFiberScheduler(MonoBehaviour behaviour)
        {
            this.behaviour = behaviour;
            behaviour.StartCoroutine(ProcessFiberQueue());
        }

        /// <summary>
        /// Queues the fiber for execution on the scheduler. 
        /// </summary>
        /// <remarks>
        /// Fibers queued from the scheduler thread will generally be executed inline whenever possible on most
        /// schedulers. 
        /// </remarks>
        /// <param name='fiber'>
        /// The fiber to queue.
        /// </param>
        protected override void QueueFiber(Fiber fiber)
        {
			if(isDisposed)
				throw new ObjectDisposedException(GetType().FullName);
			
            // Coroutines are always inlined up to their
            // first yield, so enqueuing here likely doesn't do
            // anything other than delay a frame. But, it does
            // ensure the correct thread.
            if(AllowInlining && SchedulerThread == Thread.CurrentThread)
                StartUnityFiber(fiber);
            else
                fiberQueue.Enqueue(fiber);
        }

        /// <summary>
        /// Invoked when an abort has been requested. 
        /// </summary>
        /// <remarks>
        /// Unity is always running scheduled fibers and so there is nothing
        /// special to do here to get it to attempt to execute the fiber again
        /// (which will trigger the abort).
        /// </remarks>
        /// <param name='fiber'>
        /// The fiber to be aborted. 
        /// </param>
        protected override void AbortRequested(Fiber fiber)
        {
        }

        /// <summary>
        /// Runs on the scheduler thread and dispatches all queued fibers.
        /// </summary>
        /// <returns>
        /// Yield instructions.
        /// </returns>
        private IEnumerator ProcessFiberQueue()
        {
            Fiber fiber;
            while(!fiberQueueCancelSource.IsCancellationRequested)
            {
                while(!fiberQueueCancelSource.IsCancellationRequested && fiberQueue.TryDequeue(out fiber))
                    StartUnityFiber(fiber);

                yield return null;
            }
        }

        /// <summary>
        /// Starts a fiber using the Unity scheduler.
        /// </summary>
        /// <remarks>
        /// This wraps the fiber in a special coroutine in order to convert between 
        /// the framework and Unity. Additionally it saves the Unity coroutine
        /// and associates it with the fiber so it can be used later for wait
        /// operations. Note that Unity StartCoroutine will execute inline to
        /// the first yield.
        /// </remarks>
        /// <param name='fiber'>
        /// The fiber to start executing.
        /// </param>
        private void StartUnityFiber(Fiber fiber)
        {	
            Coroutine coroutine = behaviour.StartCoroutine(ExecuteFiberInternal(fiber));
            fiber.Properties[UnityCoroutineKey] = coroutine;
        }

        /// <summary>
        /// Wraps fiber execution to translate between framework and Unity concepts.
        /// </summary>
        /// <returns>
        /// A yield instruction that Unity will understand.
        /// </returns>
        /// <param name='fiber'>
        /// The fiber to execute.
        /// </param>
        /// <param name='singleStep'>
        /// If <c>true</c>, the method only executes a single step before breaking.
        /// This is used when switching between two fibers using <see cref="YieldToFiber"/>.
        /// </param>
        /// <param name='fiberSwitchCount'>
        /// This is the number of times a fiber switch has occured. 10 switches are
        /// allowed before unwinding in case Unity doesn't do this automatically.
        /// </param>
        private IEnumerator ExecuteFiberInternal(Fiber fiber, bool singleStep = false, int fiberSwitchCount = 0)
        {
            FiberInstruction fiberInstruction = null;
            bool ranOnce = false;

            while(!fiber.IsCompleted)
            {
                // If we are set to only advance one instruction then
                // abort if we have already done that
                if(singleStep && ranOnce)
                    yield break;
                ranOnce = true;

                // Execute the fiber
                fiberInstruction = ExecuteFiber(fiber);
    
                // Nothing more to do if stopped
                if(fiberInstruction is StopInstruction)
                    yield break;

				// Not supported in Unity
				if(fiberInstruction is YieldToFiber)
					throw new InvalidOperationException("YieldToFiber is not supported by the Unity scheduler.");
    
                // Yield to any fiber means send null to the Unity scheduler
                if(fiberInstruction is YieldToAnyFiber)
                {
                    yield return null;
                    continue;
                }
    
                // Pass back any objects directly to the Unity scheduler since
                // these could be Unity scheduler commands
                if(fiberInstruction is ObjectInstruction)
                {
                    yield return ((ObjectInstruction)fiberInstruction).Value;
                    continue;
                }
    
                // Convert framework wait instruction to Unity instruction
                if(fiberInstruction is YieldForSeconds)
                {
                    yield return new WaitForSeconds(((YieldForSeconds)fiberInstruction).Seconds);
                    continue;
                }

                // Convert framework wait instruction to Unity instruction
                if(fiberInstruction is YieldUntilComplete)
                {
                    // Yield the coroutine that was stored when the instruction was started.
                    yield return ((YieldUntilComplete)fiberInstruction).Fiber.Properties[UnityCoroutineKey];
                    continue;
                }				
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents the current <see cref="SpicyPixel.Threading.UnityFiberScheduler"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents the current <see cref="SpicyPixel.Threading.UnityFiberScheduler"/>.
        /// </returns>
        public override string ToString()
        {
			if(SchedulerThread == Thread.CurrentThread)
            	return string.Format("[UnityFiberScheduler][{0}]", behaviour.ToString());
			else
				return base.ToString();
        }
		
		private bool isDisposed = false;
		
		/// <summary>
		/// Dispose the scheduler. 
		/// </summary>
		/// <param name='disposing'>
		/// Disposing.
		/// </param>
		protected override void Dispose (bool disposing)
		{
			if(isDisposed)
				return;
			
			if(disposing)
			{
				fiberQueueCancelSource.Cancel();
				Fiber fiber;
				while(fiberQueue.TryDequeue(out fiber));
			}
			
			isDisposed = true;
			
			base.Dispose (disposing);
		}
	}
}