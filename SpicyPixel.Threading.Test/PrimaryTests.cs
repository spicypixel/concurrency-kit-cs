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
using NUnit.Framework;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using SpicyPixel.Threading;
using SpicyPixel.Threading.Tasks;

namespace SpicyPixel.Threading.Test
{
	[TestFixture()]
	public class PrimaryTests
	{
		[Test()]
		public void TestWaitForTwoFibers ()
		{
			var cancelSource = new CancellationTokenSource();
						
			var task = Task.Factory.StartNew(() => {
				Console.Out.WriteLine("Calling Fiber.StartNew(TestFiberNumbers())");
				Fiber.StartNew(TestFiberNumbers());
				Console.Out.WriteLine("Calling Fiber.StartNew(TestFiberLetters())");
				Fiber.StartNew(TestFiberLetters());
				Console.Out.WriteLine("WaitThenCancel");
				Fiber.StartNew(WaitThenCancel(2f, cancelSource));
				Console.Out.WriteLine("Run");
				FiberScheduler.Current.Run(cancelSource.Token);
				Console.Out.WriteLine("Run finished");
			});
			
			Console.Out.WriteLine("Starting to wait on main thread");
			task.Wait(10000);
			Console.Out.WriteLine("Done waiting");
		}
		
		[Test()]
		public void TestRunWithFiberUsingTask()
		{						
			var task = Task.Factory.StartNew(() => {
				FiberScheduler.Current.Run(new Fiber(RunWithFiberCoroutine()));
			});
			
			Console.Out.WriteLine("Starting to wait on main thread");
			task.Wait(10000);
			Console.Out.WriteLine("Done waiting");
		}
		
		[Test()]
		public void TestRunWithFiberUsingThread()
		{						
			var thread = new Thread(() => {
				FiberScheduler.Current.Run(new Fiber(RunWithFiberCoroutine()));
			});
			thread.Start();
			
			Console.Out.WriteLine("Starting to wait on main thread");
			thread.Join();
			Console.Out.WriteLine("Done waiting");
		}
		
		[Test()]
		public void TestFunc()
		{
			Fiber.StartNew(TestFiberLetters());
			var waitOnFiber2 = Fiber.StartNew(TestFiberNumbers());
			var mainFiber = new Fiber(() => waitOnFiber2);
			FiberScheduler.Current.Run(mainFiber); 
		}
		
		[Test()]
		public void TestYieldForSeconds()
		{
			var start = DateTime.Now;
			//var wait = Fiber.StartNew(() => new YieldForSeconds(2));
			//FiberScheduler.Current.Run(new Fiber(() => wait));
			//FiberScheduler.Current.Run(new Fiber(() => new YieldForSeconds(2)));
			
			FiberScheduler.Current.Run(new Fiber(WaitTwoSeconds()));
			var end = DateTime.Now;
			Assert.GreaterOrEqual(end - start, TimeSpan.FromSeconds(2));
			Assert.LessOrEqual(end - start, TimeSpan.FromSeconds(3));
		}
		
		private IEnumerator WaitTwoSeconds()
		{
			yield return new YieldForSeconds(2);
		}
				
		[Test()]
		public void TestFuncTask()
		{
			var start = DateTime.Now;
			Console.Out.WriteLine("Started: " + start);
			FiberScheduler.Current.Run(new Fiber(TestFuncTaskCoroutine()));
			var end = DateTime.Now;
			Console.Out.WriteLine("Ended: " + end);
			Assert.GreaterOrEqual(end - start, TimeSpan.FromSeconds(2));
			Assert.LessOrEqual(end - start, TimeSpan.FromSeconds(3));
		}
		
		private IEnumerator TestFuncTaskCoroutine()
		{
			var scheduler = new FiberTaskScheduler();

			var task = new YieldableTask(() => new YieldForSeconds(2));
			task.Start(scheduler);
			
			while(!task.IsCompleted)
				yield return FiberInstruction.YieldToAnyFiber;
		}
		
		[Test()]
		public void TestInstructionInTask()
		{
			var start = DateTime.Now;
			
			using(var backgroundFiberScheduler = SystemFiberScheduler.StartNew())
			{
				// Submit a task to the background scheduler and wait for it to complete
				var task = new YieldableTask(new YieldForSeconds(2));
				task.RunSynchronously(new FiberTaskScheduler(backgroundFiberScheduler));				
			}
			
			var end = DateTime.Now;
			Assert.GreaterOrEqual(end - start, TimeSpan.FromSeconds(2));
			Assert.LessOrEqual(end - start, TimeSpan.FromSeconds(3));
		}
		
