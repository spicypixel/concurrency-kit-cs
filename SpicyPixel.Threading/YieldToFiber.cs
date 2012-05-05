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
	/// Yield execution to a specific fiber belonging to the same
	/// scheduler as the current fiber.
	/// </summary>
	public sealed class YieldToFiber : FiberInstruction
	{
		/// <summary>
		/// Gets the fiber to yield to.
		/// </summary>
		/// <value>
		/// The fiber to yield to.
		/// </value>
		public Fiber Fiber { get; private set; }
		
		/// <summary>
		/// Initializes a new instance of the <see cref="SpicyPixel.Threading.YieldToFiber"/> class.
		/// </summary>
		/// <param name='fiber'>
		/// The fiber to yield to.
		/// </param>
		public YieldToFiber (Fiber fiber)
		{
			Fiber = fiber;
		}
	}
}