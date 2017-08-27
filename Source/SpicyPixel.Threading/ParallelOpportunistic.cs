using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SpicyPixel.Threading
{
    /// <summary>
    /// Opportunistically parallelize on platforms where supported.
    /// </summary>
    /// <remarks>
    /// Threads are not currently supported on web platforms, though it would
    /// be possible to create a native plugin that leverages web workers.
    /// 
    /// ```c
    /// #include &lt;emscripten.h&gt;
    /// 
    /// extern "C"
    /// {
    ///     void process (char* input)
    ///     {
    ///         worker_handle ai_worker = emscripten_create_worker ("worker.js");
    ///         emscripten_call_worker (ai_worker, "start", input, strlen (input) + 1, workerCallback, (void*)0);
    ///     }
    /// }
    /// ```
    /// </remarks>
    public static class ParallelOpportunistic
    {
        static readonly ParallelOptions ParallelOptionsDefault = new ParallelOptions ();

        static bool supportsParallelism = true;

        /// <summary>
        /// Gets or sets whether parallelism is supported.
        /// </summary>
        /// <value>The platform supports parallelism.</value>
        public static bool SupportsParallelism {
            get {
                return supportsParallelism;
            }
            set {
                supportsParallelism = value;
            }
        }

        #region For
        /// <summary>
        /// Parallel for loop.
        /// </summary>
        /// <param name="fromInclusive">From inclusive.</param>
        /// <param name="toExclusive">To exclusive.</param>
        /// <param name="body">Body.</param>
        public static ParallelLoopResult For (int fromInclusive, int toExclusive, Action<int> body)
        {
            return For (fromInclusive, toExclusive, ParallelOptionsDefault, body);
        }

        /// <summary>
        /// Parallel for loop.
        /// </summary>
        /// <param name="fromInclusive">From inclusive.</param>
        /// <param name="toExclusive">To exclusive.</param>
        /// <param name="body">Body.</param>
        public static ParallelLoopResult For (int fromInclusive, int toExclusive, Action<int, ParallelLoopState> body)
        {
            return For (fromInclusive, toExclusive, ParallelOptionsDefault, body);
        }

        /// <summary>
        /// Parallel for loop.
        /// </summary>
        /// <param name="fromInclusive">From inclusive.</param>
        /// <param name="toExclusive">To exclusive.</param>
        /// <param name="parallelOptions">Parallel options.</param>
        /// <param name="body">Body.</param>
        public static ParallelLoopResult For (int fromInclusive, int toExclusive, ParallelOptions parallelOptions, Action<int> body)
        {
            return For (fromInclusive, toExclusive, parallelOptions, (index, state) => body (index));
        }

        /// <summary>
        /// Parallel for loop.
        /// </summary>
        /// <param name="fromInclusive">From inclusive.</param>
        /// <param name="toExclusive">To exclusive.</param>
        /// <param name="parallelOptions">Parallel options.</param>
        /// <param name="body">Body.</param>
        public static ParallelLoopResult For (int fromInclusive, int toExclusive, ParallelOptions parallelOptions, Action<int, ParallelLoopState> body)
        {
            return For<object> (fromInclusive, toExclusive, parallelOptions, () => null, (i, s, l) => { body (i, s); return null; }, _ => { });
        }

        /// <summary>
        /// Parallel for loop.
        /// </summary>
        /// <param name="fromInclusive">From inclusive.</param>
        /// <param name="toExclusive">To exclusive.</param>
        /// <param name="localInit">Local init.</param>
        /// <param name="body">Body.</param>
        /// <param name="localFinally">Local finally.</param>
        /// <typeparam name="TLocal">The 1st type parameter.</typeparam>
        public static ParallelLoopResult For<TLocal> (int fromInclusive,
                                                      int toExclusive,
                                                      Func<TLocal> localInit,
                                                      Func<int, ParallelLoopState, TLocal, TLocal> body,
                                                      Action<TLocal> localFinally)
        {
            return For<TLocal> (fromInclusive, toExclusive, ParallelOptionsDefault, localInit, body, localFinally);
        }

        /// <summary>
        /// Parallel for loop.
        /// </summary>
        /// <param name="fromInclusive">From inclusive.</param>
        /// <param name="toExclusive">To exclusive.</param>
        /// <param name="parallelOptions">Parallel options.</param>
        /// <param name="localInit">Local init.</param>
        /// <param name="body">Body.</param>
        /// <param name="localFinally">Local finally.</param>
        /// <typeparam name="TLocal">The 1st type parameter.</typeparam>
        public static ParallelLoopResult For<TLocal> (int fromInclusive,
                                                      int toExclusive,
                                                      ParallelOptions parallelOptions,
                                                      Func<TLocal> localInit,
                                                      Func<int, ParallelLoopState, TLocal, TLocal> body,
                                                      Action<TLocal> localFinally)
        {
            if (SupportsParallelism) {
                return Parallel.For (fromInclusive, toExclusive, parallelOptions, localInit, body, localFinally);
            } 

            if (body == null)
                throw new ArgumentNullException ("body");
            if (localInit == null)
                throw new ArgumentNullException ("localInit");
            if (localFinally == null)
                throw new ArgumentNullException ("localFinally");
            if (parallelOptions == null)
                throw new ArgumentNullException ("options");
            if (fromInclusive >= toExclusive)
                return new ParallelLoopResult (null, true);
            
            ParallelLoopState.ExternalInfos infos = new ParallelLoopState.ExternalInfos ();

            TLocal local = localInit ();

            ParallelLoopState state = new ParallelLoopState (infos);
            CancellationToken token = parallelOptions.CancellationToken;

            try {
                for (int i = fromInclusive; i < toExclusive; ++i) {
                    if (infos.IsStopped)
                        break;

                    token.ThrowIfCancellationRequested ();

                    if (infos.LowestBreakIteration != null && infos.LowestBreakIteration > i)
                        break;

                    state.CurrentIteration = i;

                    local = body (i, state, local);
                }
            } finally {
                localFinally (local);
            }

            return new ParallelLoopResult (infos.LowestBreakIteration, !(infos.IsStopped || infos.IsExceptional));
        }
        #endregion For

        #region ForEach
        static ParallelLoopResult ForEach<TSource, TLocal> (Func<int, IList<IEnumerator<TSource>>> enumerable, ParallelOptions options,
                                                            Func<TLocal> init, Func<TSource, ParallelLoopState, TLocal, TLocal> action,
                                                            Action<TLocal> destruct)
        {
            if (SupportsParallelism) {
                return Parallel.ForEach (enumerable, options, init, action, destruct);
            }

            if (enumerable == null)
                throw new ArgumentNullException ("source");
            if (options == null)
                throw new ArgumentNullException ("options");
            if (action == null)
                throw new ArgumentNullException ("action");
            if (init == null)
                throw new ArgumentNullException ("init");
            if (destruct == null)
                throw new ArgumentNullException ("destruct");

            ParallelLoopState.ExternalInfos infos = new ParallelLoopState.ExternalInfos ();

            TLocal local = init ();
            ParallelLoopState state = new ParallelLoopState (infos);
            CancellationToken token = options.CancellationToken;

            try {
                var elements = enumerable (1) [0]; // 1 slice
                while (elements.MoveNext ()) {
                    if (infos.IsStopped || infos.IsBroken.Value)
                        break;

                    token.ThrowIfCancellationRequested ();

                    local = action (elements.Current, state, local);
                }
            } finally {
                destruct (local);
            }

            return new ParallelLoopResult (infos.LowestBreakIteration, !(infos.IsStopped || infos.IsExceptional));
        }

        /// <summary>
        /// Parallel for each loop.
        /// </summary>
        /// <returns>Loop result.</returns>
        /// <param name="source">Source.</param>
        /// <param name="body">Body.</param>
        /// <typeparam name="TSource">The 1st type parameter.</typeparam>
        public static ParallelLoopResult ForEach<TSource> (IEnumerable<TSource> source, Action<TSource> body)
        {
            if (source == null)
                throw new ArgumentNullException ("source");
            if (body == null)
                throw new ArgumentNullException ("body");

            return ForEach<TSource, object> (Partitioner.Create (source),
                                             ParallelOptions.Default,
                                             () => null,
                                             (e, s, l) => { body (e); return null; },
                                             _ => { });
        }

        /// <summary>
        /// Parallel for each loop.
        /// </summary>
        /// <returns>Loop result.</returns>
        /// <param name="source">Source.</param>
        /// <param name="body">Body.</param>
        /// <typeparam name="TSource">The 1st type parameter.</typeparam>
        public static ParallelLoopResult ForEach<TSource> (IEnumerable<TSource> source, Action<TSource, ParallelLoopState> body)
        {
            if (source == null)
                throw new ArgumentNullException ("source");
            if (body == null)
                throw new ArgumentNullException ("body");

            return ForEach<TSource, object> (Partitioner.Create (source),
                                             ParallelOptions.Default,
                                             () => null,
                                             (e, s, l) => { body (e, s); return null; },
                                             _ => { });
        }

        /// <summary>
        /// Parallel for each loop.
        /// </summary>
        /// <returns>Loop result.</returns>
        /// <param name="source">Source.</param>
        /// <param name="body">Body.</param>
        /// <typeparam name="TSource">The 1st type parameter.</typeparam>
        public static ParallelLoopResult ForEach<TSource> (IEnumerable<TSource> source,
                                                           Action<TSource, ParallelLoopState, long> body)
        {
            if (source == null)
                throw new ArgumentNullException ("source");
            if (body == null)
                throw new ArgumentNullException ("body");


            return ForEach<TSource, object> (Partitioner.Create (source),
                                             ParallelOptions.Default,
                                             () => null,
                                             (e, s, l) => { body (e, s, -1); return null; },
                                             _ => { });
        }

        /// <summary>
        /// Parallel for each loop.
        /// </summary>
        /// <returns>Loop result.</returns>
        /// <param name="source">Source.</param>
        /// <param name="body">Body.</param>
        /// <typeparam name="TSource">The 1st type parameter.</typeparam>
        public static ParallelLoopResult ForEach<TSource> (Partitioner<TSource> source,
                                                           Action<TSource, ParallelLoopState> body)
        {
            if (body == null)
                throw new ArgumentNullException ("body");

            return ForEach<TSource, object> (source,
                                             ParallelOptions.Default,
                                             () => null,
                                             (e, s, l) => { body (e, s); return null; },
                                             _ => { });
        }

        /// <summary>
        /// Parallel for each loop.
        /// </summary>
        /// <returns>Loop result.</returns>
        /// <param name="source">Source.</param>
        /// <param name="body">Body.</param>
        /// <typeparam name="TSource">The 1st type parameter.</typeparam>
        public static ParallelLoopResult ForEach<TSource> (OrderablePartitioner<TSource> source,
                                                           Action<TSource, ParallelLoopState, long> body)

        {
            if (body == null)
                throw new ArgumentNullException ("body");

            return ForEach<TSource, object> (source,
                                             ParallelOptions.Default,
                                             () => null,
                                             (e, s, i, l) => { body (e, s, i); return null; },
                                             _ => { });
        }

        /// <summary>
        /// Parallel for each loop.
        /// </summary>
        /// <returns>Loop result.</returns>
        /// <param name="source">Source.</param>
        /// <param name="body">Body.</param>
        /// <typeparam name="TSource">The 1st type parameter.</typeparam>
        public static ParallelLoopResult ForEach<TSource> (Partitioner<TSource> source,
                                                           Action<TSource> body)

        {
            if (body == null)
                throw new ArgumentNullException ("body");

            return ForEach<TSource, object> (source,
                                             ParallelOptions.Default,
                                             () => null,
                                             (e, s, l) => { body (e); return null; },
                                             _ => { });
        }

        /// <summary>
        /// Parallel for each loop.
        /// </summary>
        /// <returns>Loop result.</returns>
        /// <param name="source">Source.</param>
        /// <param name="parallelOptions">Parallel options.</param>
        /// <param name="body">Body.</param>
        /// <typeparam name="TSource">The 1st type parameter.</typeparam>
        public static ParallelLoopResult ForEach<TSource> (IEnumerable<TSource> source,
                                                           ParallelOptions parallelOptions,
                                                           Action<TSource> body)
        {
            if (source == null)
                throw new ArgumentNullException ("source");
            if (body == null)
                throw new ArgumentNullException ("body");

            return ForEach<TSource, object> (Partitioner.Create (source),
                                             parallelOptions,
                                             () => null,
                                             (e, s, l) => { body (e); return null; },
                                             _ => { });
        }

        /// <summary>
        /// Parallel for each loop.
        /// </summary>
        /// <returns>Loop result.</returns>
        /// <param name="source">Source.</param>
        /// <param name="parallelOptions">Parallel options.</param>
        /// <param name="body">Body.</param>
        /// <typeparam name="TSource">The 1st type parameter.</typeparam>
        public static ParallelLoopResult ForEach<TSource> (IEnumerable<TSource> source, ParallelOptions parallelOptions,
                                                           Action<TSource, ParallelLoopState> body)
        {
            if (source == null)
                throw new ArgumentNullException ("source");
            if (body == null)
                throw new ArgumentNullException ("body");

            return ForEach<TSource, object> (Partitioner.Create (source),
                                             parallelOptions,
                                             () => null,
                                             (e, s, l) => { body (e, s); return null; },
                                             _ => { });
        }

        /// <summary>
        /// Parallel for each loop.
        /// </summary>
        /// <returns>Loop result.</returns>
        /// <param name="source">Source.</param>
        /// <param name="parallelOptions">Parallel options.</param>
        /// <param name="body">Body.</param>
        /// <typeparam name="TSource">The 1st type parameter.</typeparam>
        public static ParallelLoopResult ForEach<TSource> (IEnumerable<TSource> source, ParallelOptions parallelOptions,
                                                           Action<TSource, ParallelLoopState, long> body)
        {
            if (source == null)
                throw new ArgumentNullException ("source");
            if (body == null)
                throw new ArgumentNullException ("body");

            return ForEach<TSource, object> (Partitioner.Create (source),
                                             parallelOptions,
                                             () => null,
                                             (e, s, i, l) => { body (e, s, i); return null; },
                                             _ => { });
        }

        /// <summary>
        /// Parallel for each loop.
        /// </summary>
        /// <returns>Loop result.</returns>
        /// <param name="source">Source.</param>
        /// <param name="parallelOptions">Parallel options.</param>
        /// <param name="body">Body.</param>
        /// <typeparam name="TSource">The 1st type parameter.</typeparam>
        public static ParallelLoopResult ForEach<TSource> (OrderablePartitioner<TSource> source, ParallelOptions parallelOptions,
                                                           Action<TSource, ParallelLoopState, long> body)

        {
            if (body == null)
                throw new ArgumentNullException ("body");

            return ForEach<TSource, object> (source,
                                             parallelOptions,
                                             () => null,
                                             (e, s, i, l) => { body (e, s, i); return null; },
                                             _ => { });
        }

        /// <summary>
        /// Parallel for each loop.
        /// </summary>
        /// <returns>Loop result.</returns>
        /// <param name="source">Source.</param>
        /// <param name="parallelOptions">Parallel options.</param>
        /// <param name="body">Body.</param>
        /// <typeparam name="TSource">The 1st type parameter.</typeparam>
        public static ParallelLoopResult ForEach<TSource> (Partitioner<TSource> source, ParallelOptions parallelOptions,
                                                           Action<TSource> body)
        {
            if (body == null)
                throw new ArgumentNullException ("body");

            return ForEach<TSource, object> (source,
                                             parallelOptions,
                                             () => null,
                                             (e, s, l) => { body (e); return null; },
                                             _ => { });
        }

        /// <summary>
        /// Parallel for each loop.
        /// </summary>
        /// <returns>Loop result.</returns>
        /// <param name="source">Source.</param>
        /// <param name="parallelOptions">Parallel options.</param>
        /// <param name="body">Body.</param>
        /// <typeparam name="TSource">The 1st type parameter.</typeparam>
        public static ParallelLoopResult ForEach<TSource> (Partitioner<TSource> source, ParallelOptions parallelOptions,
                                                           Action<TSource, ParallelLoopState> body)
        {
            return ForEach<TSource, object> (source,
                                             parallelOptions,
                                             () => null,
                                             (e, s, l) => { body (e, s); return null; },
                                             _ => { });
        }

        /// <summary>
        /// Parallel for each loop.
        /// </summary>
        /// <returns>Loop result.</returns>
        /// <param name="source">Source.</param>
        /// <param name="localInit">Local init.</param>
        /// <param name="body">Body.</param>
        /// <param name="localFinally">Local finally.</param>
        /// <typeparam name="TSource">The 1st type parameter.</typeparam>
        /// <typeparam name="TLocal">The 2nd type parameter.</typeparam>
        public static ParallelLoopResult ForEach<TSource, TLocal> (IEnumerable<TSource> source, Func<TLocal> localInit,
                                                                   Func<TSource, ParallelLoopState, TLocal, TLocal> body,
                                                                   Action<TLocal> localFinally)
        {
            if (source == null)
                throw new ArgumentNullException ("source");

            return ForEach<TSource, TLocal> ((Partitioner<TSource>)Partitioner.Create (source),
                                             ParallelOptions.Default,
                                             localInit,
                                             body,
                                             localFinally);
        }

        /// <summary>
        /// Parallel for each loop.
        /// </summary>
        /// <returns>Loop result.</returns>
        /// <param name="source">Source.</param>
        /// <param name="localInit">Local init.</param>
        /// <param name="body">Body.</param>
        /// <param name="localFinally">Local finally.</param>
        /// <typeparam name="TSource">The 1st type parameter.</typeparam>
        /// <typeparam name="TLocal">The 2nd type parameter.</typeparam>
        public static ParallelLoopResult ForEach<TSource, TLocal> (IEnumerable<TSource> source, Func<TLocal> localInit,
                                                                   Func<TSource, ParallelLoopState, long, TLocal, TLocal> body,
                                                                   Action<TLocal> localFinally)
        {
            return ForEach<TSource, TLocal> (Partitioner.Create (source),
                                             ParallelOptions.Default,
                                             localInit,
                                             body,
                                             localFinally);
        }

        /// <summary>
        /// Parallel for each loop.
        /// </summary>
        /// <returns>Loop result.</returns>
        /// <param name="source">Source.</param>
        /// <param name="localInit">Local init.</param>
        /// <param name="body">Body.</param>
        /// <param name="localFinally">Local finally.</param>
        /// <typeparam name="TSource">The 1st type parameter.</typeparam>
        /// <typeparam name="TLocal">The 2nd type parameter.</typeparam>
        public static ParallelLoopResult ForEach<TSource, TLocal> (OrderablePartitioner<TSource> source, Func<TLocal> localInit,
                                                                   Func<TSource, ParallelLoopState, long, TLocal, TLocal> body,
                                                                   Action<TLocal> localFinally)
        {
            return ForEach<TSource, TLocal> (source, ParallelOptions.Default, localInit, body, localFinally);
        }

        /// <summary>
        /// Parallel for each loop.
        /// </summary>
        /// <returns>Loop result.</returns>
        /// <param name="source">Source.</param>
        /// <param name="localInit">Local init.</param>
        /// <param name="body">Body.</param>
        /// <param name="localFinally">Local finally.</param>
        /// <typeparam name="TSource">The 1st type parameter.</typeparam>
        /// <typeparam name="TLocal">The 2nd type parameter.</typeparam>
        public static ParallelLoopResult ForEach<TSource, TLocal> (Partitioner<TSource> source, Func<TLocal> localInit,
                                                                   Func<TSource, ParallelLoopState, TLocal, TLocal> body,
                                                                   Action<TLocal> localFinally)
        {
            return ForEach<TSource, TLocal> (source, ParallelOptions.Default, localInit, body, localFinally);
        }

        /// <summary>
        /// Parallel for each loop.
        /// </summary>
        /// <returns>Loop result.</returns>
        /// <param name="source">Source.</param>
        /// <param name="parallelOptions">Parallel options.</param>
        /// <param name="localInit">Local init.</param>
        /// <param name="body">Body.</param>
        /// <param name="localFinally">Local finally.</param>
        /// <typeparam name="TSource">The 1st type parameter.</typeparam>
        /// <typeparam name="TLocal">The 2nd type parameter.</typeparam>
        public static ParallelLoopResult ForEach<TSource, TLocal> (IEnumerable<TSource> source, ParallelOptions parallelOptions,
                                                                   Func<TLocal> localInit,
                                                                   Func<TSource, ParallelLoopState, TLocal, TLocal> body,
                                                                   Action<TLocal> localFinally)
        {
            if (source == null)
                throw new ArgumentNullException ("source");

            return ForEach<TSource, TLocal> (Partitioner.Create (source), parallelOptions, localInit, body, localFinally);
        }

        /// <summary>
        /// Parallel for each loop.
        /// </summary>
        /// <returns>Loop result.</returns>
        /// <param name="source">Source.</param>
        /// <param name="parallelOptions">Parallel options.</param>
        /// <param name="localInit">Local init.</param>
        /// <param name="body">Body.</param>
        /// <param name="localFinally">Local finally.</param>
        /// <typeparam name="TSource">The 1st type parameter.</typeparam>
        /// <typeparam name="TLocal">The 2nd type parameter.</typeparam>
        public static ParallelLoopResult ForEach<TSource, TLocal> (IEnumerable<TSource> source, ParallelOptions parallelOptions,
                                                                   Func<TLocal> localInit,
                                                                   Func<TSource, ParallelLoopState, long, TLocal, TLocal> body,
                                                                   Action<TLocal> localFinally)
        {
            if (source == null)
                throw new ArgumentNullException ("source");

            return ForEach<TSource, TLocal> (Partitioner.Create (source), parallelOptions, localInit, body, localFinally);
        }

        /// <summary>
        /// Parallel for each loop.
        /// </summary>
        /// <returns>Loop result.</returns>
        /// <param name="source">Source.</param>
        /// <param name="parallelOptions">Parallel options.</param>
        /// <param name="localInit">Local init.</param>
        /// <param name="body">Body.</param>
        /// <param name="localFinally">Local finally.</param>
        /// <typeparam name="TSource">The 1st type parameter.</typeparam>
        /// <typeparam name="TLocal">The 2nd type parameter.</typeparam>
        public static ParallelLoopResult ForEach<TSource, TLocal> (Partitioner<TSource> source, ParallelOptions parallelOptions,
                                                                   Func<TLocal> localInit,
                                                                   Func<TSource, ParallelLoopState, TLocal, TLocal> body,
                                                                   Action<TLocal> localFinally)
        {
            if (source == null)
                throw new ArgumentNullException ("source");
            if (body == null)
                throw new ArgumentNullException ("body");

            return ForEach<TSource, TLocal> (source.GetPartitions, parallelOptions, localInit, body, localFinally);
        }

        /// <summary>
        /// Parallel for each loop.
        /// </summary>
        /// <returns>Loop result.</returns>
        /// <param name="source">Source.</param>
        /// <param name="parallelOptions">Parallel options.</param>
        /// <param name="localInit">Local init.</param>
        /// <param name="body">Body.</param>
        /// <param name="localFinally">Local finally.</param>
        /// <typeparam name="TSource">The 1st type parameter.</typeparam>
        /// <typeparam name="TLocal">The 2nd type parameter.</typeparam>
        public static ParallelLoopResult ForEach<TSource, TLocal> (OrderablePartitioner<TSource> source, ParallelOptions parallelOptions,
                                                                   Func<TLocal> localInit,
                                                                   Func<TSource, ParallelLoopState, long, TLocal, TLocal> body,
                                                                   Action<TLocal> localFinally)
        {
            if (source == null)
                throw new ArgumentNullException ("source");
            if (body == null)
                throw new ArgumentNullException ("body");

            return ForEach<KeyValuePair<long, TSource>, TLocal> (source.GetOrderablePartitions,
                                                                 parallelOptions,
                                                                 localInit,
                                                                 (e, s, l) => body (e.Value, s, e.Key, l),
                                                                 localFinally);
        }
        #endregion ForEach

        #region Invoke
        /// <summary>
        /// Invoke the specified actions.
        /// </summary>
        /// <param name="actions">Actions.</param>
        public static void Invoke (params Action [] actions)
        {
            if (actions == null)
                throw new ArgumentNullException ("actions");

            Invoke (ParallelOptions.Default, actions);
        }

        /// <summary>
        /// Invoke the specified actions.
        /// </summary>
        /// <param name="parallelOptions">Parallel options.</param>
        /// <param name="actions">Actions.</param>
        public static void Invoke (ParallelOptions parallelOptions, params Action [] actions)
        {
            if (SupportsParallelism) {
                Parallel.Invoke (parallelOptions, actions);
                return;
            }
                
            if (parallelOptions == null)
                throw new ArgumentNullException ("parallelOptions");
            if (actions == null)
                throw new ArgumentNullException ("actions");
            if (actions.Length == 0)
                throw new ArgumentException ("actions is empty");
            foreach (var a in actions)
                if (a == null)
                    throw new ArgumentException ("One action in actions is null", "actions");
            if (actions.Length == 1) {
                actions [0] ();
                return;
            }

            foreach (var action in actions) {
                action ();
            }
        }
        #endregion
    }
}

