using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

// See https://davemateer.com/coding/2018/11/08/Publish-dot-net-core-console-application.html for how to publish and distribute
namespace OpenVSSolution
{
    class Program
    {
        private static string[] _commands = new[]
        {
            "--dir", "--target"
        };

        static void Main(string[] args)
        {
            var currentPath = Directory.GetCurrentDirectory();
            if (args != null)
            {
                foreach (var arg in args)
                    System.Console.WriteLine(arg);
            }

            // to parse the options passed in, let's just go for O(n) right now...
            if (args.Contains(_commands[0]))
            {
                // If we have the --dir (directory) option then we should use that folder as the starting point
                // for now, let's just pass it in as the current path
                var argPosition = args.ToList().IndexOf(_commands[0]) + 1;
                currentPath = args[argPosition];
            }

            if (args.Contains(_commands[1]))
            {
                // if we have the --target option then we should try finding what was passed in
                var arg = args[args.ToList().IndexOf(_commands[1]) + 1];
                
                var sln = new DirectoryInfo(currentPath).GetFiles()
                .Where(x => x.Extension == ".sln")
                .OrderBy(x => x.LastAccessTime)
                .FirstOrDefault(x => x.FullName.Contains(arg, StringComparison.OrdinalIgnoreCase));
            }
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
