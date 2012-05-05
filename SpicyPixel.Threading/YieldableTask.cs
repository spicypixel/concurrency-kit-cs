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
using System.Threading.Tasks;
using System.Collections;

namespace SpicyPixel.Threading.Tasks
{
	/// <summary>
	/// Yieldable task for execution on a fiber.
	/// </summary>
	/// <remarks>
	/// Regular non-blocking tasks can also be scheduled on a <see cref="FiberTaskScheduler"/>,
	/// but yieldable tasks have the distinct ability to yield execution.
	/// </remarks>
	public class YieldableTask : Task
	{
		Fiber fiber;
		Exception fiberException;
		
		internal Fiber Fiber {
			get { return fiber; }
		}
		
		internal Exception FiberException {
			get { return fiberException; }
			set { fiberException = value; }
		}
		
        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.Tasks.YieldableTask"/> class.
        /// </summary>
        /// <returns>
        /// The task.
        /// </returns>
        /// <param name='coroutine'>
        /// The coroutine to execute.
        /// </param>
		public YieldableTask (IEnumerator coroutine) : 
			base(() => InternalAction())
		{
			fiber = new Fiber(coroutine);
		}
		
        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.Tasks.YieldableTask"/> class.
        /// </summary>
        /// <returns>
        /// The task.
        /// </returns>
        /// <param name='coroutine'>
        /// The coroutine to execute.
        /// </param>
        /// <param name='creationOptions'>
        /// Creation options.
        /// </param>
		public YieldableTask (IEnumerator coroutine, TaskCreationOptions creationOptions) : 
			base(() => InternalAction(), creationOptions)
		{
			fiber = new Fiber(coroutine);
		}
		
        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.Tasks.YieldableTask"/> class.
        /// </summary>
        /// <returns>
        /// The task.
        /// </returns>
        /// <param name='coroutine'>
        /// The coroutine to execute.
        /// </param>
        /// <param name='cancellationToken'>
        /// Cancellation token.
        /// </param>
		public YieldableTask (IEnumerator coroutine, CancellationToken cancellationToken) : 
			base(() => InternalAction(), cancellationToken)
		{
			fiber = new Fiber(coroutine);
		}
		
        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.Tasks.YieldableTask"/> class.
        /// </summary>
        /// <returns>
        /// The task.
        /// </returns>
        /// <param name='coroutine'>
        /// The coroutine to execute.
        /// </param>
        /// <param name='cancellationToken'>
        /// Cancellation token.
        /// </param>
        /// <param name='creationOptions'>
        /// Creation options.
        /// </param>
		public YieldableTask (IEnumerator coroutine, CancellationToken cancellationToken, TaskCreationOptions creationOptions) : 
			base(() => InternalAction(), cancellationToken, creationOptions)
		{
			fiber = new Fiber(coroutine);
		}
		
		/// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.Tasks.YieldableTask"/> class.
        /// </summary>
        /// <returns>
        /// The task.
        /// </returns>
        /// <param name='instruction'>
        /// The coroutine to execute.
        /// </param>
		public YieldableTask (FiberInstruction instruction) : 
			base(() => InternalAction())
		{
			fiber = new Fiber(() => instruction);
		}
		
        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.Tasks.YieldableTask"/> class.
        /// </summary>
        /// <returns>
        /// The task.
        /// </returns>
        /// <param name='instruction'>
        /// The coroutine to execute.
        /// </param>
        /// <param name='creationOptions'>
        /// Creation options.
        /// </param>
		public YieldableTask (FiberInstruction instruction, TaskCreationOptions creationOptions) : 
			base(() => InternalAction(), creationOptions)
		{
			fiber = new Fiber(() => instruction);
		}
		
        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.Tasks.YieldableTask"/> class.
        /// </summary>
        /// <returns>
        /// The task.
        /// </returns>
        /// <param name='instruction'>
        /// The coroutine to execute.
        /// </param>
        /// <param name='cancellationToken'>
        /// Cancellation token.
        /// </param>
		public YieldableTask (FiberInstruction instruction, CancellationToken cancellationToken) : 
			base(() => InternalAction(), cancellationToken)
		{
			fiber = new Fiber(() => instruction);
		}
		
        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.Tasks.YieldableTask"/> class.
        /// </summary>
        /// <returns>
        /// The task.
        /// </returns>
        /// <param name='instruction'>
        /// The instruction to execute.
        /// </param>
        /// <param name='cancellationToken'>
        /// Cancellation token.
        /// </param>
        /// <param name='creationOptions'>
        /// Creation options.
        /// </param>
		public YieldableTask (FiberInstruction instruction, CancellationToken cancellationToken, TaskCreationOptions creationOptions) : 
			base(() => InternalAction(), cancellationToken, creationOptions)
		{
			fiber = new Fiber(() => instruction);
		}
		
