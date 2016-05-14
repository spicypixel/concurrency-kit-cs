using System;
using NUnit.Framework;
using System.Threading;
using System.Collections;

namespace SpicyPixel.Threading.Test
{
    [TestFixture]
    public class FiberCancellationTests
    {
        FiberScheduler scheduler;

        public FiberCancellationTests()
        {            
        }

        [TestFixtureSetUp]
        public void Init()
        {
            scheduler = SystemFiberScheduler.StartNew();
        }

        [TestFixtureTearDown]
        public void Dispose()
        {
            scheduler.Dispose();
        }

        /// <summary>
        /// Waits until cancellation occurs.
        /// </summary>
        /// <returns>The and cancel.</returns>
        /// <param name="token">Token.</param>
        IEnumerator WaitForCancel(CancellationToken token)
        {
            while (true) {
                token.ThrowIfCancellationRequested();
                yield return FiberInstruction.YieldToAnyFiber;
            }
        }

        /// <summary>
        /// Tests that canceling by throwing does not result in a fault.
        /// </summary>
        [Test]
        public void TestCanceledResult()
        {
            FiberScheduler.Current.Run(new Fiber(TestCanceledResultCoroutine()));
        }

        IEnumerator TestCanceledResultCoroutine()
        {
            CancellationTokenSource cts1 = new CancellationTokenSource(1000);

            var fiber = Fiber.Factory.StartNew(WaitForCancel(cts1.Token), cts1.Token);
            yield return fiber;

            Assert.AreEqual(fiber.Status, FiberStatus.Canceled, "Expected Canceled");
            Assert.IsNull(fiber.Exception, "Expected no exception");
        }

        /// <summary>
        /// Tests that throwing a different cancellation token than the one
        /// passed to the fiber results in a fault.
        /// </summary>
        [Test]
        public void TestDifferentTokensFail()
        {
            FiberScheduler.Current.Run(new Fiber(TestDifferentTokensFailCoroutine()));
        }

        IEnumerator TestDifferentTokensFailCoroutine()
        {
            // Create 2 different sources
            CancellationTokenSource cts1 = new CancellationTokenSource(1000);
            CancellationTokenSource cts2 = new CancellationTokenSource();

            var fiber = Fiber.Factory.StartNew(WaitForCancel(cts1.Token), cts2.Token);
            yield return fiber;

            Assert.AreEqual(fiber.Status, FiberStatus.Faulted, "Expected Faulted");
            Assert.IsNotNull(fiber.Exception, "Expected an exception");
            Assert.IsInstanceOfType(typeof(System.OperationCanceledException), fiber.Exception, 
                "Expected OperationCanceledException");
        }
    }
}