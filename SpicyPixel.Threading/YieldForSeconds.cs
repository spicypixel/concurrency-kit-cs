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
	/// A <see cref="FiberInstruction"/> to pause execution
	/// of a fiber for the specified duration.
	/// </summary>
	public sealed class YieldForSeconds : FiberInstruction
	{
		float seconds;
		
		/// <summary>
		/// Gets the seconds.
		/// </summary>
		/// <value>
		/// The seconds to pause execution for.
		/// </value>
		public float Seconds {
			get { return seconds; }
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="YieldForSeconds"/> class.
		/// </summary>
		/// <param name='seconds'>
		/// The seconds to pause execution for.
		/// </param>
		public YieldForSeconds (float seconds)
		{
			this.seconds = seconds;
		}
	}
}