		/// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.Tasks.YieldableTask"/> class.
        /// </summary>
        /// <returns>
        /// The task.
        /// </returns>
        /// <param name='coroutine'>
        /// The coroutine to execute.
        /// </param>
		public YieldableTask (Func<FiberInstruction> coroutine) : 
			base(() => InternalAction())
		{
			fiber = new Fiber(coroutine);
		}
		
        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.Tasks.YieldableTask"/> class.
        /// </summary>
        /// <returns>
        /// The task.
        /// </returns>
        /// <param name='coroutine'>
        /// The coroutine to execute.
        /// </param>
        /// <param name='creationOptions'>
        /// Creation options.
        /// </param>
		public YieldableTask (Func<FiberInstruction> coroutine, TaskCreationOptions creationOptions) : 
			base(() => InternalAction(), creationOptions)
		{
			fiber = new Fiber(coroutine);
		}
		
        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.Tasks.YieldableTask"/> class.
        /// </summary>
        /// <returns>
        /// The task.
        /// </returns>
        /// <param name='coroutine'>
        /// The coroutine to execute.
        /// </param>
        /// <param name='cancellationToken'>
        /// Cancellation token.
        /// </param>
		public YieldableTask (Func<FiberInstruction> coroutine, CancellationToken cancellationToken) : 
			base(() => InternalAction(), cancellationToken)
		{
			fiber = new Fiber(coroutine);
		}
		
        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.Tasks.YieldableTask"/> class.
        /// </summary>
        /// <returns>
        /// The task.
        /// </returns>
        /// <param name='coroutine'>
        /// The coroutine to execute.
        /// </param>
        /// <param name='cancellationToken'>
        /// Cancellation token.
        /// </param>
        /// <param name='creationOptions'>
        /// Creation options.
        /// </param>
		public YieldableTask (Func<FiberInstruction> coroutine, CancellationToken cancellationToken, TaskCreationOptions creationOptions) : 
			base(() => InternalAction(), cancellationToken, creationOptions)
		{
			fiber = new Fiber(coroutine);
		}
		
		/// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.Tasks.YieldableTask"/> class.
        /// </summary>
        /// <returns>
        /// The task.
        /// </returns>
        /// <param name='coroutine'>
        /// The coroutine to execute.
        /// </param>
		/// <param name='state'>
		/// State to pass to the function.
		/// </param>
		public YieldableTask (Func<object, FiberInstruction> coroutine, object state) : 
			base(() => InternalAction())
		{
			fiber = new Fiber(coroutine, state);
		}
		
        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.Tasks.YieldableTask"/> class.
        /// </summary>
        /// <returns>
        /// The task.
        /// </returns>
        /// <param name='coroutine'>
        /// The coroutine to execute.
        /// </param>
		/// <param name='state'>
		/// State to pass to the function.
		/// </param>
        /// <param name='creationOptions'>
        /// Creation options.
        /// </param>
		public YieldableTask (Func<object, FiberInstruction> coroutine, object state, TaskCreationOptions creationOptions) : 
			base(() => InternalAction(), creationOptions)
		{
			fiber = new Fiber(coroutine, state);
		}
		
        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.Tasks.YieldableTask"/> class.
        /// </summary>
        /// <returns>
        /// The task.
        /// </returns>
        /// <param name='coroutine'>
        /// The coroutine to execute.
        /// </param>
		/// <param name='state'>
		/// State to pass to the function.
		/// </param>
        /// <param name='cancellationToken'>
        /// Cancellation token.
        /// </param>
		public YieldableTask (Func<object, FiberInstruction> coroutine, object state, CancellationToken cancellationToken) : 
			base(() => InternalAction(), cancellationToken)
		{
			fiber = new Fiber(coroutine, state);
		}
		
        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.Tasks.YieldableTask"/> class.
        /// </summary>
        /// <returns>
        /// The task.
        /// </returns>
        /// <param name='coroutine'>
        /// The coroutine to execute.
        /// </param>
		/// <param name='state'>
		/// State to pass to the function.
		/// </param>
        /// <param name='cancellationToken'>
        /// Cancellation token.
        /// </param>
        /// <param name='creationOptions'>
        /// Creation options.
        /// </param>
		public YieldableTask (Func<object, FiberInstruction> coroutine, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions) : 
			base(() => InternalAction(), cancellationToken, creationOptions)
		{
			fiber = new Fiber(coroutine, state);
		}
		
		private void InternalAction()
		{
            if(!(TaskScheduler.Current is FiberTaskScheduler))
                throw new InvalidOperationException("A YieldableTask can only be queued to a FiberTaskScheduler.");

			if(fiberException != null)
				throw fiberException;
		}		
	}
}