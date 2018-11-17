using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace WorldChunkTool
{
    public class UtilsEx
    {
        // Import Oodle decompression
        [DllImport("oo2core_5_win64.dll")]
        private static extern int OodleLZ_Decompress(byte[] buffer, long bufferSize, byte[] outputBuffer, long outputBufferSize, uint a, uint b, ulong c, uint d, uint e, uint f, uint g, uint h, uint i, uint threadModule);

        private readonly byte[] outputBuffer;

        public UtilsEx(int outputBufferSize)
        {
            if (outputBufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(outputBufferSize));

            outputBuffer = new byte[outputBufferSize];
        }

        // Decompress oodle chunk
        // Part of https://github.com/Crauzer/OodleSharp
        public ArraySegment<byte> Decompress(byte[] buffer, int size)
        {
            int decompressedCount = OodleLZ_Decompress(buffer, size, outputBuffer, outputBuffer.Length, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3);
            return new ArraySegment<byte>(outputBuffer, 0, decompressedCount);
        }

        // Console printing helper
        public static void Print(string Input, bool Before)
        {
            if (!Before)
            {
                Console.WriteLine(Input);
                Console.WriteLine("==============================");
            }
            else
            {
                Console.WriteLine("\n==============================");
                Console.WriteLine(Input);
            }
        }
    }
}
