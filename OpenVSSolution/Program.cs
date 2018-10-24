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
            // dotnet publish -c Release -r win10-x64
            // open this foldere: C:\dev\test\OpenVSSolution\OpenVSSolution\bin\Release\netcoreapp2.1\win10 - x64
            // copy the entire directory C:\sharedTools\OpenVSSolution

            var currentPath = Directory.GetCurrentDirectory();
            var files = new DirectoryInfo(currentPath).GetFiles().Where(x => x.Extension == ".sln");

            foreach (var file in files)
            {
                Console.WriteLine($"{file.Name,-20} : Opening this file! "); // nice console formatting
                var proc = new Process();
                var devenvpath = @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\Common7\IDE\";
                proc.StartInfo.FileName = devenvpath + "devenv";
                proc.StartInfo.Arguments = currentPath + @"\" + file.Name;
                proc.Start();
            }
        }
    }
}
