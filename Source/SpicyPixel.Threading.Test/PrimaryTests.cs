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
using System.Collections.Generic;
using System.Linq;

namespace SpicyPixel.Threading.Test
{
    [TestFixture()]
    public class PrimaryTests
    {
        [Test()]
        public void TestWaitForTwoFibers()
        {
            var cancelSource = new CancellationTokenSource();
						
            var task = Task.Factory.StartNew(() => {
                Console.Out.WriteLine("Calling Fiber.Factory.StartNew(TestFiberNumbers())");
                Fiber.Factory.StartNew(TestFiberNumbers());
                Console.Out.WriteLine("Calling Fiber.Factory.StartNew(TestFiberLetters())");
                Fiber.Factory.StartNew(TestFiberLetters());
                Console.Out.WriteLine("WaitThenCancel");
                Fiber.Factory.StartNew(WaitThenCancel(2f, cancelSource));
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
            Fiber.Factory.StartNew(TestFiberLetters());
            var waitOnFiber2 = Fiber.Factory.StartNew(TestFiberNumbers());
            var mainFiber = new Fiber(() => new YieldUntilComplete(waitOnFiber2));
            FiberScheduler.Current.Run(mainFiber); 
        }

        [Test()]
        public void TestYieldForSeconds()
        {
            var start = DateTime.Now;
            //var wait = Fiber.Factory.StartNew(() => new YieldForSeconds(2));
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
			
            while (!task.IsCompleted)
                yield return FiberInstruction.YieldToAnyFiber;
        }

        [Test()]
        public void TestInstructionInTask()
        {
            var start = DateTime.Now;
			
            using (var backgroundFiberScheduler = SystemFiberScheduler.StartNew()) {
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

        List<int> testNestingSteps = new List<int>();

        [Test()]
        public void TestNestedFibers()
        {
            testNestingSteps.Clear();
            FiberScheduler.Current.Run(new Fiber(FadeAndMoveAndShoot(true)));

            Assert.AreEqual(10, testNestingSteps.Count, "All steps in nested fibers did not complete");
            for (int i = 0; i < 10; ++i) {
                Assert.AreEqual(i + 1, testNestingSteps[i], "Fibers did not run in sequence");
            }
        }

        private IEnumerator FadeAndMoveAndShoot(bool asFiber)
        {
            testNestingSteps.Add(1);
            if (asFiber)
                yield return Fiber.Factory.StartNew(Fade());
            else
                yield return Fade();
            testNestingSteps.Add(4);
            if (asFiber)
                yield return Fiber.Factory.StartNew(MoveAndShoot(asFiber));
            else
                yield return MoveAndShoot(asFiber);
            testNestingSteps.Add(10);
        }

        private IEnumerator Fade()
        {
            testNestingSteps.Add(2);
            yield return new YieldForSeconds(2.0f);
            testNestingSteps.Add(3);
        }

        private IEnumerator MoveAndShoot(bool asFiber)
        {
            testNestingSteps.Add(5);
            yield return new YieldForSeconds(3.0f);
            testNestingSteps.Add(6);
            if (asFiber)
                yield return Fiber.Factory.StartNew(Shoot());
            else
                yield return Shoot();
            testNestingSteps.Add(9);
        }

        private IEnumerator Shoot()
        {
            testNestingSteps.Add(7);
            yield return new YieldForSeconds(1.0f);
            testNestingSteps.Add(8);
        }

        [Test()]
        public void TestNestedCoroutines()
        {
            testNestingSteps.Clear();
            FiberScheduler.Current.Run(new Fiber(FadeAndMoveAndShoot(false)));

            Assert.AreEqual(10, testNestingSteps.Count, "All steps in nested coroutines did not complete");
            for (int i = 0; i < 10; ++i) {
                Assert.AreEqual(i + 1, testNestingSteps[i], "Coroutines did not run in sequence");
            }
        }

        [Test()]
        public void TestNestedCoroutinesBasic()
        {
            FiberScheduler.Current.Run(new Fiber(Coroutine1()));
            Assert.IsTrue(coroutine2Completed, "coroutine2 did not complete");
            Assert.IsTrue(coroutine1Completed, "coroutine1 did not complete");
        }

        IEnumerator Coroutine1()
        {
            Console.Out.WriteLine("TestNestedCoroutines: Coroutine1 entered");
            yield return Coroutine2();
            coroutine1Completed = true;
            Console.Out.WriteLine("TestNestedCoroutines: Coroutine1 completed");
        }

        IEnumerator Coroutine2()
        {
            Console.Out.WriteLine("TestNestedCoroutines: Coroutine2 entered");
            for (int i = 0; i < 25; ++i)
                yield return FiberInstruction.YieldToAnyFiber;

            coroutine2Completed = true;
            Console.Out.WriteLine("TestNestedCoroutines: Coroutine2 completed");
        }

        bool coroutine1Completed = false;
        bool coroutine2Completed = false;

        [Test()]
        public void TestYieldToFiber()
        {
            using (var backgroundFiberScheduler = SystemFiberScheduler.StartNew()) {
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
            //Fiber other = Fiber.Factory.StartNew(IncrementerCoroutine2(Fiber.CurrentFiber)).Fiber;
            Fiber other = new Fiber(IncrementerCoroutine2(Fiber.CurrentFiber));
            while (yieldToFiberCounter1 < 25) {
                Console.Out.WriteLine("IncrementerCoroutine1: Loop");
                ++yieldToFiberTotalCounter;
                ++yieldToFiberCounter1;
                Console.Out.WriteLine("IncrementerCoroutine1: Yield 1");
                if (!other.IsCompleted)
                    yield return new YieldToFiber(other);
                Console.Out.WriteLine("IncrementerCoroutine1: Yield 2");
                if (!other.IsCompleted)
                    yield return new YieldToFiber(other);
            }
            Console.Out.WriteLine("IncrementerCoroutine1: Done");
        }

        private IEnumerator IncrementerCoroutine2(Fiber other)
        {
            Console.Out.WriteLine("IncrementerCoroutine2: Start");
            while (yieldToFiberCounter2 < 50) {
                Console.Out.WriteLine("IncrementerCoroutine2: Loop");
                ++yieldToFiberTotalCounter;
                ++yieldToFiberCounter2;
                Console.Out.WriteLine("IncrementerCoroutine2: Yield");
                if (!other.IsCompleted)
                    yield return new YieldToFiber(other);
            }
            Console.Out.WriteLine("IncrementerCoroutine2: Done");
        }

        private IEnumerator RunWithFiberCoroutine()
        {
            yield return Fiber.Factory.StartNew(TestFiberLetters());
            yield return Fiber.Factory.StartNew(TestFiberNumbers());
        }

        private IEnumerator TestFiberNumbers()
        {
            for (int i = 0; i < 26; i++) {
                Console.Out.WriteLine(i);
                yield return FiberInstruction.YieldToAnyFiber;
            }
        }

        private IEnumerator TestFiberLetters()
        {
            for (char c = 'a'; c <= 'z'; c++) {
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

        ///////////////////////////
        // WaitAll

        List<int> waitAllTokens = new List<int>();

        [Test()]
        public void TestWhenAll()
        {
            waitAllTokens.Clear();

            var fibers = new Fiber[] {
                Fiber.Factory.StartNew(WaitRandomTimeCoroutine(0)),
                Fiber.Factory.StartNew(WaitRandomTimeCoroutine(1)),
                Fiber.Factory.StartNew(WaitRandomTimeCoroutine(2)),
                Fiber.Factory.StartNew(WaitRandomTimeCoroutine(3)),
                Fiber.Factory.StartNew(WaitRandomTimeCoroutine(4)),
                Fiber.Factory.StartNew(WaitRandomTimeCoroutine(5))
            };

            FiberScheduler.Current.Run(new Fiber(TestWhenAllCoroutine(fibers)));
        }

        IEnumerator TestWhenAllCoroutine(Fiber[] fibers)
        {
            foreach (var fiber in fibers)
                Assert.IsFalse(fiber.IsCompleted, "Fiber was not running");

            var waitAllFiber = Fiber.WhenAll(fibers);
            yield return waitAllFiber;

            foreach (var fiber in fibers)
                Assert.IsTrue(fiber.IsCompleted, "Fiber was still running and should have been stopped");

            // Result should be true
            Assert.IsNotNull(waitAllFiber.ResultAsObject, "Result should not be null");
            Assert.IsTrue((bool)waitAllFiber.ResultAsObject, "Result should have been true");
        }

        IEnumerator WaitRandomTimeCoroutine(int token)
        {
            waitAllTokens.Add(token);
            yield return new YieldForSeconds((float)(new Random().Next() % 30) / 10f);
        }

        [Test()]
        public void TestWhenAllTimeout()
        {
            var fibers = new Fiber[] {
                Fiber.Factory.StartNew(() => new YieldForSeconds(3f)),
                Fiber.Factory.StartNew(() => new YieldForSeconds(3f)),
                Fiber.Factory.StartNew(() => new YieldForSeconds(3f)),
                Fiber.Factory.StartNew(() => new YieldForSeconds(3f)),
                Fiber.Factory.StartNew(() => new YieldForSeconds(3f)),
                Fiber.Factory.StartNew(() => new YieldForSeconds(3f))
            };

            FiberScheduler.Current.Run(new Fiber(TestWhenAllTimeoutCoroutine(fibers)));
        }

        IEnumerator TestWhenAllTimeoutCoroutine(Fiber[] fibers)
        {
            // Timeout after 2s
            var waitAllFiber = Fiber.WhenAll(fibers, TimeSpan.FromSeconds(2.0));
            yield return waitAllFiber;

            // Some should still be running
            Assert.IsTrue(fibers.Any(f => f.Status == FiberStatus.Running), "Some fiber should have been running but was not");

            // Wait 2s
            yield return new YieldForSeconds(2);

            // Now none should be running
            Assert.IsFalse(fibers.Any(f => f.Status == FiberStatus.Running), "No fibers should have been running");

            // Result should be false
            Assert.IsNotNull(waitAllFiber.ResultAsObject, "Result should not be null");
            Assert.IsFalse((bool)waitAllFiber.ResultAsObject, "Result should have been false");
        }

        [Test()]
        public void TestWhenAllCancellation()
        {
            var fibers = new Fiber[] {
                Fiber.Factory.StartNew(() => new YieldForSeconds(3f)),
                Fiber.Factory.StartNew(() => new YieldForSeconds(3f)),
                Fiber.Factory.StartNew(() => new YieldForSeconds(3f)),
                Fiber.Factory.StartNew(() => new YieldForSeconds(3f)),
                Fiber.Factory.StartNew(() => new YieldForSeconds(3f)),
                Fiber.Factory.StartNew(() => new YieldForSeconds(3f))
            };

            FiberScheduler.Current.Run(new Fiber(TestWhenAllCancellationCoroutine(fibers)));
        }

        IEnumerator TestWhenAllCancellationCoroutine(Fiber[] fibers)
        {
            var cancelSource = new CancellationTokenSource();
            Fiber.Factory.StartNew(WaitThenCancel(2f, cancelSource));

            // Cancels after 2s
            var waitAllFiber = Fiber.WhenAll(fibers, cancelSource.Token);
            yield return waitAllFiber;

            // Some should still be running
            Assert.IsTrue(fibers.Any(f => f.Status == FiberStatus.Running), "Some fiber should have been running but was not");

            // Wait 2s
            yield return new YieldForSeconds(2);

            // Now none should be running
            Assert.IsFalse(fibers.Any(f => f.Status == FiberStatus.Running), "No fibers should have been running");

            // Result should be false
            Assert.IsNotNull(waitAllFiber.ResultAsObject, "Result should not be null");
            Assert.IsFalse((bool)waitAllFiber.ResultAsObject, "Result should have been false");
        }

        [Test()]
        public void TestWhenAny()
        {
            var fibers = new Fiber[] {
                Fiber.Factory.StartNew(() => new YieldForSeconds(1.5f)),
                Fiber.Factory.StartNew(() => new YieldForSeconds(1.6f)),
                Fiber.Factory.StartNew(() => new YieldForSeconds(1f)),
                Fiber.Factory.StartNew(() => new YieldForSeconds(1.8f)),
                Fiber.Factory.StartNew(() => new YieldForSeconds(1.9f)),
                Fiber.Factory.StartNew(() => new YieldForSeconds(1.4f))
            };

            FiberScheduler.Current.Run(new Fiber(TestWhenAnyCoroutine(fibers)));
        }

        IEnumerator TestWhenAnyCoroutine(Fiber[] fibers)
        {
            // Wait
            var waitAllFiber = Fiber.WhenAny(fibers);
            yield return waitAllFiber;

            // Verify index 2 was the winner and is done
            Assert.AreEqual(fibers[2], waitAllFiber.ResultAsObject, "Fiber at index 2 was not the winner and should be");
            Assert.IsTrue(fibers[2].IsCompleted, "Fiber at index 2 should have been stopped");

            // Others should still be running
            Assert.IsTrue(fibers.Any(f => f.Status == FiberStatus.Running), "Other fibers should have been running but were not");

            // Wait 2s more
            yield return new YieldForSeconds(2);

            // Now none should be running
            Assert.IsTrue(fibers.All(f => f.IsCompleted), "No fibers should have been running");
        }

        [Test()]
        public void TestDelay()
        {
            var startTime = DateTime.Now;
            FiberScheduler.Current.Run(Fiber.Delay(2000));
            Assert.GreaterOrEqual(DateTime.Now, startTime + TimeSpan.FromSeconds(2));
        }

        bool fiber1Ran;
        bool fiber2Ran;

        [Test]
        public void TestFiberContinueWithAction()
        {
            fiber1Ran = false;
            fiber2Ran = false;

            Fiber.Factory.StartNew (() => {
                fiber1Ran = true;
            }).ContinueWith(f => fiber2Ran = true);

            FiberScheduler.Current.Run(new Fiber(TestFiberContinueWithActionCoroutine()));
        }

        IEnumerator TestFiberContinueWithActionCoroutine()
        {
            yield return new YieldForSeconds(1f);
            Assert.IsTrue(fiber1Ran);
            Assert.IsTrue(fiber2Ran);
        }

        [Test]
        public void TestParallelFor ()
        {
            ParallelOpportunistic.SupportsParallelism = true;
            int x = 0;
            ParallelOpportunistic.For (0, 10, a => ++x);
            Assert.AreEqual (x, 10);

            ParallelOpportunistic.SupportsParallelism = false;
            x = 0;
            ParallelOpportunistic.For (0, 10, a => ++x);
            Assert.AreEqual (x, 10);
        }

        [Test]
        public void TestParallelForEach ()
        {
            int [] x = new int [10];
            for (int i = 0; i < 10; ++i)
                x [i] = 1;

            int total = 0;
            ParallelOpportunistic.SupportsParallelism = true;
            ParallelOpportunistic.ForEach (x, i => total += i);
            Assert.AreEqual (total, 10);

            total = 0;
            ParallelOpportunistic.SupportsParallelism = false;
            ParallelOpportunistic.ForEach (x, i => total += i);
            Assert.AreEqual (total, 10);
        }

        [Test]
        public void TestParallelInvoke ()
        {
            int x = 0;

            Action a1 = () => {
                x += 2;
            };
            Action a2 = () => {
                x += 3;
            };

            ParallelOpportunistic.SupportsParallelism = true;
            ParallelOpportunistic.Invoke (a1, a2);
            Assert.AreEqual (x, 5);

            x = 0;
            ParallelOpportunistic.SupportsParallelism = false;
            ParallelOpportunistic.Invoke (a1, a2);
            Assert.AreEqual (x, 5);
        }
    }
}

