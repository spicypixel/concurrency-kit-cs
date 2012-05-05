// SpinWait.cs
//
// Copyright (c) 2008 Jérémie "Garuma" Laval
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//

#if NET_4_0
using System;

namespace System.Threading
{
	public struct SpinWait
	{
		// The number of step until SpinOnce yield on multicore machine
		const           int  step = 10;
		const           int  maxTime = 200;
		static readonly bool isSingleCpu = (Environment.ProcessorCount == 1);

		int ntime;

		public void SpinOnce ()
		{
			ntime += 1;

			if (isSingleCpu) {
				// On a single-CPU system, spinning does no good
				Yield ();
			} else {
				if (ntime % step == 0)
					Yield ();
				else
					// Multi-CPU system might be hyper-threaded, let other thread run
					Thread.SpinWait (Math.Min (ntime, maxTime) << 1);
			}
		}
		
		// Spicy Pixel: Thread.Yield () is not supported in .NET < 4
		const int s1step = 20;
		int s1time;
		void Yield ()
		{
			// Replace sched_yield by Thread.Sleep(0) which does almost the same thing
			// (going back in kernel mode and yielding) but avoid the branching and unmanaged bridge
			//Thread.Sleep (0); // Thread.Yield() is better

			/*
			    http://stackoverflow.com/questions/1413630/switchtothread-thread-yield-vs-thread-sleep0-vs-thead-sleep1
			    
				SwitchToThread [win32] / Thread.Yield [.NET 4 Beta 1]: yields to any thread on same processor
				Advantage: about twice as fast as Thread.Sleep(0)
				Disadvantage: yields only to threads on same processor
				
				Thread.Sleep(0): yields to any thread of same or higher priority on any processor
				Advantage: faster than Thread.Sleep(1)
				Disadvantage: yields only to threads of same or higher priority
				
				Thread.Sleep(1): yields to any thread on any processor
				Advantage: yields to any thread on any processor
				Disadvantage: slowest option (Thread.Sleep(1) will usually suspend the thread by about 15ms if timeBeginPeriod/timeEndPeriod [win32] are not used)
			*/

			// Because Sleep(1) pulls us out of the queue for 10-15ms or so, 
			// don't do it that often.
			s1time += 1;

			if(s1time % s1step == 0)
				Thread.Sleep(1);
			else
				Thread.Sleep(0);
		}

		public static void SpinUntil (Func<bool> condition)
		{
			SpinWait sw = new SpinWait ();
			while (!condition ())
				sw.SpinOnce ();
		}

		public static bool SpinUntil (Func<bool> condition, TimeSpan timeout)
		{
			return SpinUntil (condition, (int)timeout.TotalMilliseconds);
		}

		public static bool SpinUntil (Func<bool> condition, int millisecondsTimeout)
		{
			SpinWait sw = new SpinWait ();
			Watch watch = Watch.StartNew ();

			while (!condition ()) {
				if (watch.ElapsedMilliseconds > millisecondsTimeout)
					return false;
				sw.SpinOnce ();
			}

			return true;
		}

		public void Reset ()
		{
			ntime = 0;
			s1time = 0;
		}

		public bool NextSpinWillYield {
			get {
				return isSingleCpu ? true : ntime % step == 0;
			}
		}

		public int Count {
			get {
				return ntime;
			}
		}
	}
}
#endif
