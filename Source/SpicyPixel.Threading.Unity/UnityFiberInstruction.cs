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
	/// Represents a fiber instruction to be processed by a 
	/// <see cref="FiberScheduler"/>.
	/// </summary>
	/// <remarks>
	/// Specific instructions understood by a scheduler are to be derived 
	/// from this abstract type.
	/// </remarks>
	/// <seealso cref="YieldForSeconds"/>
	/// <seealso cref="YieldToAnyFiber"/>
	/// <seealso cref="YieldToFiber"/>
	/// <seealso cref="YieldUntilComplete"/>
	public static class UnityFiberInstruction
	{
		/// <summary>
		/// Convenience coroutine to send a <see cref="WaitForSeconds"/> instruction to the scheduler.
		/// </summary>
		/// <returns>
		/// A <see cref="WaitForSeconds"/> instruction for a <see cref="UnityFiberScheduler"/>.
		/// </returns>
		/// <param name='seconds'>
		/// The seconds to wait.
		/// </param>
		public static FiberInstruction WaitForSeconds(float seconds)
		{
			return new ObjectInstruction(new UnityEngine.WaitForSeconds(seconds));
		}
		
		/// <summary>
		/// Convenience coroutine to send a WaitForFixedUpdate instruction to the scheduler.
		/// </summary>
		/// <returns>
		/// A WaitForFixedUpdate instruction for a <see cref="UnityFiberScheduler"/>.
		/// </returns>
		public static FiberInstruction WaitForFixedUpdate = new ObjectInstruction(new UnityEngine.WaitForFixedUpdate());
		
		/// <summary>
		/// Convenience coroutine to send a WaitForEndOfFrame instruction to the scheduler.
		/// </summary>
		/// <returns>
		/// A WaitForEndOfFrame instruction for a <see cref="UnityFiberScheduler"/>.
		/// </returns>
		public static FiberInstruction WaitForEndOfFrame = new ObjectInstruction(new UnityEngine.WaitForEndOfFrame());
	}
}