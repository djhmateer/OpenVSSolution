using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

// See https://davemateer.com/coding/2018/11/08/Publish-dot-net-core-console-application.html for how to publish and distribute
namespace OpenVSSolution
{
    class Program
    {
        static void Main()
        {
            var currentPath = Directory.GetCurrentDirectory();

            // To debug a certain sln file hard code in the path to test
            // var currentPath = @"c:\dev\test\WebApplication5";

            // Get the most recently accessed solution file or return null if none
            var slnFile = new DirectoryInfo(currentPath).GetFiles()
                .Where(x => x.Extension == ".sln")
                .OrderBy(x => x.LastAccessTimeUtc)
                .FirstOrDefault();
            if (slnFile == null)
            {
                Console.WriteLine("No .sln file found");
                return;
            }

            // Prefer VS2019 then downgrade to VS2017 if not there
            var devEnvPath = @"C:\Program Files (x86)\Microsoft Visual Studio\";
            var vsDirectoryVersion = new DirectoryInfo(devEnvPath).GetDirectories();
            if (vsDirectoryVersion.Any(x => x.Name == "2019"))
                devEnvPath += @"2019\";
            else if (vsDirectoryVersion.Any(x => x.Name == "2017"))
                devEnvPath += @"2017\";
            else
            {
                Console.WriteLine($"Neither Visual Studio Community, Professional nor Enterprise can be found in {devEnvPath}");
                return;
            }

            var vsDirectory = new DirectoryInfo(devEnvPath).GetDirectories();
            // Where is VS - Community or Enterprise?
            if (vsDirectory.Any(x => x.Name == "Community"))
                devEnvPath += @"Community\Common7\IDE\";
            else if (vsDirectory.Any(x => x.Name == "Professional"))
                devEnvPath += @"Professional\Common7\IDE\";
            else if (vsDirectory.Any(x => x.Name == "Enterprise"))
                devEnvPath += @"Enterprise\Common7\IDE\";
            else
            {
                Console.WriteLine($"Neither Visual Studio Community, Professional nor Enterprise can be found in {devEnvPath}");
                return;
            }

            // Call VS in a new process and return to the shell
            Console.WriteLine($"{slnFile.Name,-20} : Opening this file! ");
            var proc = new Process();
            proc.StartInfo.FileName = devEnvPath + "devenv";
            
            // Enclose single argument in "" if file path or sln name includes a space
            var arguments = "\"" + currentPath + @"\" + slnFile.Name + "\"";

            proc.StartInfo.Arguments = arguments;
            proc.Start();
        }
    }
}
