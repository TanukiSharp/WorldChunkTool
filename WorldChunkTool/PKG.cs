// VisualStudio is a piece of garbage and nuked this file while it was open during an outage, 
// so I had to decompile and edit it from my last build before that.
// Excuse the lack of comments.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldChunkTool
{
    class PKG
    {
        private static readonly byte[] NameBuffer = new byte[160];
        private static readonly SubStream subStream = new SubStream();

        public static void ExtractPKG(string FileInput, bool FlagPKGExtraction)
        {
            string OutputDirectory = $"{Environment.CurrentDirectory}\\{Path.GetFileNameWithoutExtension(FileInput)}";
            BinaryReader Reader = new BinaryReader(File.Open(FileInput, FileMode.Open));
            StreamWriter LogWriter = new StreamWriter($"{Path.GetFileNameWithoutExtension(FileInput)}.csv", false);
            LogWriter.WriteLine("Index,Offset,Size,EntryType,Unk,Directory,FileName,FileType");

            Reader.BaseStream.Seek(0x0C, SeekOrigin.Begin);
            int TotalParentCount = Reader.ReadInt32();
            int ParentPadding = TotalParentCount.ToString().Length;
            int TotalChildrenCount = Reader.ReadInt32();
            Utils.Print($"PKG file has {TotalParentCount} parent entries with {TotalChildrenCount} children entries.", false);

            Reader.BaseStream.Seek(0x100, SeekOrigin.Begin);
            for (int i = 0; i < TotalParentCount; i++)
            {
                Reader.BaseStream.Seek(0x3C, SeekOrigin.Current);

                string StringNameParent;
                long FileSize = Reader.ReadInt64();
                long FileOffset = Reader.ReadInt64();
                int EntryType = Reader.ReadInt32();
                int CountChildren = Reader.ReadInt32();

                for (int j = 0; j < CountChildren; j++)
                {
                    Console.Write($"\rParent entry {(i + 1).ToString().PadLeft(ParentPadding)}/{TotalParentCount}. Processing child entry {(j + 1).ToString().PadLeft(4)} / {CountChildren.ToString().PadLeft(4)}...");

                    Reader.Read(NameBuffer, 0, NameBuffer.Length);
                    FileSize = Reader.ReadInt64();
                    FileOffset = Reader.ReadInt64();
                    EntryType = Reader.ReadInt32();
                    int Unknown = Reader.ReadInt32();

                    int trailingZeroIndex = Array.IndexOf<byte>(NameBuffer, 0);
                    StringNameParent = Encoding.UTF8.GetString(NameBuffer, 0, trailingZeroIndex);

                    // Extract remapped and regular files
                    if (FlagPKGExtraction && (EntryType == 0x02 || EntryType == 0x00))
                    {
                        string filename = Path.Combine(OutputDirectory, StringNameParent.TrimStart('\\'));
                        string directory = Path.GetDirectoryName(filename);
                        if (Directory.Exists(directory) == false)
                            Directory.CreateDirectory(directory);

                        long ReaderPositionBeforeEntry = Reader.BaseStream.Position;

                        subStream.UpdateWorkingSet(Reader.BaseStream, FileOffset, FileSize);
                        using (var fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
                            CopyStreams(subStream, fs);

                        Reader.BaseStream.Seek(ReaderPositionBeforeEntry, SeekOrigin.Begin);
                    }

                    // Handle directory entries
                    if (EntryType != 0x01)
                    {
                        LogWriter.WriteLine(
                            i + "," +
                            FileOffset.ToString("X16") + "," +
                            FileSize + "," +
                            EntryType + "," +
                            Unknown + "," +
                            StringNameParent.Remove(StringNameParent.LastIndexOf('\\') + 1) + "," +
                            StringNameParent.Substring(StringNameParent.LastIndexOf('\\') + 1) + "," +
                            StringNameParent.Substring(StringNameParent.IndexOf('.') + 1)
                        );
                    }
                }
            }
            Reader.Close();
            LogWriter.Close();

            Utils.Print("Finished.", true);
            Utils.Print($"Output at: {OutputDirectory}", false);
            Console.WriteLine("Press Enter to quit");
        }

        private static readonly byte[][] transferBuffer = new byte[][]
        {
            new byte[256 * 1024],
            new byte[256 * 1024],
        };

        private static void CopyStreams(Stream input, Stream output)
        {
            CopyStreamsAsync(input, output).GetAwaiter().GetResult();
        }

        private static async Task CopyStreamsAsync(Stream input, Stream output)
        {
            int indexA = 0;
            int indexB = 1;

            int bytesRead = await input.ReadAsync(transferBuffer[indexA], 0, transferBuffer[indexA].Length);

            while (bytesRead > 0)
            {
                Task writeTask = output.WriteAsync(transferBuffer[indexA], 0, bytesRead);
                Task<int> readTask = input.ReadAsync(transferBuffer[indexB], 0, transferBuffer[indexB].Length);

                await Task.WhenAll(writeTask, readTask);

                bytesRead = await readTask;

                indexA = (indexA + 1) & 1;
                indexB = (indexB + 1) & 1;
            }
        }
    }
}
