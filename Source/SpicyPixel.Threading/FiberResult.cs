using System;

namespace SpicyPixel.Threading
{
    /// <summary>
    /// An instruction to stop fiber execution and set a result on the fiber.
    /// </summary>
    public sealed class FiberResult : FiberInstruction
    {
        object result;

        /// <summary>
        /// Gets the result of the fiber execution.
        /// </summary>
        /// <value>The result of the fiber execution.</value>
        public object Result {
            get { return result; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpicyPixel.Threading.FiberResult"/> class.
        /// </summary>
        /// <param name="result">Result of the fiber execution.</param>
        public FiberResult(object result)
        {
            this.result = result;
        }
    }
}

