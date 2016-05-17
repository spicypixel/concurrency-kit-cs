Spicy Pixel Concurrency Kit
===========================
[Download it now in the Unity Asset Store](http://u3d.as/content/spicy-pixel/spicy-pixel-concurrency-kit) | [Get the Source Code on GitHub](https://github.com/spicypixel/concurrency-kit-cs)

The Concurrency Kit is a .NET / Mono kit that includes a port of the [Task Parallel Library](http://msdn.microsoft.com/en-us/library/dd460717.aspx) and extends it to support [Fibers](http://en.wikipedia.org/wiki/Fiber_(computer_science)), [Coroutines](http://en.wikipedia.org/wiki/Coroutine), and [Unity](http://unity3d.com/). Fibers allow code paths to execute concurrently using a single thread by leveraging the co-operative yielding behavior of coroutines.

```{.cs}
	// Start task 1
	var t1 = Task.Factory.StartNew(() => PatHead());
	 
	// Start task 2
	var t2 = Task.Factory.StartNew(() => RubTummy());
	 
	// This task will complete when t1 and t2 complete and
	// then it will continue by executing a happy dance.
	Task.WhenAll(t1, t2).ContinueWith(t3 => HappyDance());
```

Because code written in this manner is designed with concurrency in mind, tasks can run in parallel across multiple threads or as concurrent fibers on a single thread by changing out the task scheduler. This flexibility makes it easy to write and maintain portable asynchronous code that scales.

Interoperability
----------------
Use the feature rich **asynchronous task model** in your designs.

```{.cs}
	public class HttpClient {
	  public Task<HttpResponseMessage> GetAsync(
	    string requestUri);
	 
	  public Task<HttpResponseMessage> PostAsync(
	    string requestUri,
	    HttpContent content);
	}
```

Usability
---------
* Start a background task using the thread pool and **complete the operation on the main thread**

* Declaratively schedule workflows with **chained asynchronous tasks** and **anonymous delegates**

```{.cs}
	Task.Factory.StartNew(() => DoSomethingFromThreadPool()).
	  ContinueWith(lastTask => DoSomethingFromMainThread(), 
	  mainThreadScheduler);
```

* **Coordinate** between concurrently executing tasks

```{.cs}
	Task.Factory.ContinueWhenAny(tasksToRun, 
	  winner => print("The winner is: " + winner));
```

* Easily **cancel tasks** in progress

```{.cs}
	CancellationTokenSource tokenSource = new CancellationTokenSource();
	void Start()
	{
	  Task.Factory.StartNew(() => DoSomething(), tokenSource.Token);
	}
	void OnClick()
	{
	  tokenSource.Cancel();
	}
```

Performance
-----------
* Leverage **multiple CPU cores** for maximum throughput
* Maximize individual thread usage with **co-operative multitasking** and task inlining
* **Control how tasks are scheduled** and the level of concurrency

Productivity
------------
* Write more **maintainable**, more **performant** asynchronous code

Learn More
----------
Learn more by reading the [Conceptual Documentation](Docs/overview.md) or [API Reference](http://spicypixel.com/developer/concurrency-kit/api-reference/).

See [Building the Kit](Docs/build.md) for information on compiling.

---
Copyright (c) 2012-2014 [Spicy Pixel, Inc.](http://spicypixel.com)