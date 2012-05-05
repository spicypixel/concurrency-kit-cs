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
using UnityEngine;
using System.Collections;
using System.Threading.Tasks;
using SpicyPixel.Threading;
using SpicyPixel.Threading.Tasks;

namespace SpicyPixel.Threading
{
	/// <summary>
	/// Convenience class that extends MonoBehavior to provide a
	/// Scheduler and TaskFactory for executing tasks on the
	/// behaviour instance.
	/// </summary>
	/// <remarks>
	/// Derived classes must remember to use the <c>override</c>
	/// keyword when providing an <see cref="Awake"/> implementation
	/// or the task factory will not be initialized.
	/// </remarks>
	public abstract class ConcurrentBehaviour : MonoBehaviour 
	{
		private TaskFactory _taskFactory;
		private TaskScheduler _taskScheduler;
		private FiberScheduler _fiberScheduler;
	
		/// <summary>
		/// Gets the shared instance valid for the lifetime of the application.
		/// </summary>
		/// <value>
		/// The shared instance valid until Unity raises OnApplicationQuit().
		/// </value>
		/// <description>
		/// This instance can be used to create a scheduler and task factory
		/// not bound to a specific MonoBehaviour.
		/// </description>
		public static ConcurrentBehaviour SharedInstance {
			get {
				return SharedConcurrentBehaviour.SharedInstance;
			}
		}

		/// <summary>
		/// Gets the task factory.
		/// </summary>
		/// <value>
		/// The task factory.
		/// </value>
		public TaskFactory taskFactory {
			get { return _taskFactory; }
		}
		
		/// <summary>
		/// Gets the task scheduler for queuing to this MonoBehaviour.
		/// </summary>
		/// <value>
		/// The scheduler.
		/// </value>
		public TaskScheduler taskScheduler {
			get { return _taskScheduler; }
		}
		
		/// <summary>
		/// Gets the fiber scheduler for queuing to this MonoBehaviour.
		/// </summary>
		/// <value>
		/// The fiber scheduler.
		/// </value>
		public FiberScheduler fiberScheduler {
			get { return _fiberScheduler; }
		}
		
		/// <summary>
		/// Initializes the task factory during Awake().
		/// </summary>
		/// <remarks>
		/// The task factory cannot be initialized in the constructor
		/// because it must be initialized from the coroutine
		/// execution thread which the constructor does not guarantee.
		/// </remarks>
		protected virtual void Awake()
		{
			_taskFactory = this.CreateTaskFactory();
			_taskScheduler = _taskFactory.Scheduler;
			_fiberScheduler = ((FiberTaskScheduler)_taskScheduler).FiberScheduler;
		}
	}
}