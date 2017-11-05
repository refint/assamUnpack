using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace assamUnpack
{
    class Program
    {
        private static Dictionary<int, string> dirNames = new Dictionary<int, string>();


        class Header
        {
            public string name { get; set; }
            public int isFile { get; set; }
            public int parent { get; set; }
            public int nextSibling { get; set; }
            public int firstChild { get; set; }
            public int length { get; set; }
            public int blockOffset { get; set; }
        }

        static void Main(string[] args)
        {
            if (args.Length == 0) return;
            if (!args[0].EndsWith(".xaf")) return;

            Stream file = File.OpenRead(args[0]);
            using (BinaryReader reader = new BinaryReader(file))
            {
                reader.ReadInt32(); // header
                reader.ReadInt32(); // version
                int alignment = reader.ReadInt32(); // byte alignment
                int numFiles = reader.ReadInt32();

                reader.BaseStream.Position = 0x100;
                long lastPos;

                List<Header> files = new List<Header>();

                for (int i = 0; i < numFiles; i++)
                {
                    string name = Encoding.ASCII.GetString(reader.ReadBytes(128)).TrimEnd('\0');
                    int isFile = reader.ReadInt32();
                    int parent = reader.ReadInt32();
                    int nextSibling = reader.ReadInt32();
                    int firstChild = reader.ReadInt32();

                    reader.ReadBytes(8); // unk
                    int length = reader.ReadInt32();
                    reader.ReadBytes(4);

                    int blockOffset = reader.ReadInt32();
                    reader.ReadBytes(12); // unk

                    files.Add(new Header()
                    {
                        name = name,
                        isFile = isFile,
                        parent = parent,
                        nextSibling = nextSibling,
                        firstChild = firstChild,
                        length = length,
                        blockOffset = blockOffset
                    });
                }


                foreach (var fff in files)
                {
                    //Console.WriteLine($"{name}: isDir {isFile}, parent {parent}, nextSibling {nextSibling}");
                    lastPos = reader.BaseStream.Position;

                    if ((fff.isFile & 0x1) == 1)
                    {
                        List<string> parentNames = new List<string>();

                        void TraverseParents(Header thisHeader)
                        {
                            if (thisHeader.parent != -1)
                            {
                                parentNames.Add(thisHeader.name);

                                TraverseParents(files[thisHeader.parent]);
                            }
                        }

                        TraverseParents(fff);

                        parentNames.Reverse();

                        var outName = string.Join("/", parentNames);

                        Console.WriteLine(outName);

                        reader.BaseStream.Position = fff.blockOffset * alignment;

                        byte[] compressed = reader.ReadBytes(fff.length);

                        Directory.CreateDirectory(Path.GetDirectoryName(outName));

                        FileStream ou = File.OpenWrite(outName);

                        ou.Write(compressed, 0, compressed.Length);

                        ou.Close();

                        reader.BaseStream.Position = lastPos;
                    }
                }

                //Console.WriteLine("test file decompressed, hopefully");
                Console.ReadKey();
            }
        }

        private static bool CompareByteSets(byte[] set1, byte[] set2)
        {
            bool ret = true;

            int idx = 0;
            foreach (byte byt in set1)
            {
                if (byt != set2[idx++])
                    ret = false;
            }

            return ret;
        }
    }
}
