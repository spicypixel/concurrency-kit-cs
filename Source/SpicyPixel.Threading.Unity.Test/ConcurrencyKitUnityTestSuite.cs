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
using System;

/// <summary>
/// Test suite to verify Concurrency Kit functionality for Unity.
/// </summary>
public class ConcurrencyKitUnityTestSuite : MonoBehaviour
{
	Task task;
	
	private int yieldToFiberTotalCounter;
	private int yieldToFiberCounter1;
	private int yieldToFiberCounter2;
	
	// Use this for initialization
	void Start ()
	{
		var UnityTaskFactory = this.CreateTaskFactory();
		
		// TestYieldableActionException 
		task = UnityTaskFactory.StartNew (TestYieldableActionException ());		
		UnityTaskFactory.StartNew (WaitForSeconds(1f)).ContinueWith ((antecedent) =>
		{
			if (!task.IsFaulted)
				Debug.LogError ("TestYieldableActionException should have faulted.");
			else
				Debug.Log ("TestYieldableActionException succeeded.");
		});
		
		// Verify schedulers
		UnityTaskFactory.StartNew(() => { Debug.Log("StartNew scheduler: " + TaskScheduler.Current.GetType()); }).
			ContinueWith((antecedent) => { Debug.Log("ContinueWith scheduler (default): " + TaskScheduler.Current.GetType()); }).
			ContinueWith((antecedent) => { Debug.Log("ContinueWith scheduler (factory): " + TaskScheduler.Current.GetType()); }, UnityTaskFactory.Scheduler);
		
		// TestYieldableContinueWith
		UnityTaskFactory.StartNew(() => Debug.Log ("Running TestYieldableContinueWith")).
			ContinueWith(TestYieldableContinueWith(), UnityTaskFactory.Scheduler).
			ContinueWith((antecedent) => Debug.Log ("TestYieldableContinueWith succeeded"), UnityTaskFactory.Scheduler);
		
		// Test YieldToFiber
		//Fiber.StartNew(IncrementerCoroutine1(), ((FiberTaskScheduler)UnityTaskFactory.Scheduler).FiberScheduler);
	}
	
	private IEnumerator IncrementerCoroutine1()
	{
		Debug.Log("IncrementerCoroutine1: Start");
		//Fiber other = Fiber.StartNew(IncrementerCoroutine2(Fiber.CurrentFiber)).Fiber;
		Fiber other = new Fiber(IncrementerCoroutine2(Fiber.CurrentFiber));
		while(yieldToFiberCounter1 < 2500)
		{
			Debug.Log("IncrementerCoroutine1: Loop " + yieldToFiberCounter1);
			++yieldToFiberTotalCounter;
			++yieldToFiberCounter1;
			Debug.Log("IncrementerCoroutine1: Yield 1");
			if(other.FiberState != FiberState.Stopped)
				yield return new YieldToFiber(other);
			else
				Debug.LogWarning("IncrementerCoroutine1: Can't yield to stopped fiber");
			Debug.Log("IncrementerCoroutine1: Yield 2");
			if(other.FiberState != FiberState.Stopped)
				yield return new YieldToFiber(other);
			else
				Debug.LogWarning("IncrementerCoroutine1: Can't yield to stopped fiber");
		}
		Debug.Log("IncrementerCoroutine1: Done");
		Debug.Log("C1: " + (yieldToFiberCounter1 * 1).ToString() + " C2: " + yieldToFiberCounter2);
	}
	
	private IEnumerator IncrementerCoroutine2(Fiber other)
	{
		Debug.Log("IncrementerCoroutine2: Start");
		while(yieldToFiberCounter2 < 5000)
		{
			Debug.Log("IncrementerCoroutine2: Loop " + yieldToFiberCounter2);
			++yieldToFiberTotalCounter;
			++yieldToFiberCounter2;
			Debug.Log("IncrementerCoroutine2: Yield");
			if(other.FiberState != FiberState.Stopped)
				yield return new YieldToFiber(other);
			else
				Debug.LogWarning("IncrementerCoroutine2: Can't yield to stopped fiber");
		}
		Debug.Log("IncrementerCoroutine2: Done");
	}

	// Update is called once per frame
	void Update ()
	{
	}
	
	IEnumerator WaitForSeconds(float seconds)
	{
		yield return new SpicyPixel.Threading.YieldForSeconds (seconds);
	}
	
	IEnumerator TestYieldableActionException ()
	{
		yield return new SpicyPixel.Threading.YieldForSeconds (0.25f);
		throw new System.InvalidOperationException ("TestYieldableActionException is supposed to fail.");
	}
	
	IEnumerator TestYieldableContinueWith ()
	{
		yield return new SpicyPixel.Threading.YieldForSeconds (0.25f);
		Debug.Log ("TestYieldableContinueWith ran test.");
	}
}
