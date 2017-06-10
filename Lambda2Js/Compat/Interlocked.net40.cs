using System;
using System.Collections.Generic;
using System.Text;

namespace System.Threading
{
    class Interlocked
    {
        public static void MemoryBarrier()
        {
            Thread.MemoryBarrier();
        }
    }
}
