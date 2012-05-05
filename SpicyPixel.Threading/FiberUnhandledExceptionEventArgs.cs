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
	/// The event args passed to subscribers of unhandled fiber exception notifications.
	/// </summary>
	/// <seealso cref="Fiber.UnhandledException"/>
	public sealed class FiberUnhandledExceptionEventArgs : EventArgs
	{
		/// <summary>
		/// Gets the fiber that threw an exception.
		/// </summary>
		/// <value>
		/// The fiber.
		/// </value>
		public Fiber Fiber { get; private set; }
		
		/// <summary>
		/// Gets the exception.
		/// </summary>
		/// <value>
		/// The exception.
		/// </value>
		public Exception Exception { get; private set; }
		
		/// <summary>
		/// Gets or sets a value indicating whether the exception
		/// was handled and the scheduler should continue processing other fibers.
		/// </summary>
		/// <value>
		/// <c>true</c> if handled; otherwise, <c>false</c>.
		/// </value>
		public bool Handled { get; set; } 
		
		internal FiberUnhandledExceptionEventArgs (Fiber fiber, Exception ex)
		{
			Fiber = fiber;
			Exception = ex;
			Handled = false;
		}
	}
}

