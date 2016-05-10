Concurrency Kit Overview
========================
The Concurrency Kit is a .NET/Mono kit that includes a port of the [Task Parallel Library](http://msdn.microsoft.com/en-us/library/dd460717.aspx) and extends it to support [Fibers](http://en.wikipedia.org/wiki/Fiber_(computer_science)), [Coroutines](http://en.wikipedia.org/wiki/Coroutine), and [Unity](http://unity3d.com/). Fibers allow code paths to execute concurrently using a single thread by leveraging the co-operative yielding behavior of coroutines.


```{.cs}
// Start task 1
var t1 = Task.Factory.StartNew(() => PatHead());

// Start task 2
var t2 = Task.Factory.StartNew(() => RubTummy());

// This task will complete when t1 and t2 complete and
// then it will continue by executing a happy dance.
Task.WhenAll(t1, t2).ContinueWith((t3) => HappyDance());
```

Because code written in this manner is designed with concurrency in mind, tasks can run in parallel across multiple threads or as concurrent fibers on a single thread just by changing out the task scheduler. This flexibility makes it easy to write and maintain asynchronous code that scales.

Provided Packages
-----------------
The Concurrency Kit consists of four modular packages.

![Packages](images/packages.png)

Using Asynchronous Tasks
------------------------
When using a .NET runtime earlier than v4, add a reference to the **System.Threading.dll** assembly provided by the Concurrency Kit to add support for [Tasks](http://msdn.microsoft.com/en-us/library/dd537609) and [Thread-Safe Collections](http://msdn.microsoft.com/en-us/library/dd997305.aspx) to your project.

For a full explanation of the Task programming model, please see the [Task Parallel Library](http://msdn.microsoft.com/en-us/library/dd460717) documentation on MSDN.

Using Fibers
------------
Fiber support is provided by the **SpicyPixel.Threading.dll** assembly and it includes packages for both the core fiber features as well as an asynchronous task model for fibers. 

Please see the [Fibers](Docs/fibers.md) and [Fiber Tasks](Docs/fiber-tasks.md) topics for more information.

Using Fibers in Unity
---------------------
Unity specific fiber support is provided by the **SpicyPixel.Threading.Unity.dll** assembly and it includes a [UnityFiberScheduler](@ref SpicyPixel.Threading.UnityFiberScheduler) that can schedule work as a Unity coroutine using either the core fiber model or the task model. 

The package also extends MonoBehaviour to support convenience methods for working with fibers and tasks. See [Unity Integration](Docs/unity-integration.md).

Version History
---------------
Version 1.0.4

 * Integrate latest Mono version
 * AOT fixes
 * Rename scheduler 'SharedInstance' singletons to 'Default' for consistency with TPL

Version 1.0.3

 * Support iOS by working around AOT compiler limitations
 * Add the following convenience types and members:
  * class UnitySynchronizationContext
  * class UnityTaskFactory
  * class UnityTaskScheduler
  * property UnityFiberScheduler.SharedInstance
  * property ConcurrentBehaviour.SharedInstance
 * Add Unity sample ConcurrencyKitSample

Version 1.0

 * Initial release