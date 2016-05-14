using System;

namespace SpicyPixel.Threading
{
    public sealed class FiberResult : FiberInstruction
    {
        object result;

        public object Result {
            get { return result; }
        }

        public FiberResult(object result)
        {
            this.result = result;
        }
    }
}

