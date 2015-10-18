using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using CodeComb.Package;

namespace CodeComb.CI.Publisher.NuGet
{
    public class NuGetCIPublisher : ICIPublisher
    {
        public NuGetCIPublisher() { }

        public NuGetCIPublisher(string path, string apiKey, string fileRules = "*.nupkg", string address = "https://www.nuget.org/")
        {
            ApiKey = apiKey;
            FileRules = fileRules;
            Address = address;
            Path = path;
        }

        public string Address { get; set; }

        public string ApiKey { get; set; }

        public string FileRules { get; set; }

        public string Path { get; set; }

        public List<string> Discover()
        {
            return Directory.GetFiles(Path, FileRules, SearchOption.AllDirectories).ToList();
        }

        public void Publish()
        {
            foreach(var x in Discover())
            {
                string fileName, arguments;
                if (OS.Current == OSType.Windows)
                {
                    fileName = "NuGet.exe";
                    arguments = $"push {x} -s {Address} {ApiKey}";
                }
                else
                {
                    fileName = "mono";
                    arguments = $"nuget.exe push {x} -s {Address} {ApiKey}";
                }
                Process process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    FileName = fileName,
                    Arguments = arguments
                };
                process.Start();
                process.WaitForExit();
            }
        }
    }
}
