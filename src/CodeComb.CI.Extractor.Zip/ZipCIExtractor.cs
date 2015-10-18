using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using CodeComb.Package;

namespace CodeComb.CI.Extractor.Zip
{
    public class ZipCIExtractor : ICIExtractor
    {
        public ZipCIExtractor() { }
        public ZipCIExtractor(string uri, string workingPath)
        {
            Uri = uri;
            WorkingPath = workingPath;
        }

        public string Uri { get; set; }
        public string WorkingPath { get; set; }

        public void Clean()
        {
            Directory.Delete(WorkingPath, true);
        }

        public void Extract()
        {
            Download.DownloadAndExtractAll(Uri, WorkingPath);
        }
    }
}