		[Test()]
		public void TestCancellationToken()
		{
			var start = DateTime.Now;
			
			var cancelSource = new CancellationTokenSource();
			var backgroundFiberScheduler = SystemFiberScheduler.StartNew(cancelSource.Token);
			
			// Submit a task to the background scheduler and wait for it to complete
			var task = new YieldableTask(new YieldForSeconds(2));
			task.RunSynchronously(new FiberTaskScheduler(backgroundFiberScheduler));
			
			// Shutdown the scheduler thread
			cancelSource.Cancel();
			backgroundFiberScheduler.SchedulerThread.Join(5000);
			
			var end = DateTime.Now;
			Assert.GreaterOrEqual(end - start, TimeSpan.FromSeconds(2));
			Assert.LessOrEqual(end - start, TimeSpan.FromSeconds(3));
		}
		
		[Test()]
		public void TestYieldToFiber()
		{
			using(var backgroundFiberScheduler = SystemFiberScheduler.StartNew())
			{
				backgroundFiberScheduler.AllowInlining = true;
				var f1 = new Fiber(IncrementerCoroutine1());
				f1.Start(backgroundFiberScheduler);
				backgroundFiberScheduler.SchedulerThread.Join(2000);				
			}
			Assert.AreEqual(yieldToFiberCounter2, yieldToFiberCounter1 * 2);
		}
		
		//private Fiber yieldToFiber1;
		//private Fiber yieldToFiber2;
		private int yieldToFiberTotalCounter;
		private int yieldToFiberCounter1;
		private int yieldToFiberCounter2;
		
		private IEnumerator IncrementerCoroutine1()
		{
			Console.Out.WriteLine("IncrementerCoroutine1: Start");
			//Fiber other = Fiber.StartNew(IncrementerCoroutine2(Fiber.CurrentFiber)).Fiber;
			Fiber other = new Fiber(IncrementerCoroutine2(Fiber.CurrentFiber));
			while(yieldToFiberCounter1 < 25)
			{
				Console.Out.WriteLine("IncrementerCoroutine1: Loop");
				++yieldToFiberTotalCounter;
				++yieldToFiberCounter1;
				Console.Out.WriteLine("IncrementerCoroutine1: Yield 1");
				if(other.FiberState != FiberState.Stopped)
					yield return new YieldToFiber(other);
				Console.Out.WriteLine("IncrementerCoroutine1: Yield 2");
				if(other.FiberState != FiberState.Stopped)
					yield return new YieldToFiber(other);
			}
			Console.Out.WriteLine("IncrementerCoroutine1: Done");
		}
		
		private IEnumerator IncrementerCoroutine2(Fiber other)
		{
			Console.Out.WriteLine("IncrementerCoroutine2: Start");
			while(yieldToFiberCounter2 < 50)
			{
				Console.Out.WriteLine("IncrementerCoroutine2: Loop");
				++yieldToFiberTotalCounter;
				++yieldToFiberCounter2;
				Console.Out.WriteLine("IncrementerCoroutine2: Yield");
				if(other.FiberState != FiberState.Stopped)
					yield return new YieldToFiber(other);
			}
			Console.Out.WriteLine("IncrementerCoroutine2: Done");
		}
		
		private IEnumerator RunWithFiberCoroutine()
		{
			yield return Fiber.StartNew(TestFiberLetters());
			yield return Fiber.StartNew(TestFiberNumbers());
		}
			
		private IEnumerator TestFiberNumbers()
		{
			for(int i = 0; i < 26; i++)
			{
				Console.Out.WriteLine(i);
				yield return FiberInstruction.YieldToAnyFiber;
			}
		}
		
		private IEnumerator TestFiberLetters()
		{
			for(char c = 'a'; c <= 'z'; c++)
			{
				Console.Out.WriteLine(c);
				yield return FiberInstruction.YieldToAnyFiber;
			}
		}
		
		private IEnumerator WaitThenCancel(float duration, CancellationTokenSource cancelSource)
		{
			Console.Out.WriteLine("Starting wait: " + DateTime.Now);
			yield return new YieldForSeconds(duration);
			Console.Out.WriteLine("Ending wait and cancelling: " + DateTime.Now);
			cancelSource.Cancel();
		}
	}
}

