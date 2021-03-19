using System.Runtime.InteropServices;

namespace _4Has4Letters
{
    public class Cuda
    {
        public struct sec
        {
            public ulong start;
            public int steps;
        }

        public const string CudaDll = "Cuda.dll";

        [DllImport(CudaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void prepare(int[] under, int[] thousandCount, uint blocks);

        [DllImport(CudaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern sec findBetween(ulong start, ulong end);

        [DllImport(CudaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void reset();
    }
}
