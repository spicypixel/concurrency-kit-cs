using System;

namespace SpicyPixel.Threading
{
    class FiberContinuation
    {
        Fiber fiber;
        FiberContinuationOptions options;
        FiberScheduler scheduler;

        public FiberContinuation(Fiber fiber, FiberContinuationOptions options, FiberScheduler scheduler)
        {
            this.fiber = fiber;
            this.options = options;
            this.scheduler = scheduler;
        }

        bool ContinuationStateCheck (FiberContinuationOptions options)
        {
            if (options == FiberContinuationOptions.None)
                return true;

            int optionCode = (int) options;
            var status = fiber.antecedent.Status;

            if (optionCode >= ((int) FiberContinuationOptions.NotOnRanToCompletion)) {
                if (status == FiberStatus.Canceled) {
                    if (options == FiberContinuationOptions.NotOnCanceled)
                        return false;
                    if (options == FiberContinuationOptions.OnlyOnFaulted)
                        return false;
                    if (options == FiberContinuationOptions.OnlyOnRanToCompletion)
                        return false;
                } else if (status == FiberStatus.Faulted) {
                    if (options == FiberContinuationOptions.NotOnFaulted)
                        return false;
                    if (options == FiberContinuationOptions.OnlyOnCanceled)
                        return false;
                    if (options == FiberContinuationOptions.OnlyOnRanToCompletion)
                        return false;
                } else if (status == FiberStatus.RanToCompletion) {
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
            if (fiber.IsCompleted)
                return;

            // Cancel the fiber if criteria is not met
            if (!ContinuationStateCheck (options)) {
                fiber.CancelContinuation();
                return;
            }

            fiber.Start (scheduler);
        }
    }
}

