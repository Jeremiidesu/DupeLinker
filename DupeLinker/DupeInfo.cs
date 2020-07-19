using System.IO;

namespace DupeLinker
{
    public struct DupeInfo
    {
        public string File { get; set; }
        public FileInfo Info { get; set; }

        public void Deconstruct(out string file, out FileInfo info)
        {
            file = File;
            info = Info;
        }
    }
}