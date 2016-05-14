using System;

namespace SpicyPixel.Threading
{
    class FiberContinuation
    {
        public Fiber Fiber;
        public FiberContinuationOptions Options;
        public FiberScheduler Scheduler;

        public FiberContinuation(Fiber fiber, FiberContinuationOptions options, FiberScheduler scheduler)
        {
            this.Fiber = fiber;
            this.Options = options;
            this.Scheduler = scheduler;
        }

        bool ContinuationStateCheck (FiberContinuationOptions options)
        {
            if (options == FiberContinuationOptions.None)
                return true;

            int kindCode = (int) options;
            var state = Fiber.antecedent.Status;

            if (kindCode >= ((int) FiberContinuationOptions.NotOnRanToCompletion)) {
                if (state == FiberStatus.Canceled) {
                    if (options == FiberContinuationOptions.NotOnCanceled)
                        return false;
                    if (options == FiberContinuationOptions.OnlyOnFaulted)
                        return false;
                    if (options == FiberContinuationOptions.OnlyOnRanToCompletion)
                        return false;
                } else if (state == FiberStatus.Faulted) {
                    if (options == FiberContinuationOptions.NotOnFaulted)
                        return false;
                    if (options == FiberContinuationOptions.OnlyOnCanceled)
                        return false;
                    if (options == FiberContinuationOptions.OnlyOnRanToCompletion)
                        return false;
                } else if (state == FiberStatus.RanToCompletion) {
                    if (options == FiberContinuationOptions.NotOnRanToCompletion)
                        return false;
                    if (options == FiberContinuationOptions.OnlyOnFaulted)
                        return false;
                    if (options == FiberContinuationOptions.OnlyOnCanceled)
                        return false;
                }
            }

            return true;
        }

        public void Execute()
        {
            // In case ran, faulted, canceled externally
            if (Fiber.IsCompleted)
                return;

            // Cancel the fiber if criteria is not met
            if (!ContinuationStateCheck (Options)) {
                Fiber.CancelContinuation();
                return;
            }

            Fiber.Start (Scheduler);
        }
    }
}

