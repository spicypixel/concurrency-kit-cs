Learn
=====

Task Parallel Library
---------------------
If you are new to the Task Parallel Library, please see this overview by Sacha Barber on [Code Project](http://www.codeproject.com/Articles/152765/Task-Parallel-Library-1-of-n) or the reference documentation on [MSDN](http://msdn.microsoft.com/en-us/library/dd537609(v=vs.100).aspx).

API Reference
-------------
The [Concurrency Kit API Reference](http://spicypixel.com/developer/concurrency-kit/api-reference/) is a good place to start if you want to see what is in the library and learn more about how fibers and coroutines are supported.

Tasks vs. Unity Coroutines
--------------------------
Unity has [built-in support](http://unity3d.com/support/documentation/ScriptReference/index.Coroutines_26_Yield.html) for coroutines and so you may be wondering how tasks are different. This section calls out a few key distinctions.

### Declaration and Execution
Starting a coroutine in Unity requires one to define an explicit method for the routine and then initiate it with [MonoBehaviour.StartCoroutine](http://unity3d.com/support/documentation/ScriptReference/MonoBehaviour.StartCoroutine.html).

```{.cs}
using UnityEngine;
using System.Collections;

public class example : MonoBehaviour {
    void Start() {
        print("Starting " + Time.time);
        StartCoroutine(WaitAndPrint(2.0F));
        print("Before WaitAndPrint Finishes " + Time.time);
    }
    IEnumerator WaitAndPrint(float waitTime) {
        yield return new WaitForSeconds(waitTime);
        print("WaitAndPrint " + Time.time);
    }
}
```

Note how the WaitAndPrint method requires the special IEnumerator return type, the signature of all coroutines. Using tasks, the same example could be written as follows.

```{.cs}
using UnityEngine;
using System.Collections;

public class example : ConcurrentBehaviour {
    void Start() {
        print("Starting " + Time.time);
        taskFactory.StartNew(WaitAndPrint(2.0F));
        print("Before WaitAndPrint Finishes " + Time.time);
    }
    IEnumerator WaitAndPrint(float waitTime) {
        yield return new WaitForSeconds(waitTime);
        print("WaitAndPrint " + Time.time);
    }
}
```

As you can see, only a minimal change is required to use an existing coroutine as a task with the Concurrency Kit. But, these are some of the benefits you get when doing so. Because the TaskFactory.StartNew method returns a Task you can:

 * Wait from any other thread for the task to complete using [Task.Wait](http://msdn.microsoft.com/en-us/library/dd235635.aspx)

 * Continue execution based on multiple tasks using [Task.WaitAny](http://msdn.microsoft.com/en-us/library/dd270672) or [Task.WaitAll](http://msdn.microsoft.com/en-us/library/dd270695)

 * Check both the [completion status](http://msdn.microsoft.com/en-us/library/system.threading.tasks.taskstatus.aspx) and the [result](http://msdn.microsoft.com/en-us/library/dd321405) of a task

Additionally, you can pass in a [CancellationToken](http://msdn.microsoft.com/en-us/library/system.threading.cancellationtoken.aspx) in order to cancel a specific instance of an executing task or a group of executing tasks. Unity only supports stopping all instances of a coroutine using [MonoBehaviour.StopCoroutine](http://unity3d.com/support/documentation/ScriptReference/MonoBehaviour.StopCoroutine.html) or [MonoBehaviour.StopAllCoroutines](http://unity3d.com/support/documentation/ScriptReference/MonoBehaviour.StopAllCoroutines.html) which in some circumstances isn't fine grained enough.

### Anonymous Methods
Sometimes it is more convenient to write a workflow using anonymous methods instead of declaring a coroutine, especially for simple property changes. Here is the same example rewritten to use an anonymous method instead of an explicitly declared one.

```{.cs}
using UnityEngine;
using System.Collections;

public class example : ConcurrentBehaviour {
    void Start() {
        print("Starting " + Time.time);
        taskFactory.StartNew(new YieldForSeconds(2.0F)).
            ContinueWith(lastTask => print("WaitAndPrint " + Time.time),
            taskScheduler);
        print("Before WaitAndPrint Finishes " + Time.time);
    }
}
```

Tasks also allow invocation of standard methods and functions that don't have an **IEnumerator** return type such as the **print** method above.

### Portability
Unity coroutines allow one to write asynchronous code, but that code is not portable outside of the Unity framework because of the dependency on the Unity coroutine scheduler. If you are writing or using a library that needs to operate in a Unity environment but also in an external environment such as for tools, framework, or cross-platform development, the Task Parallel Library is the standard way to expose asynchronous behavior in your public interface.

Advanced Uses
-------------
Tasks make it easy to run functions on another thread and to run logic in parallel. Here are some of the more advanced things you can do with them:

 * **AI:** Perform path or planning searches in parallel and select either the first to complete or the best from multiple options when all searches complete

 * **AI:** Run high-level agent behaviors across multiple threads and optionally at a different frequency than the Unity run loop for more control over scalability

 * **Physics: ** Calculate multiple ballistic trajectories to zero in on a solution

 * **Networking:** Stream multiple assets in parallel from a remote site

 * **Networking:** Asynchronously coordinate a complex sequence of events such as NAT type discovery, matchmaking on a master server, connection management, fallbacks, and timeout handling

One caveat to working with multiple threads and Unity is that much like working with a UI framework, all access to Unity objects must be made from the main Unity thread. This makes it necessary to adopt a multi-threaded architecture like a producer / consumer model in order to safely run operations in parallel and access or marshal game state across threads.