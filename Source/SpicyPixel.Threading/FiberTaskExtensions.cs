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
using System.Threading;
using System.Threading.Tasks;
using SpicyPixel.Threading;
using SpicyPixel.Threading.Tasks;

namespace SpicyPixel.Threading.Tasks
{
    /// <summary>
    /// Extends the <see cref="Task"/> and <see cref="TaskFactory"/> classes with methods to support coroutines.
    /// </summary>
    public static class FiberTaskExtensions
    {       
        /// <summary>
        /// Continues the task with a coroutine.
        /// </summary>
        /// <returns>
        /// The continued task.
        /// </returns>
        /// <param name='task'>
        /// Task to continue.
        /// </param>
        /// <param name='coroutine'>
        /// The coroutine to continue with.
        /// </param>
        public static Task ContinueWith (this Task task, IEnumerator coroutine)
        {
            return ContinueWith (task, coroutine, TaskContinuationOptions.None);
        }
        
        /// <summary>
        /// Continues the task with a coroutine.
        /// </summary>
        /// <returns>
        /// The continued task.
        /// </returns>
        /// <param name='task'>
        /// Task to continue.
        /// </param>
        /// <param name='coroutine'>
        /// The coroutine to continue with.
        /// </param>
        /// <param name='continuationOptions'>
        /// Continuation options.
        /// </param>
        public static Task ContinueWith (this Task task, IEnumerator coroutine, TaskContinuationOptions continuationOptions)
        {
            return ContinueWith (task, coroutine, CancellationToken.None, continuationOptions, TaskScheduler.Current);
        }
        
        /// <summary>
        /// Continues the task with a coroutine.
        /// </summary>
        /// <returns>
        /// The continued task.
        /// </returns>
        /// <param name='task'>
        /// Task to continue.
        /// </param>
        /// <param name='coroutine'>
        /// The coroutine to continue with.
        /// </param>
        /// <param name='cancellationToken'>
        /// Cancellation token.
        /// </param>
        public static Task ContinueWith (this Task task, IEnumerator coroutine, CancellationToken cancellationToken)
        {
            return ContinueWith (task, coroutine, cancellationToken, TaskContinuationOptions.None, TaskScheduler.Current);
        }
        
        /// <summary>
        /// Continues the task with a coroutine.
        /// </summary>
        /// <returns>
        /// The continued task.
        /// </returns>
        /// <param name='task'>
        /// Task to continue.
        /// </param>
        /// <param name='coroutine'>
        /// The coroutine to continue with.
        /// </param>
        /// <param name='scheduler'>
        /// Scheduler.
        /// </param>
        public static Task ContinueWith (this Task task, IEnumerator coroutine, TaskScheduler scheduler)
        {
            return ContinueWith (task, coroutine, CancellationToken.None, TaskContinuationOptions.None, scheduler);
        }
        
        /// <summary>
        /// Continues the task with a coroutine.
        /// </summary>
        /// <returns>
        /// The continued task.
        /// </returns>
        /// <param name='task'>
        /// Task to continue.
        /// </param>
        /// <param name='coroutine'>
        /// The coroutine to continue with.
        /// </param>
        /// <param name='cancellationToken'>
        /// Cancellation token.
        /// </param>
        /// <param name='continuationOptions'>
        /// Continuation options.
        /// </param>
        /// <param name='scheduler'>
        /// Scheduler to use when scheduling the task.
        /// </param>
        public static Task ContinueWith (this Task task, IEnumerator coroutine, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (coroutine == null)
                throw new ArgumentNullException ("coroutine");
            if (scheduler == null)
                throw new ArgumentNullException ("scheduler");
            if (!(scheduler is FiberTaskScheduler))
                throw new ArgumentException ("The scheduler for a YieldableTask must be a FiberTaskScheduler", "scheduler");
            
            // This creates a continuation that runs on the default scheduler (e.g. ThreadPool)
            // where it's OK to wait on a child task to complete. The child task is scheduled
            // on the given scheduler and attached to the parent.
            
            //var outerScheduler = TaskScheduler.Current;
            //if(outerScheduler is MonoBehaviourTaskScheduler)
            //  outerScheduler = TaskScheduler.Default;
            
            var outerScheduler = TaskScheduler.Default;
            
            return task.ContinueWith((Task antecedent) => {
                var yieldableTask = new YieldableTask(coroutine, cancellationToken, TaskCreationOptions.AttachedToParent);
                yieldableTask.Start(scheduler);
            }, cancellationToken, continuationOptions, outerScheduler);
        }
		
