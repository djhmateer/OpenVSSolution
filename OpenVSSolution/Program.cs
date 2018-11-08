using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace OpenVSSolution
{
    class Program
    {
        static void Main()
        {
            // cd C:\dev\test\OpenVSSolution
            // dotnet publish -c Release -r win10-x64 -o c:\sharedTools\OpenVSSolution

            // without the -o flag you can get the following when deployed to another machine
            // An assembly specified in the application dependencies manifest (d.deps.json) was not found:
            // package: 'runtime.win-x64.Microsoft.NETCore.App', version: '2.1.5'
            // path: 'runtimes/win-x64/lib/netcoreapp2.1/Microsoft.CSharp.dll'

            var currentPath = Directory.GetCurrentDirectory();

            // Get the most recently accessed solution file or return null if none
            var slnfile = new DirectoryInfo(currentPath).GetFiles()
                .Where(x => x.Extension == ".sln")
                .OrderBy(x => x.LastAccessTimeUtc)
                .FirstOrDefault();

            if (slnfile == null)
            {
                Console.WriteLine("No .sln file found");
                return;
            }

            // where is VS - Community or Enterprise?
            var devenvpath = @"C:\Program Files (x86)\Microsoft Visual Studio\2017\";
            var vsdiretory = new DirectoryInfo(devenvpath).GetDirectories();

            if (vsdiretory.Any(x => x.Name == "Community"))
                devenvpath += @"Community\Common7\IDE\";
            else if (vsdiretory.Any(x => x.Name=="Enterprise"))
                devenvpath += @"Enterprise\Common7\IDE\";
            else
            {
                Console.WriteLine($"Neither Visual Studio Community nor Enterprise can be found in {devenvpath}");
                return;
            }

            Console.WriteLine($"{slnfile.Name,-20} : Opening this file! "); // nice console formatting
            var proc = new Process();
            proc.StartInfo.FileName = devenvpath + "devenv";
            proc.StartInfo.Arguments = currentPath + @"\" + slnfile.Name;
            proc.Start();
        }
    }
}
