Fibers
======

A [Fiber](@ref SpicyPixel.Threading.Fiber) is a lightweight means of scheduling work that enables multiple units of processing to execute concurrently by co-operatively sharing execution time on a single thread. Fibers are also known as "micro-threads" and can be implemented using programming language facilities such as "coroutines".

Fibers simplify many concurrency issues generally associated with multithreading because a given fiber has complete control over when it yields execution to another fiber. A fiber does not need to manage resource locking or handle changing data in the same way as a thread does because access to a resource is never preempted by another fiber without co-operation.

Fibers can improve performance in certain applications with concurrency requirements. Because many fibers can run on a thread, this can relieve pressure on precious resources in the thread pool and reduce latency.

Additionally, some applications have concurrent, interdependent processes that naturally lend themselves to co-operative scheduling which can result in greater efficiency when the application manages the context switch instead of a pre-emptive scheduler.

Fibers can also be a convenient way to express a state machine. The master fiber implementing the machine can test state conditions, start new fibers for state actions, yield to an action fiber until it completes, and then handle the transition out of the state and into a new state.

Starting a fiber
----------------
A fiber can be created and started on the default scheduler for the thread as follows.

```{.cs}
Fiber.StartNew(() => DoTask());
```

It is also possible to more precisely control startup and scheduler options.

```{.cs}
var fiber = new Fiber(() => DoTask()); // create the fiber
fiber.Start(myCustomScheduler); // queue for execution by a custom scheduler
```
Using coroutines
----------------
A coroutine is a function that can yield execution to another function and this allows multiple functions to execute concurrently on the same thread by co-operatively yielding execution time.

Coroutines must return an IEnumerator type and have one or more yield statements that passes a yield instruction back to the fiber scheduler.

```{.cs}
void Main()
{
	Fiber.StartNew(FadeOutCoroutine());
}

IEnumerator FadeOutCoroutine()
{
	var totalTime = 4f;
	var currentTime = totalTime;
	while(currentTime > 0f)
	{
		setAlpha(currentTime / totalTime); // fade out
		currentTime -= Time.deltaTime;
		yield return FiberInstruction.YieldToAnyFiber;
	}
}
```

Waiting for completion
----------------------
One fiber can wait for completion of another by yielding the [YieldUntilComplete](@ref SpicyPixel.Threading.YieldUntilComplete) instruction returned by [Start](@ref SpicyPixel.Threading.Fiber.Start).

```{.cs}
void Main()
{
	Fiber.StartNew(Fiber1());
}

IEnumerator Fiber1()
{
	Console.Out.WriteLine(“Beginning Fiber 1 and waiting for Fiber 2 to complete”);
	yield return Fiber.StartNew(Fiber2());
	Console.Out.WriteLine(“Fiber 2 has completed.);
}

IEnumerator Fiber2()
{
	// Do work
	while(!workDone)
	{
		Process();
		yield return FiberInstruction.YieldToAnyFiber;
	}
}
```

Starting a fiber scheduler
--------------------------
Fiber coordination and execution is handled by a [FiberScheduler](@ref SpicyPixel.Threading.FiberScheduler). The default system scheduler can be started on the running thread as follows.

```{.cs}
void Main()
{
	FiberScheduler.Current.Run(new Fiber(MainFiber()));
}

IEnumerator MainFiber()
{
  // do work
}
```

Starting a fiber scheduler on a new thread
------------------------------------------
It is often more convenient to start a fiber scheduler on a new thread. The [SystemFiberScheduler](@ref SpicyPixel.Threading.SystemFiberScheduler) has convenience methods to create the scheduler on a new thread and return a reference.

```{.cs}
void Main()
{
	var scheduler = SystemFiberScheduler.StartNew(new Fiber(MainFiber()));
}

IEnumerator MainFiber()
{
  // do work
}
```
