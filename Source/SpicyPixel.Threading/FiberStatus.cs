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

namespace SpicyPixel.Threading
{
	/// <summary>
	/// Represents the current state of a fiber.
	/// </summary>
	public enum FiberStatus
	{
        /// <summary>
        /// The fiber has been initialized but has not yet been scheduled.
        /// </summary>
        Created,

        /// <summary>
        /// The fiber is waiting to be activated and scheduled internally.
        /// </summary>
        /// <remarks>
        /// Generally this indicates a `ContinueWith` state because the fiber
        /// is not queued to the scheduler, it's waiting to activate and be
        /// scheduled once the antecdent fiber completes.
        /// </remarks>
        WaitingForActivation,

        /// <summary>
        /// The fiber has been scheduled for execution but has not yet begun executing.
        /// </summary>
        WaitingToRun,

        /// <summary>
        /// The fiber is running but has not yet completed.
        /// </summary>
        Running,

        /// <summary>
        /// The fiber completed execution successfully.
        /// </summary>
        RanToCompletion,

        /// <summary>
        /// The fiber acknowledged cancellation by throwing an OperationCanceledException 
        /// with its own CancellationToken while the token was in signaled state, or the 
        /// fiber's CancellationToken was already signaled before the fiber started executing.
        /// </summary>
        Canceled,

        /// <summary>
        /// The fiber completed due to an unhandled exception.
        /// </summary>
        Faulted
	}
}

