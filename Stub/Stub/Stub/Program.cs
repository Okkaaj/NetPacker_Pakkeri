namespace Stub
{
    class Program
    {
        static byte[] CompressedData =
        {
            0x00
        };

        static void Main(string[] args)
        {
            byte[] DecompressedData = NativeMethods.Decompress(CompressedData);
            System.Reflection.MethodInfo T = System.Reflection.Assembly.Load(DecompressedData).EntryPoint;
            T.Invoke(null, T.GetParameters().Length == 1 ? (new object[] { new string[] { "" } }) : null);
        }
    }

    internal static class NativeMethods
    {
        const ushort COMPRESSION_FORMAT_LZNT1 = 2;

        [System.Runtime.InteropServices.DllImport("ntdll.dll")]
        private static extern uint RtlGetCompressionWorkSpaceSize(ushort CompressionFormat, ref uint pNeededBufferSize, ref uint Unknown);

        [System.Runtime.InteropServices.DllImport("ntdll.dll")]
        private static extern uint RtlDecompressBuffer(ushort CompressionFormat, byte[] DestinationBuffer, int DestinationBufferLength, byte[] SourceBuffer, int SourceBufferLength, ref int pDestinationSize);

        public static byte[] Decompress(byte[] buffer)
        {
            byte[] outBuf = new byte[buffer.Length * 6];

            uint dwSize = 0, dwRet = 0;

            uint ret = RtlGetCompressionWorkSpaceSize(COMPRESSION_FORMAT_LZNT1, ref dwSize, ref dwRet);

            if (ret != 0)
            {
                return null;
            }

            int dstSize = 0;

            ret = RtlDecompressBuffer(COMPRESSION_FORMAT_LZNT1, outBuf, outBuf.Length, buffer, buffer.Length, ref dstSize);

            if (ret != 0)
            {
                return null;
            }

            System.Array.Resize(ref outBuf, dstSize);
            return outBuf;
        }
    }
}
