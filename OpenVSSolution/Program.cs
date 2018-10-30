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

            // using the -o flag above gives a 66MB directory
            //   An assembly specified in the application dependencies manifest (d.deps.json) was not found:
            // package: 'runtime.win-x64.Microsoft.NETCore.App', version: '2.1.5'
            // path: 'runtimes/win-x64/lib/netcoreapp2.1/Microsoft.CSharp.dll'

            var currentPath = Directory.GetCurrentDirectory();
            var files = new DirectoryInfo(currentPath).GetFiles().Where(x => x.Extension == ".sln");

            // where is VS - Community or Enterprise?
            var devenv = @"C:\Program Files (x86)\Microsoft Visual Studio\2017\";
            var vsversion = new DirectoryInfo(devenv).GetDirectories().Where(x => x.Name == "Community");

            var devenvpath = devenv; 
            if (vsversion.Count() == 1)
                devenvpath += @"Community\Common7\IDE\";
            else
                devenvpath += @"Enterprise\Common7\IDE\";

            foreach (var file in files)
            {
                Console.WriteLine($"{file.Name,-20} : Opening this file! "); // nice console formatting
                var proc = new Process();
                proc.StartInfo.FileName = devenvpath + "devenv";
                proc.StartInfo.Arguments = currentPath + @"\" + file.Name;
                proc.Start();
            }
        }
    }
}
