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
	/// Interface used by Fiber to access protected methods of the scheduler.
	/// </summary>
	internal interface IFiberScheduler
	{				
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
		void QueueFiber(Fiber fiber);
		
		/// <summary>
		/// Invoked when an abort has been requested.
		/// </summary>
		/// <param name='fiber'>
		/// The fiber to be aborted.
		/// </param>
		void AbortRequested(Fiber fiber);
	}
}