		/// <summary>
        /// Continues the task with a coroutine.
        /// </summary>
        /// <returns>
        /// The continued task.
        /// </returns>
        /// <param name='task'>
        /// Task to continue.
        /// </param>
        /// <param name='instruction'>
        /// The instruction to continue with.
        /// </param>
        public static Task ContinueWith (this Task task, FiberInstruction instruction)
        {
            return ContinueWith (task, instruction, TaskContinuationOptions.None);
        }
        
        /// <summary>
        /// Continues the task with a coroutine.
        /// </summary>
        /// <returns>
        /// The continued task.
        /// </returns>
        /// <param name='task'>
        /// Task to continue.
        /// </param>
        /// <param name='instruction'>
        /// The instruction to continue with.
        /// </param>
        /// <param name='continuationOptions'>
        /// Continuation options.
        /// </param>
        public static Task ContinueWith (this Task task, FiberInstruction instruction, TaskContinuationOptions continuationOptions)
        {
            return ContinueWith (task, instruction, CancellationToken.None, continuationOptions, TaskScheduler.Current);
        }
        
        /// <summary>
        /// Continues the task with a coroutine.
        /// </summary>
        /// <returns>
        /// The continued task.
        /// </returns>
        /// <param name='task'>
        /// Task to continue.
        /// </param>
        /// <param name='instruction'>
        /// The instruction to continue with.
        /// </param>
        /// <param name='cancellationToken'>
        /// Cancellation token.
        /// </param>
        public static Task ContinueWith (this Task task, FiberInstruction instruction, CancellationToken cancellationToken)
        {
            return ContinueWith (task, instruction, cancellationToken, TaskContinuationOptions.None, TaskScheduler.Current);
        }
        
        /// <summary>
        /// Continues the task with a coroutine.
        /// </summary>
        /// <returns>
        /// The continued task.
        /// </returns>
        /// <param name='task'>
        /// Task to continue.
        /// </param>
        /// <param name='instruction'>
        /// The instruction to continue with.
        /// </param>
        /// <param name='scheduler'>
        /// Scheduler.
        /// </param>
        public static Task ContinueWith (this Task task, FiberInstruction instruction, TaskScheduler scheduler)
        {
            return ContinueWith (task, instruction, CancellationToken.None, TaskContinuationOptions.None, scheduler);
        }
        
        /// <summary>
        /// Continues the task with a coroutine.
        /// </summary>
        /// <returns>
        /// The continued task.
        /// </returns>
        /// <param name='task'>
        /// Task to continue.
        /// </param>
        /// <param name='instruction'>
        /// The instruction to continue with.
        /// </param>
        /// <param name='cancellationToken'>
        /// Cancellation token.
        /// </param>
        /// <param name='continuationOptions'>
        /// Continuation options.
        /// </param>
        /// <param name='scheduler'>
        /// Scheduler to use when scheduling the task.
        /// </param>
        public static Task ContinueWith (this Task task, FiberInstruction instruction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (instruction == null)
                throw new ArgumentNullException ("instruction");
            if (scheduler == null)
                throw new ArgumentNullException ("scheduler");
            if (!(scheduler is FiberTaskScheduler))
                throw new ArgumentException ("The scheduler for a YieldableTask must be a FiberTaskScheduler", "scheduler");
            
            // This creates a continuation that runs on the default scheduler (e.g. ThreadPool)
            // where it's OK to wait on a child task to complete. The child task is scheduled
            // on the given scheduler and attached to the parent.
            
            //var outerScheduler = TaskScheduler.Current;
            //if(outerScheduler is MonoBehaviourTaskScheduler)
            //  outerScheduler = TaskScheduler.Default;
            
            var outerScheduler = TaskScheduler.Default;
            
            return task.ContinueWith((Task antecedent) => {
                var yieldableTask = new YieldableTask(instruction, cancellationToken, TaskCreationOptions.AttachedToParent);
                yieldableTask.Start(scheduler);
            }, cancellationToken, continuationOptions, outerScheduler);
        }
        
        /// <summary>
        /// Creates a new task and starts executing it.
        /// </summary>
        /// <returns>
        /// The new executing task.
        /// </returns>
        /// <param name='taskFactory'>
        /// Task factory to start with.
        /// </param>
        /// <param name='coroutine'>
        /// The coroutine to start.
        /// </param>
        public static Task StartNew (this TaskFactory taskFactory, IEnumerator coroutine)
        {
            return StartNew (taskFactory, coroutine, taskFactory.CancellationToken, taskFactory.CreationOptions, taskFactory.Scheduler);
        }
        
