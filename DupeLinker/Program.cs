using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace DupeLinker
{
    internal static class Program
    {
        private static string _source;
        private static string _destination;
        
        public static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: dupelinker <source_folder> <destination_folder>");
                Environment.Exit(2);
            }

            _source = args[0];
            _destination = args[1];

            if (_source == _destination)
                return;

            var files = Directory.GetFiles(_source, "*", SearchOption.AllDirectories)
                .Select(s => s.Substring(_source.Length).TrimStart("\\/".ToCharArray())).ToList();

            using var md5 = MD5.Create();
            
            foreach (var file in files)
            {
                var srcFile = Path.Combine(_source, file);
                var destFile = Path.Combine(_destination, file);

                if (!File.Exists(destFile))
                {
                    MissingFiles.Add(file);
                    Console.WriteLine($"Not in destination: {file}");
                    continue;
                }

                using var srcStream = File.OpenRead(srcFile);
                using var destStream = File.OpenRead(destFile);

                var srcHash = BitConverter.ToString(md5.ComputeHash(srcStream)).Replace("-", "").ToLowerInvariant();
                var destHash = BitConverter.ToString(md5.ComputeHash(destStream)).Replace("-", "").ToLowerInvariant();

                if (srcHash == destHash)
                {
                    var info = new FileInfo(srcFile);

                    DuplicateFiles.Add(new DupeInfo
                    {
                        File = file,
                        Info = info
                    });
                    
                    Console.WriteLine($"Duplicate file: {file} ({info.Length:#,##0} bytes)");
                }
                else
                {
                    DifferentFiles.Add(file);
                    Console.WriteLine($"Different file: {file}");
                }
            }
            
            Console.WriteLine($"Files present in source but not in destination: {MissingFiles.Count}");
            Console.WriteLine($"Duplicate files: {DuplicateFiles.Count} (possible save: {DuplicateFiles.Aggregate(0L, ((total, info) => total += info.Info.Length)):#,##0} bytes)");
            Console.WriteLine($"Different files: {DifferentFiles.Count}");
            
            Console.WriteLine("Writing link creation script...");
            WriteWindowsLinkScript();
            Console.WriteLine("Link script generated in 'link.bat'. It must be run as administrator to work.");
        }

        private static readonly List<DupeInfo> DuplicateFiles = new List<DupeInfo>();
        private static readonly List<string> DifferentFiles = new List<string>();
        private static readonly List<string> MissingFiles = new List<string>();

        private static void WriteWindowsLinkScript()
        {
            if (File.Exists("link.bat"))
                File.Delete("link.bat");
            
            using var sw = new StreamWriter(File.OpenWrite("link.bat"));

            var cwd = Directory.GetCurrentDirectory();
            
            sw.WriteLine("@ECHO OFF");
            sw.WriteLine(Directory.GetDirectoryRoot(cwd).Substring(0, 2));
            sw.WriteLine($"cd {cwd}");
            
            foreach (var (file, _) in DuplicateFiles)
            {
                sw.WriteLine($@"del ""{Path.Combine(_destination, file)}""");
                sw.WriteLine($@"mklink /H ""{Path.Combine(_destination, file)}"" ""{Path.Combine(_source, file)}""");
            }

            sw.WriteLine("pause");
        }
    }
}