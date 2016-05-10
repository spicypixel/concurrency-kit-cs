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
	/// Fiber scheduler synchronization context to support task
	/// synchronization across schedulers or other synchronization
	/// models.
	/// </summary>
	public class FiberSchedulerSynchronizationContext : SynchronizationContext
	{
		FiberScheduler scheduler;
		
		/// <summary>
		/// Initializes a new instance of the <see cref="SpicyPixel.Threading.FiberSchedulerSynchronizationContext"/> class.
		/// </summary>
		/// <param name='scheduler'>
		/// The scheduler to send or post callbacks to.
		/// </param>
		public FiberSchedulerSynchronizationContext (FiberScheduler scheduler)
		{
			this.scheduler = scheduler;
		}
		
		/// <summary>
		/// Dispatches an asynchronous message to a synchronization context (the <see cref="FiberScheduler"/>).
		/// </summary>
		/// <remarks>
		/// The scheduler may choose to inline the callback if the Post is
		/// executed from the scheduler thread.
		/// </remarks>
		/// <param name='d'>
		/// Callback to invoke
		/// </param>
		/// <param name='state'>
		/// State to pass
		/// </param>
		public override void Post (SendOrPostCallback d, object state)
		{
			// The scheduler may choose to inline this if the Post
			// is executed from the scheduler thread.
			Fiber.StartNew(() => {
				d(state);
			}, scheduler);
		}

		/// <summary>
		/// Dispatches an synchronous message to a synchronization context (the <see cref="FiberScheduler"/>).
		/// </summary>
		/// <remarks>
		/// The callback is always inlined if Send is executed from the 
		/// scheduler thread regardless of any scheduler specific inline settings.
		/// Because inlining always occurs when on the scheduler thread, the 
		/// caller must manage stack depth.
		/// </remarks>
		/// <param name='d'>
		/// Callback to invoke
		/// </param>
		/// <param name='state'>
		/// State to pass
		/// </param>
		public override void Send (SendOrPostCallback d, object state)
		{
			// Force inlining if the threads match.
			if(scheduler.SchedulerThread == Thread.CurrentThread)
			{
				d(state);
				return;
			}
			
			// FIXME: This could block indefinitely if the scheduler goes down
			// before executing the task. Need another wait handle here or
			// better approach. Maybe add a WaitHandle to the fiber itself or
            // add Join().
			
			// The threads don't match, so queue the action
			// and wait for it to complete.
			ManualResetEvent wait = new ManualResetEvent(false);
			Fiber.StartNew(() => {
				d(state);
				wait.Set();
			}, scheduler);
			wait.WaitOne();
		}
	}
}