Unity Integration
=================
The **SpicyPixel.Threading.Unity.dll** assembly provides functionality to integrate the [Fiber](@ref SpicyPixel.Threading.Fiber) and Task features of the Concurrency Kit with the Unity coroutine scheduler.

MonoBehaviour Extensions
------------------------

### Using ConcurrentBehaviour
The [ConcurrentBehavior](@ref SpicyPixel.Threading.ConcurrentBehaviour) class extends MonoBehaviour with additional properties to make it easy to access the scheduler associated with the behaviour.

```{.cs}
public class MyBehaviour : ConcurrentBehaviour
{
  // If your code requires Awake(), always override the base
  // because this is where the schedulers are setup to ensure
  // the correct thread is selected.
  override void Awake()
  {
    base.Awake();
    // Do custom logic
  }
  
  void Update()
  {
    taskFactory.StartNew(() => DoWorkOnUnityThread()).
      ContinueWith((t) => DoMoreWorkOnUnityThread(), taskScheduler);
  }
}
```

### Managing scheduler creation
If it is not convenient to extend your behaviour from [ConcurrentBehavior](@ref SpicyPixel.Threading.ConcurrentBehaviour), you can create factory and scheduler instances by invoking one of the extension methods in [UnityTaskExtensions](@ref SpicyPixel.Threading.Tasks.UnityTaskExtensions).

```{.cs}
public class MyBehaviour : MonoBehaviour
{
  private TaskFactory customTaskFactory;
  
  void Awake()
  {
    customTaskFactory = this.CreateTaskFactory();
  }
  
  void Update()
  {
    customTaskFactory.StartNew(() => DoWorkOnUnityThread()).
      ContinueWith((t) => DoMoreWorkOnUnityThread(), customTaskFactory.Scheduler);
  }
}
```

### Shared factory
You can use [UnityTaskFactory](@ref SpicyPixel.Threading.Tasks.UnityTaskFactory) to access a shared factory instance which ultimately relies on the shared [ConcurrentBehavior](@ref SpicyPixel.Threading.ConcurrentBehaviour) instance.

```{.cs}
public class MyBehaviour : MonoBehaviour
{
  void Update()
  {
    UnityTaskFactory.Default.StartNew(() => DoWorkOnUnityThread()).
      ContinueWith((t) => DoMoreWorkOnUnityThread(), UnityTaskScheduler.Default);
  }
}
```

Synchronization Context
-----------------------
The [UnitySynchronizationContext](@ref SpicyPixel.Threading.UnitySynchronizationContext) is a convenient way to integrate with code which relies on a [SynchronizationContext](https://msdn.microsoft.com/en-us/library/system.threading.synchronizationcontext(v=vs.110).aspx) or to post operations back to the calling thread in a scheduler agnostic manner.

```{.cs}
public class MyBehaviour : MonoBehaviour
{
  void Awake()
  {
    // Install this once at startup in a single behaviour to
    // initialize the context for the main thread.
    SynchronizationContext.SetSynchronizationContext(
		  UnitySynchronizationContext.SharedInstance);
  }
  
  // This example method might be invoked by something from the Unity thread
  // but which needs to process results on a timer.
  void ProcessResults()
  {
    // Save the current thread's sync context before we enter the timer
    // so we can use it for callback.
    var syncContext = SynchronizationContext.Current;
    
    // Start a timer
    new Timer(TimerCallback, syncContext, TimeSpan.FromSeconds(3), TimeSpan.Zero);
  }
  
  // Do something on a timer thread
  void TimerCallback(object state)
  {
    // Get the sync context for the thread that created
    // the timer. Technically we could have referenced
    // UnitySynchronizationContext.SharedInstance since
    // we know that is the thread we want to send back to,
    // but this is the typical pattern you would use to write
    // scheduler agnostic code.
    var syncContext = state as SynchronizationContext;
   
    // We are running in a timer thread here.
    
    // Now, run something back on the thread that started the timer
    syncContext.Send((moreState) => {
      Debug.Log("Back on the Unity thread");
    }, null);
  }
}
```
