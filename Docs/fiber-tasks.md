Fiber Tasks
===========
A "Fiber Task" is any non-blocking task executed on a [FiberTaskScheduler](@ref SpicyPixel.Threading.Tasks.FiberTaskScheduler), but the most versatile fiber task is a [YieldableTask](@ref SpicyPixel.Threading.Tasks.YieldableTask). These tasks are designed to yield execution so that multiple tasks can execute concurrently on the same thread.
 
For a full explanation of how tasks and schedulers work, see the [Task Parallel Library](http://msdn.microsoft.com/en-us/library/dd460717.aspx) documentation on MSDN.

Creating a fiber task scheduler
-------------------------------
Creating a fiber task scheduler is a two part process: create a fiber scheduler you want to use and then wrap that scheduler in a FiberTaskScheduler.
          
```{.cs}
var taskScheduler = new FiberTaskScheduler(SystemFiberScheduler.StartNew());
```

Executing yieldable tasks
-------------------------
Once a scheduler has been created, yieldable tasks can be queued to it.

```{.cs}
void Main()
{
	// Start a task scheduler to dispatch tasks as fibers
	// on a separate thread
	var taskScheduler = new FiberTaskScheduler(SystemFiberScheduler.StartNew());

	// Start a new task, continue it with more work,
	// then wait for it to complete
	var task = Task.Factory.StartNew(FadeOutCoroutine(), 
		CancellationToken.None, TaskCreationOptions.None, 
		taskScheduler);
	task.ContinueWith(DoMoreWork(), taskScheduler).Wait();
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

IEnumerator DoMoreWork()
{
  // do work
}
```
