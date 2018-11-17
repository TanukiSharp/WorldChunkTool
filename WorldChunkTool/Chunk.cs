using System;
using System.Collections.Generic;
using System.IO;

namespace WorldChunkTool
{
    class Chunk
    {
        private static readonly byte[] eightBytesBuffer = new byte[8];
        private static byte[] dataBuffer = new byte[5 * 1024 * 1024];

        private static readonly UtilsEx utils = new UtilsEx(0x40000);

        public static void DecompressChunks(String FileInput, bool FlagPKGExtraction)
        {
            string NamePKG = $"{Environment.CurrentDirectory}\\{Path.GetFileNameWithoutExtension(FileInput)}.pkg";
            BinaryReader Reader = new BinaryReader(File.Open(FileInput, FileMode.Open));

            // Key = ChunkOffset, Value = ChunkSize
            Dictionary<long, long> MetaChunk = new Dictionary<long, long>();

            // Read header
            Reader.BaseStream.Seek(4, SeekOrigin.Begin);
            int ChunkCount = Reader.ReadInt32(); int ChunkPadding = ChunkCount.ToString().Length;
            Utils.Print($"{ChunkCount} chunks in this file.", false);

            // Read file list
            for (int i = 0; i < ChunkCount; i++)
            {
                Reader.Read(eightBytesBuffer, 0, eightBytesBuffer.Length);
                eightBytesBuffer[0] >>= 4;

                long ChunkSize =
                    eightBytesBuffer[2] << 16 |
                    eightBytesBuffer[1] << 8 |
                    eightBytesBuffer[0];

                long ChunkOffset =
                    (long)eightBytesBuffer[7] << 32 |
                    (long)eightBytesBuffer[6] << 24 |
                    (long)eightBytesBuffer[5] << 16 |
                    (long)eightBytesBuffer[4] << 8 |
                    eightBytesBuffer[3];

                MetaChunk.Add(ChunkOffset, ChunkSize);
            }

            // Write decompressed chunks to pkg
            BinaryWriter Writer = new BinaryWriter(File.Create(NamePKG));
            int DictCount = 1;

            foreach (KeyValuePair<long, long> Entry in MetaChunk)
            {
                Console.Write($"\rProcessing {DictCount.ToString().PadLeft(ChunkPadding)} / {ChunkCount}...");
                Reader.BaseStream.Seek(Entry.Key, SeekOrigin.Begin);

                if (Entry.Value != 0)
                {
                    int inputSize = (int)Entry.Value;

                    byte[] buffer = GetDataBuffer(inputSize);
                    int bytesRead = Reader.Read(buffer, 0, inputSize);

                    ArraySegment<byte> chunkDecompressed = utils.Decompress(buffer, bytesRead);
                    Writer.Write(chunkDecompressed.Array, chunkDecompressed.Offset, chunkDecompressed.Count);
                }
                else
                {
                    const int inputSize = 0x40000;

                    byte[] buffer = GetDataBuffer(inputSize);
                    int bytesRead = Reader.Read(buffer, 0, inputSize);

                    Writer.Write(buffer, 0, bytesRead);
                }

                DictCount++;
            }
            Reader.Dispose();
            Writer.Dispose();

            Utils.Print("Finished.", true);
            Utils.Print($"Output at: {NamePKG}", false);
            Console.WriteLine("The PKG file will now be extracted. Press Enter to continue or close window to quit.");
            Console.Read();

            Console.SetCursorPosition(0, Console.CursorTop - 1);
            Console.WriteLine("==============================");
            PKG.ExtractPKG(NamePKG, FlagPKGExtraction);
            Console.Read();
        }

        private static byte[] GetDataBuffer(int minimumSize)
        {
            if (minimumSize > dataBuffer.Length)
                dataBuffer = new byte[minimumSize];
            return dataBuffer;
        }
    }
}
