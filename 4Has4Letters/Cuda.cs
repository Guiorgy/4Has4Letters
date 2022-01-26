using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace _4Has4Letters
{
    [SuppressMessage("Major Code Smell", "S4200:Native methods should be wrapped", Justification = "Don't wanna")]
    [SuppressMessage("Major Code Smell", "S4214:\"P/Invoke\" methods should not be visible")]
    [SuppressMessage("Interoperability", "CA1401:P/Invokes should not be visible")]
    public static class Cuda
    {
        [SuppressMessage("Minor Code Smell", "S1104:Fields should not have public accessibility", Justification = "Unnecesary")]
        public struct Section
        {
            public ulong start;
            public int steps;
        }

        public const string CudaDll = "Cuda.dll";

        [DllImport(CudaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void prepare(int[] under, int[] thousandCount, uint blocks);

        [DllImport(CudaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern Section findBetween(ulong start, ulong end);

        [DllImport(CudaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void reset();
    }
}