        /// <summary>
        /// Creates a new task and starts executing it.
        /// </summary>
        /// <returns>
        /// The new executing task.
        /// </returns>
        /// <param name='taskFactory'>
        /// Task factory to start with.
        /// </param>
        /// <param name='coroutine'>
        /// The coroutine to start.
        /// </param>
        /// <param name='cancellationToken'>
        /// Cancellation token.
        /// </param>
        public static Task StartNew (this TaskFactory taskFactory, IEnumerator coroutine, CancellationToken cancellationToken)
        {
            return StartNew (taskFactory, coroutine, cancellationToken, taskFactory.CreationOptions, taskFactory.Scheduler);
        }
        
        /// <summary>
        /// Creates a new task and starts executing it.
        /// </summary>
        /// <returns>
        /// The new executing task.
        /// </returns>
        /// <param name='taskFactory'>
        /// Task factory to start with.
        /// </param>
        /// <param name='coroutine'>
        /// The coroutine to start.
        /// </param>
        /// <param name='creationOptions'>
        /// Creation options.
        /// </param>
        public static Task StartNew (this TaskFactory taskFactory, IEnumerator coroutine, TaskCreationOptions creationOptions)
        {
            return StartNew (taskFactory, coroutine, taskFactory.CancellationToken, creationOptions, taskFactory.Scheduler);
        }

        /// <summary>
        /// Creates a new task and starts executing it.
        /// </summary>
        /// <returns>
        /// The new executing task.
        /// </returns>
        /// <param name='taskFactory'>
        /// Task factory to start with.
        /// </param>
        /// <param name='coroutine'>
        /// The coroutine to start.
        /// </param>
        /// <param name='cancellationToken'>
        /// Cancellation token.
        /// </param>
        /// <param name='creationOptions'>
        /// Creation options.
        /// </param>
        /// <param name='scheduler'>
        /// Scheduler.
        /// </param>
        public static Task StartNew (this TaskFactory taskFactory, IEnumerator coroutine, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler)
        {
            var task = new YieldableTask (coroutine, cancellationToken, creationOptions);
            task.Start (scheduler);
            return task;
        }
		
		/// <summary>
        /// Creates a new task and starts executing it.
        /// </summary>
        /// <returns>
        /// The new executing task.
        /// </returns>
        /// <param name='taskFactory'>
        /// Task factory to start with.
        /// </param>
        /// <param name='instruction'>
        /// The instruction to start.
        /// </param>
        public static Task StartNew (this TaskFactory taskFactory, FiberInstruction instruction)
        {
            return StartNew (taskFactory, instruction, taskFactory.CancellationToken, taskFactory.CreationOptions, taskFactory.Scheduler);
        }
        
        /// <summary>
        /// Creates a new task and starts executing it.
        /// </summary>
        /// <returns>
        /// The new executing task.
        /// </returns>
        /// <param name='taskFactory'>
        /// Task factory to start with.
        /// </param>
        /// <param name='instruction'>
        /// The instruction to start.
        /// </param>
        /// <param name='cancellationToken'>
        /// Cancellation token.
        /// </param>
        public static Task StartNew (this TaskFactory taskFactory, FiberInstruction instruction, CancellationToken cancellationToken)
        {
            return StartNew (taskFactory, instruction, cancellationToken, taskFactory.CreationOptions, taskFactory.Scheduler);
        }
        
        /// <summary>
        /// Creates a new task and starts executing it.
        /// </summary>
        /// <returns>
        /// The new executing task.
        /// </returns>
        /// <param name='taskFactory'>
        /// Task factory to start with.
        /// </param>
        /// <param name='instruction'>
        /// The instruction to start.
        /// </param>
        /// <param name='creationOptions'>
        /// Creation options.
        /// </param>
        public static Task StartNew (this TaskFactory taskFactory, FiberInstruction instruction, TaskCreationOptions creationOptions)
        {
            return StartNew (taskFactory, instruction, taskFactory.CancellationToken, creationOptions, taskFactory.Scheduler);
        }

        /// <summary>
        /// Creates a new task and starts executing it.
        /// </summary>
        /// <returns>
        /// The new executing task.
        /// </returns>
        /// <param name='taskFactory'>
        /// Task factory to start with.
        /// </param>
        /// <param name='instruction'>
        /// The instruction to start.
        /// </param>
        /// <param name='cancellationToken'>
        /// Cancellation token.
        /// </param>
        /// <param name='creationOptions'>
        /// Creation options.
        /// </param>
        /// <param name='scheduler'>
        /// Scheduler.
        /// </param>
        public static Task StartNew (this TaskFactory taskFactory, FiberInstruction instruction, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler)
        {
            var task = new YieldableTask (instruction, cancellationToken, creationOptions);
            task.Start (scheduler);
            return task;
        }
    }
}

