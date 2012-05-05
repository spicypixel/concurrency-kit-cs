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
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SpicyPixel.Threading.Tasks
{	
	/// <summary>
	/// Extends MonoBehaviour to support <see cref="FiberTaskScheduler"/>.
	/// </summary>
	public static class UnityTaskExtensions
	{
		/// <summary>
		/// Creates a task factory using a <see cref="FiberTaskScheduler"/>
		/// initialized with the MonoBehaviour.
		/// </summary>
		/// <returns>
		/// The task factory.
		/// </returns>
		/// <param name='behaviour'>
		/// The MonoBehaviour.
		/// </param>
		public static TaskFactory CreateTaskFactory(this MonoBehaviour behaviour)
		{
			var scheduler = new FiberTaskScheduler(new UnityFiberScheduler(behaviour));
			return new TaskFactory(scheduler.CancellationToken, TaskCreationOptions.None, TaskContinuationOptions.None, scheduler);
		}

		/// <summary>
		/// Creates the task scheduler.
		/// </summary>
		/// <returns>
		/// The task scheduler.
		/// </returns>
		/// <param name='behaviour'>
		/// Behaviour.
		/// </param>
		public static FiberTaskScheduler CreateTaskScheduler(this MonoBehaviour behaviour)
		{
			return new FiberTaskScheduler(new UnityFiberScheduler(behaviour));
		}
		
		/// <summary>
		/// Creates the fiber scheduler.
		/// </summary>
		/// <returns>
		/// The fiber scheduler.
		/// </returns>
		/// <param name='behaviour'>
		/// Behaviour.
		/// </param>
		public static FiberScheduler CreateFiberScheduler(this MonoBehaviour behaviour)
		{
			return new UnityFiberScheduler(behaviour);
		}
	}
}

