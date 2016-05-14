using System;

namespace SpicyPixel.Threading
{
    /// <summary>
    /// Specifies the behavior for a fiber that is created by using the `Fiber.ContinueWith` method.
    /// </summary>
    [Flags, Serializable]
    public enum FiberContinuationOptions
    {
        /// <summary>
        /// When no continuation options are specified, default behavior should be used 
        /// to execute a continuation. The continuation runs asynchronously when the antecedent 
        /// completes, regardless of the antecedent's final Fiber.Status property value.
        /// </summary>
        None                  = 0x00000,

        /// <summary>
        /// The continuation should not be scheduled if its antecedent ran to completion. 
        /// An antecedent runs to completion if its Fiber.Status property upon completion 
        /// is FiberStatus.RanToCompletion. This option is not valid for multi-fiber continuations.
        /// </summary>
        NotOnRanToCompletion  = 0x10000,

        /// <summary>
        /// Specifies that the continuation should not be scheduled if its antecedent threw 
        /// an unhandled exception. An antecedent throws an unhandled exception if its Fiber.Status 
        /// property upon completion is FiberStatus.Faulted. This option is not valid for multi-fiber 
        /// continuations.
        /// </summary>
        NotOnFaulted          = 0x20000,

        /// <summary>
        /// The continuation should not be scheduled if its antecedent was canceled. An antecedent 
        /// is canceled if its Fiber.Status property upon completion is FiberStatus.Canceled. This option 
        /// is not valid for multi-fiber continuations.
        /// </summary>
        NotOnCanceled         = 0x40000,

        /// <summary>
        /// The continuation should be scheduled only if its antecedent ran to completion. 
        /// An antecedent runs to completion if its Fiber.Status property upon completion is 
        /// FiberStatus.RanToCompletion. This option is not valid for multi-fiber continuations.
        /// </summary>
        OnlyOnRanToCompletion = 0x60000,

        /// <summary>
        /// The continuation should be scheduled only if its antecedent threw an unhandled 
        /// exception. An antecedent throws an unhandled exception if its Fiber.Status property 
        /// upon completion is FiberStatus.Faulted.
        ///
        /// The OnlyOnFaulted option guarantees that the Fiber.Exception property in the antecedent 
        /// is not null. You can use that property to catch the exception and see which exception 
        /// caused the fiber to fault. If you do not access the Exception property, the exception 
        /// is unhandled. If you attempt to access the Result property of a fiber that has been 
        /// canceled or has faulted, a new exception is thrown.
        ///
        /// This option is not valid for multi-fiber continuations.
        /// </summary>
        OnlyOnFaulted         = 0x50000,

        /// <summary>
        /// Specifies that the continuation should be scheduled only if its antecedent was canceled. 
        /// An antecedent is canceled if its Fiber.Status property upon completion is 
        /// FiberStatus.Canceled. This option is not valid for multi-fiber continuations.
        /// </summary>
        OnlyOnCanceled        = 0x30000,
    }
}

