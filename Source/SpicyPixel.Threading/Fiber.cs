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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SpicyPixel.Threading
{
    /// <summary>
    /// A Fiber is a lightweight means of scheduling work that enables multiple
    /// units of processing to execute concurrently by co-operatively
    /// sharing execution time on a single thread. Fibers are also known
    /// as "micro-threads" and can be implemented using programming language
    /// facilities such as "coroutines".
    /// </summary>
    /// <remarks>
    /// <para>
    /// Fibers simplify many concurrency issues generally associated with
    /// multithreading because a given fiber has complete control over
    /// when it yields execution to another fiber. A fiber does not need
    /// to manage resource locking or handle changing data in the same way as a 
    /// thread does because access to a resource is never preempted by 
    /// another fiber without co-operation.
    /// </para>
    /// <para>
    /// Fibers can improve performance in certain applications with concurrency
    /// requirements. Because many fibers can run on a thread, this can relieve 
    /// pressure on precious resources in the thread pool and reduce latency.
    /// Additionally, some applications have concurrent, interdependent processes 
    /// that naturally lend themselves to co-operative scheduling which can
    /// result in greater efficiency when the application manages the context
    /// switch instead of a pre-emptive scheduler.
    /// </para>
    /// <para>
    /// Fibers can also be a convenient way to express a state machine. The 
    /// master fiber implementing the machine can test state conditions, start 
    /// new fibers for state actions, yield to an action fiber until it completes,
    /// and then handle the transition out of the state and into a new state.
    /// </para>
    /// </remarks>
    public partial class Fiber
    {
        /// <summary>
        /// The currently executing fiber on the thread.
        /// </summary>
        [ThreadStatic]
        private static Fiber currentFiber;

        private static FiberFactory fiberFactory = new FiberFactory();
				
        /// <summary>
        /// The next unique identifier.
        /// </summary>
        private static int nextId = 0;
		
        private IEnumerator coroutine;
        private Stack<IEnumerator> nestedCoroutines;
		
        private Action action;
        private Action<object> actionObject;
        private object objectState;
		
        private Func<FiberInstruction> func;
        private Func<object, FiberInstruction> funcObject;
		
        private FiberScheduler scheduler;
        private int status = (int)FiberStatus.Created;
        // must be int to work with Interlocked on Mono
        private IDictionary<string, object> properties;

        internal CancellationToken cancelToken;
        internal Fiber antecedent;
        internal Queue<FiberContinuation> continuations;

        /// <summary>
        /// Gets the currently executing fiber on this thread.
        /// </summary>
        /// <value>
        /// The currently executing fiber on this thread.
        /// </value>
        public static Fiber CurrentFiber { 
            get {
                return currentFiber;
            }
            private set {
                currentFiber = value;
            }
        }

        /// <summary>
        /// Gets the default factory for creating fibers.
        /// </summary>
        /// <value>The factory.</value>
        public static FiberFactory Factory {
            get {
                return fiberFactory;
            }
        }

        static int CheckTimeout(TimeSpan timeout)
        {
            try {
                return checked ((int)timeout.TotalMilliseconds);
            } catch (System.OverflowException) {
                throw new ArgumentOutOfRangeException("timeout");
            }
        }

        /// <summary>
        /// Gets user-defined properties associated with the fiber.
        /// </summary>
        /// <remarks>
        /// Similar to thread local storage, callers may associate
        /// data with a fiber. A FiberStorage&lt;T&gt; class
        /// could retrieve data from the this property collection
        /// on the <see cref="CurrentFiber"/>.
        /// 
        /// Schedulers may also use this storage to associate
        /// additional data needed to perform scheduling operations.
        /// </remarks>
        /// <value>
        /// The properties.
        /// </value>
        public IDictionary<string, object> Properties {
            get { 
                if (properties == null)
                    properties = new Dictionary<string, object>();

                return properties; 
            }
        }

        /// <summary>
        /// Gets the scheduler used to start the fiber.
        /// </summary>
        /// <value>
        /// The scheduler that was used to start the fiber.
        /// </value>
        internal FiberScheduler Scheduler { 
            get { return scheduler; } 
            set { scheduler = value; }
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the result of the fiber's execution as an object.
        /// </summary>
        /// <value>The result of the fiber's execution as an object.</value>
        public object ResultAsObject { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether this instance is canceled.
        /// </summary>
        /// <value><c>true</c> if this instance is canceled; otherwise, <c>false</c>.</value>
        public bool IsCanceled {
            get {
                return (FiberStatus)status == FiberStatus.Canceled;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is completed.
        /// </summary>
        /// <remarks>
        /// "Completed" means:
        ///  * FiberStatus.RanToCompletion
        ///  * FiberStatus.Canceled
        ///  * FiberStatus.Faulted
        /// </remarks>
        /// <value><c>true</c> if this instance is completed; otherwise, <c>false</c>.</value>
        public bool IsCompleted {
            get {
                return (FiberStatus)status >= FiberStatus.RanToCompletion;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is faulted.
        /// </summary>
        /// <remarks>
        /// See `Fiber.Exception` for the exception causing the fault.
        /// </remarks>
        /// <value><c>true</c> if this instance is faulted; otherwise, <c>false</c>.</value>
        public bool IsFaulted {
            get {
                return (FiberStatus)status == FiberStatus.Faulted;
            }
        }

        /// <summary>
        /// Gets the exception that led to the Faulted state.
        /// </summary>
        /// <value>The exception.</value>
        public Exception Exception {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the state of the fiber.
        /// </summary>
        /// <value>
        /// The state of the fiber (Unstarted, Running, Stopped).
        /// </value>
        public FiberStatus Status { 
            get { return (FiberStatus)status; }
            set { status = (int)value; }
        }

        /// <summary>
        /// Gets the antecedent, which is the fiber that this fiber was dependent upon
        /// for starting execution.
        /// </summary>
        /// <remarks>
        /// The antecedent provides access to a prior result through `GetAsResultObject()`.
        /// </remarks>
        /// <value>The antecedent.</value>
        public Fiber Antecedent {
            get { return antecedent; }
        }

        /// <summary>
        /// Gets the cancellation token for the Fiber.
        /// </summary>
        /// <remarks>
        /// The token is made available because coroutines don't have a convenient way
        /// to get at it otherwise. This way, an executing coroutine can:
        /// 
        /// ```
        /// Fiber.CurrentFiber.CancellationToken.ThrowIfCancellationRequested();
        /// ```
        /// </remarks>
        /// <value>The cancellation token.</value>
        public CancellationToken CancellationToken {
            get { return cancelToken; }
        }

        /// <summary>
        /// Run to finish transitioning to Completed by execution continuations.
        /// </summary>
        private void FinishCompletion()
        {
            if (continuations == null)
                return;

            while (true) {
                if (continuations.Count == 0)
                    return;
                
                var continuation = continuations.Dequeue();
                continuation.Execute();
            }
        }

        /// <summary>
        /// Called by a continuation to cancel the task and trigger other continuations.
        /// </summary>
        /// <returns><c>true</c> if this instance cancel continuation; otherwise, <c>false</c>.</returns>
        internal void CancelContinuation()
        {
            status = (int)FiberStatus.Canceled;
            FinishCompletion();
        }

        /// <summary>
        /// Gets the thread unique identifier for the fiber.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public int Id { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.Fiber"/> class.
        /// </summary>
        /// <param name='coroutine'>
        /// A couroutine to execute on the fiber.
        /// </param>
        public Fiber(IEnumerator coroutine) : this(coroutine, CancellationToken.None)
        {			
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.Fiber"/> class.
        /// </summary>
        /// <param name='coroutine'>
        /// A couroutine to execute on the fiber.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Fiber(IEnumerator coroutine, CancellationToken cancellationToken)
        {           
            Id = nextId++;
            this.coroutine = coroutine;
            this.cancelToken = cancellationToken;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.Fiber"/> class.
        /// </summary>
        /// <param name='action'>
        /// A non-blocking action to execute on the fiber.
        /// </param>
        public Fiber(Action action) : this(action, CancellationToken.None)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.Fiber"/> class.
        /// </summary>
        /// <param name='action'>
        /// A non-blocking action to execute on the fiber.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Fiber(Action action, CancellationToken cancellationToken)
        {
            Id = nextId++;
            this.action = action;
            this.cancelToken = cancellationToken;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.Fiber"/> class.
        /// </summary>
        /// <param name='action'>
        /// A non-blocking action to execute on the fiber.
        /// </param>
        /// <param name='state'>
        /// State to pass to the action.
        /// </param>
        public Fiber(Action<object> action, object state) : this(action, state, CancellationToken.None)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.Fiber"/> class.
        /// </summary>
        /// <param name='action'>
        /// A non-blocking action to execute on the fiber.
        /// </param>
        /// <param name='state'>
        /// State to pass to the action.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Fiber(Action<object> action, object state, CancellationToken cancellationToken)
        {
            Id = nextId++;
            this.actionObject = action;
            this.objectState = state;
            this.cancelToken = cancellationToken;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.Fiber"/> class.
        /// </summary>
        /// <param name='func'>
        /// A non-blocking function that returns a <see cref="FiberInstruction"/> when complete.
        /// </param>
        public Fiber(Func<FiberInstruction> func) : this(func, CancellationToken.None)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.Fiber"/> class.
        /// </summary>
        /// <param name='func'>
        /// A non-blocking function that returns a <see cref="FiberInstruction"/> when complete.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Fiber(Func<FiberInstruction> func, CancellationToken cancellationToken)
        {
            Id = nextId++;
            this.func = func;
            this.cancelToken = cancellationToken;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.Fiber"/> class.
        /// </summary>
        /// <param name='func'>
        /// A non-blocking function that returns a <see cref="FiberInstruction"/> when complete.
        /// </param>
        /// <param name='state'>
        /// State to pass to the function.
        /// </param>
        public Fiber(Func<object, FiberInstruction> func, object state) : this(func, state, CancellationToken.None)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.Fiber"/> class.
        /// </summary>
        /// <param name='func'>
        /// A non-blocking function that returns a <see cref="FiberInstruction"/> when complete.
        /// </param>
        /// <param name='state'>
        /// State to pass to the function.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Fiber(Func<object, FiberInstruction> func, object state, CancellationToken cancellationToken)
        {
            Id = nextId++;
            this.funcObject = func;
            this.objectState = state;
            this.cancelToken = cancellationToken;
        }

        /// <summary>
        /// Start executing the fiber using the default scheduler on the thread.
        /// </summary>
        public void Start()
        {
            Start(FiberScheduler.Current);
        }

        /// <summary>
        /// Start executing the fiber using the specified scheduler.
        /// </summary>
        /// <remarks>
        /// This method is safe to call from any thread even if different
        /// than the scheduler execution thread.
        /// </remarks>
        /// <param name='scheduler'>
        /// The scheduler to start the fiber on.
        /// </param>
        public void Start(FiberScheduler scheduler)
        {	
            // It would be unusual to attempt to start a Fiber more than once,
            // but to be safe and to support calling from any thread
            // use Interlocked with boxing on the enum.
			
            var originalState = (FiberStatus)Interlocked.CompareExchange(ref status, (int)FiberStatus.WaitingToRun, (int)FiberStatus.Created);
            if (originalState != FiberStatus.Created) {
                originalState = (FiberStatus)Interlocked.CompareExchange(ref status, (int)FiberStatus.WaitingToRun, (int)FiberStatus.WaitingForActivation);
                if (originalState != FiberStatus.WaitingForActivation) {
                    throw new InvalidOperationException("A fiber cannot be started again once it has begun running or has completed.");
                }
            }
			
            this.scheduler = scheduler;
            ((IFiberScheduler)this.scheduler).QueueFiber(this);
        }

        /// <summary>
        /// Executes the fiber until it ends or yields.
        /// </summary>
        /// <returns>
        /// A fiber instruction to be processed by the scheduler.
        /// </returns>
        internal FiberInstruction Execute()
        {
            // Sanity check the scheduler. Since this is a scheduler
            // only issue, this test happens first before execution
            // and before allowing an exception handler to take over.
            if (IsCompleted)
                throw new InvalidOperationException("An attempt was made to execute a completed Fiber. This indicates a logic error in the scheduler.");

            // Setup thread globals with this scheduler as the owner.
            // The sync context is also setup when the scheduler changes.
            // Setting the sync context isn't a simple assign in .NET
            // so don't do this until needed.
            //
            // Doing this setup in Execute() frees derived schedulers from the 
            // burden of setting up the sync context or scheduler. It also allows the
            // current scheduler to change on demand when there is more than one
            // scheduler running per thread. 
            // 
            // For example, each MonoBehaviour in Unity is assigned its own
            // scheduler since the behavior determines the lifetime of tasks.
            // The active scheduler then can vary depending on which behavior is 
            // currently executing tasks. Unity will decide outside of this framework 
            // which behaviour to execute in which order and therefore which 
            // scheduler is active. The active scheduler in this case can 
            // only be determined at the time of fiber execution.
            //
            // Unlike CurrentFiber below, this does not need to be stacked
            // once set because the scheduler will never change during fiber
            // execution. Only something outside of the scheduler would
            // change the scheduler.
            if (FiberScheduler.Current != scheduler)
                FiberScheduler.SetCurrentScheduler(scheduler, true);

            // Push the current fiber onto the stack and pop it in finally.
            // This must be stacked because the fiber may spawn another
            // fiber which the scheduler may choose to inline which would result
            // in a new fiber temporarily taking its place as current.
            var lastFiber = Fiber.CurrentFiber;
            Fiber.CurrentFiber = this;

            try {
                // The loop will execute until hitting a valid instruction.
                // Specifically, nested coroutines need to execute this until
                // a yield instruction is hit.
                while (true) {
                    object result = null;

                    // Execute the coroutine or action
                    if (nestedCoroutines != null && nestedCoroutines.Count > 0) {
                        var nestedCoroutine = nestedCoroutines.Peek();
                        if (nestedCoroutine.MoveNext()) {
                            result = nestedCoroutine.Current;

                            // A stop instruction in a nested coroutine
                            // means stop that execution, not the parent
                            if (result is StopInstruction) {
                                nestedCoroutines.Pop();
                                continue;
                            }
                        } else {
                            // Nested routine finished
                            nestedCoroutines.Pop();
                            continue;
                        }
                    } else if (coroutine != null) {
                        // Execute coroutine
                        if (coroutine.MoveNext()) {
                            // Get result of execution
                            result = coroutine.Current;

                            // If the coroutine returned a stop directly
                            // the fiber still needs to process it
                            if (result is StopInstruction)
                                Stop(FiberStatus.RanToCompletion);
                        } else {
                            // Coroutine finished executing
                            result = Stop(FiberStatus.RanToCompletion);
                        }
                    } else if (action != null) {
                        // Execute action
                        action();
						
                        // Action finished executing
                        result = Stop(FiberStatus.RanToCompletion);
                    } else if (actionObject != null) {
                        // Execute action
                        actionObject(objectState);
	                    
                        // Action finished executing
                        result = Stop(FiberStatus.RanToCompletion);
                    } else if (func != null) {
                        result = func();
                        func = null;

                        if (result is StopInstruction)
                            Stop(FiberStatus.RanToCompletion);
                    } else if (funcObject != null) {
                        result = funcObject(objectState);
                        funcObject = null;

                        if (result is StopInstruction)
                            Stop(FiberStatus.RanToCompletion);
                    } else {
                        // Func execution nulls out the function
                        // so the scheduler will return to here
                        // when complete and then stop.
                        result = Stop(FiberStatus.RanToCompletion);
                    }
					
                    // Treat null as a special case
                    if (result == null)
                        return FiberInstruction.YieldToAnyFiber;

                    // Return instructions or throw if invalid
                    var instruction = result as FiberInstruction;
                    if (instruction == null) {
                        // If the result was an enumerator there is a nested coroutine to execute
                        if (result is IEnumerator) {
                            // Lazy create
                            if (nestedCoroutines == null)
                                nestedCoroutines = new Stack<IEnumerator>();
							
                            // Push the nested coroutine onto the stack
                            nestedCoroutines.Push(result as IEnumerator);

                            // For performance we execute nested coroutines until hitting
                            // an actual instruction at the deepest nest level.
                            result = null;
                            continue;
                        } else if (result is Fiber) {
                            // Convert fibers into yield instructions
                            instruction = new YieldUntilComplete(result as Fiber);
                        } else {
                            // Pass through other values
                            return new ObjectInstruction(result);
                        }
                    }

                    if (instruction is FiberResult) {
                        ResultAsObject = ((FiberResult)instruction).Result;
                        result = Stop(FiberStatus.RanToCompletion);
                    }

                    // Verify same scheduler	
                    if (instruction is YieldUntilComplete && ((YieldUntilComplete)instruction).Fiber.Scheduler != FiberScheduler.Current)
                        throw new InvalidOperationException("Currently only fibers belonging to the same scheduler may be yielded to. FiberScheduler.Current = "
                        + (FiberScheduler.Current == null ? "null" : FiberScheduler.Current.ToString())
                        + ", Fiber.Scheduler = " + (((YieldUntilComplete)instruction).Fiber.Scheduler == null ? "null" : ((YieldUntilComplete)instruction).Fiber.Scheduler.ToString()));
					
                    var yieldToFiberInstruction = instruction as YieldToFiber;									
                    if (yieldToFiberInstruction != null) {
                        // Start fibers yielded to that aren't running yet
                        Interlocked.CompareExchange(ref yieldToFiberInstruction.Fiber.status, (int)FiberStatus.WaitingToRun, (int)FiberStatus.Created);
                        var originalState = (FiberStatus)Interlocked.CompareExchange(ref yieldToFiberInstruction.Fiber.status, (int)FiberStatus.Running, (int)FiberStatus.WaitingToRun);
                        if (originalState == FiberStatus.WaitingToRun)
                            yieldToFiberInstruction.Fiber.scheduler = scheduler;
					
                        // Can't switch to completed fibers
                        if (yieldToFiberInstruction.Fiber.IsCompleted)
                            throw new InvalidOperationException("An attempt was made to yield to a completed fiber.");
						
                        // Verify scheduler
                        if (yieldToFiberInstruction.Fiber.Scheduler != FiberScheduler.Current)
                            throw new InvalidOperationException("Currently only fibers belonging to the same scheduler may be yielded to. FiberScheduler.Current = "
                            + (FiberScheduler.Current == null ? "null" : FiberScheduler.Current.ToString())
                            + ", Fiber.Scheduler = " + (yieldToFiberInstruction.Fiber.Scheduler == null ? "null" : yieldToFiberInstruction.Fiber.Scheduler.ToString()));
                    }
						
                    return instruction;
                }
            } catch (System.Threading.OperationCanceledException cancelException) {
                // Handle as proper cancellation only if the token matches.
                // Otherwise treat it as a fault.
                if (cancelException.CancellationToken == cancelToken) {
                    this.Exception = null;
                    return Stop(FiberStatus.Canceled);
                } else {
                    this.Exception = cancelException;
                    return Stop(FiberStatus.Faulted);
                }
            } catch (Exception fiberException) {
                this.Exception = fiberException;
                return Stop(FiberStatus.Faulted);
            } finally {	
                // Pop the current fiber
                Fiber.CurrentFiber = lastFiber;
            }
        }

        private StopInstruction Stop(FiberStatus finalStatus)
        {
            // Interlocked isn't necessary because we don't need
            // the initial value (stops are final). They are also
            // only called by the scheduler thread during Execute().
            status = (int)finalStatus;
            FinishCompletion();
            return FiberInstruction.Stop;
        }
    }
}