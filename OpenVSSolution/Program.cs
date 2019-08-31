using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace OpenVSSolution
{
    class Program
    {
        static void Main()
        {

            var builder = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location))
                .AddJsonFile("appsettings.json", false, true);

            IConfiguration config = builder.Build();


            var settings = new VSSettings();
            config.GetSection("VisualStudio").Bind(settings);

            var currentPath = Directory.GetCurrentDirectory();

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

            //var devEnvPath = $@"{settings.BasePath}\{settings.Year}\{settings.Edition}\Common7\IDE\";
            var devEnvPath = $@"{settings.BasePath}\{settings.Year}\{settings.Edition}\{settings.IDEPath}\";

            if (devEnvPath == null || !Directory.Exists(devEnvPath))
            {
                Console.WriteLine($"Unable to locate the {settings.Edition} in {devEnvPath}");
                return;
            }
            
            if (!File.Exists($"{devEnvPath}{settings.Executable}"))
            {
                Console.WriteLine($"Unable to locate {devEnvPath}{settings.Executable}");
                return;
            }

            int width = (Console.WindowWidth - 1);

            Console.WriteLine(new String('-', width));
            Console.WriteLine($"{Utility.Tab}Opening: {string.Empty,-5}{slnFile.Name}");
            Console.WriteLine(new String('-', width));
            using (var process = new Process())
            {
                process.StartInfo.FileName = $"{devEnvPath}{settings.Executable}";
                process.StartInfo.Arguments = $"{currentPath}\\{slnFile.Name}";
                process.Start();
            }
            
        }
    }
}
