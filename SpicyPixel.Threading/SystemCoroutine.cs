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

namespace SpicyPixel.Threading
{
	/// <summary>
	/// This static class exposes convenience coroutines that can be
	/// passed to a fiber or task.
	/// </summary>
	public static class SystemCoroutine
	{
#if false
		/// <summary>
		/// Convenience coroutine to send a <see cref="YieldForSeconds"/> instruction to the scheduler and wait on it to complete.
		/// </summary>
		/// <returns>
		/// A <see cref="YieldForSeconds"/> <see cref="FiberInstruction"/>.
		/// </returns>
		/// <param name="seconds">
		/// The seconds to wait.
		/// </param>
		internal static FiberInstruction YieldForSeconds(float seconds)
		{
			return new YieldForSeconds(seconds);
		}

		/// <summary>
		/// Convenience coroutine to repeat an action for the specified duration at the scheduler frequency.
		/// </summary>
		/// <remarks>
		/// This is internal until there is a scheduler agnostic way to handle delta time.
		/// </remarks>
		/// <returns>
		/// A <see cref="FiberInstruction"/>.
		/// </returns>
		/// <param name='action'>
		/// The action to execute.
		/// </param>
		/// <param name='seconds'>
		/// The seconds to execute for at the scheduler frequency.
		/// </param>
		internal static IEnumerator RepeatForSeconds(Action<float> action, float seconds)
		{
			long startTime = DateTime.Now.Ticks;
            float deltaTime = 0.0f;

            for (float progress = 0.0f; progress < 1.0f; progress += deltaTime/seconds) 
            {
                action(progress);
                yield return FiberInstruction.YieldToAnyFiber;

                deltaTime = (float)(DateTime.Now.Ticks - startTime) / TimeSpan.TicksPerSecond;
            }
            
            action(1.0f);
		}	
#endif
		
		/// <summary>
		/// Convenience coroutine to repeat an action for the specified number of iterations at the scheduler frequency.
		/// </summary>
		/// <returns>
		/// A <see cref="FiberInstruction"/>.
		/// </returns>
		/// <param name='action'>
		/// The action to execute.
		/// </param>
		/// <param name='iterations'>
		/// The iterations to execute for.
		/// </param>
		public static IEnumerator RepeatForIterations(Action<int> action, int iterations)
		{
			for (int i = 0; i < iterations; ++i)
            {
                action(i);
                yield return FiberInstruction.YieldToAnyFiber;
            }
		}
	}
}