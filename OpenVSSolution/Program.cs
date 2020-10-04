using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using static System.PlatformID;

// See https://davemateer.com/coding/2018/11/08/Publish-dot-net-core-console-application.html for how to publish and distribute
namespace OpenVSSolution
{
    internal static class Program
    {
        /// <summary>
        /// The current options.
        /// </summary>
        private static readonly string[] _commands = new[]
        {
            "--dir", //Specify directory
            "--target", //Specify sln name
            "-I", //Run as user
            "-h", //Print help & exit
            "-U" //Username
        };

        static void Main(string[] args)
        {
            var currentPath = Directory.GetCurrentDirectory();
#if DEBUG
            if (args != null)
                foreach (var arg in args)
                    Console.WriteLine(arg);
# endif

            var os = Environment.OSVersion;
            switch (os.Platform)
            {
                case Win32Windows:
                case Win32NT:
                    HandleWindows(args, currentPath);
                    break;
                case Unix:
                case MacOSX:
                    Console.WriteLine("Not yet implemented for *nix systems. \n");
                    return;
                case Win32S:
                case WinCE:
                case Xbox:
                    Console.WriteLine("WE DON'T SUPPORT THIS \n");
                    return;
                default:
                    break;
            }
        }

        /// <summary>
        /// Handles the operation for <see cref="Win32NT"/> and <see cref="Win32Windows"/>
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <param name="currentPath">The current path.</param>
        private static void HandleWindows(IEnumerable<string> args, string currentPath)
        {
            args = args.ToList();
            // --help -h
            if (args.Contains(_commands[3], StringComparer.Ordinal) || args.Contains("--help", StringComparer.Ordinal))
            {
                Console.WriteLine("USAGE: d [options] \n Open a .sln file in the current directory with VisualStudio 2019/2017"
                    + "\n -h, --help\t\t Display this help text."
                    + "\n -d, --dir [DIRECTORY]\t open the sln located in [DIRECTORY]."
                    + "\n -I\t\t\t Use current logged in user's identity to open."
                    + "\n -U [USER[@DOMAIN]]\t Use the identity of [USER]. Overrides -I."
                    + "\n --target [NAME]\t open the [NAME].sln file.");
                return;
            }

            FileInfo sln;
            //  --dir -d
            if (args.Contains(_commands[0], StringComparer.Ordinal) || args.Contains("-d", StringComparer.Ordinal))
            {
                // If we have the --dir (directory) option then we should use that directory as the starting point instead of the current directory
                var directoryArgIndex = (args as List<string>).IndexOf(_commands[0]);
                if (directoryArgIndex < 0) directoryArgIndex = (args as List<string>).IndexOf("-d");
                directoryArgIndex += 1;

                // Not sure how this will work on WSL, but it gets weird using git-bash
                currentPath = args.ElementAt(directoryArgIndex);
                // For cygwin, git-bash, etc.
                currentPath = currentPath.Replace('/', '\\');
                if (currentPath.StartsWith(@".\"))
                {
                    currentPath = currentPath.Replace(".\\", Environment.CurrentDirectory);
                }
                if (currentPath.StartsWith(@"..\") || currentPath.StartsWith(".."))
                {
                    var dirArray = currentPath.Split(@"..\");
                    var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
                    // if we're in C:/foo/bar/project
                    // then ../../foobar => C:/foo/foobar
                    foreach (var parent in dirArray.Skip(1).Where(string.IsNullOrWhiteSpace)) //  if we have n `../`s then we get dirArray.Length = n + 1
                        currentDir = currentDir.Parent ?? currentDir;

                    currentPath = currentDir.FullName;
                }
            }

            // To debug a certain sln file hard code in the path to test
            // currentPath = @"c:\dev\test\WebApplication5"

            // --target
            // Get the most recently accessed solution file or return null if none
            if (args.Contains(_commands[1], StringComparer.OrdinalIgnoreCase))
            {
                // if we have the --target option then we should try finding what was passed in
                var fileName = args.ElementAt((args as List<string>).IndexOf(_commands[1]) + 1);

                sln = new DirectoryInfo(currentPath).GetFiles()
                .Where(x => x.Extension == ".sln")
                .OrderBy(x => x.LastAccessTime)
                .FirstOrDefault(x => x.FullName.Contains(fileName, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                sln = new DirectoryInfo(currentPath).GetFiles()
                    .Where(x => x.Extension == ".sln")
                    .OrderBy(x => x.LastAccessTimeUtc)
                    .FirstOrDefault();
            }

            if (sln == null)
            {
                Console.WriteLine("No .sln file found");
                return;
            }

            // Prefer VS2019 then downgrade to VS2017 if not there
            // "C:\Program Files (x86)\Microsoft Visual Studio"
            var devEnvPath = @$"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)}\Microsoft Visual Studio";
            var vsDirectory = new DirectoryInfo(devEnvPath).GetDirectories().OrderByDescending(d => d.Name)
                .FirstOrDefault(d => d.Name == "2019" || d.Name == "2017");
            if (vsDirectory == null)
            {
                Console.WriteLine($"Cannot find Visual Studio Community, Professional nor Enterprise 2019/2017 in {devEnvPath}");
                return;
            }

            // Where is VS - Community, Professional or Enterprise?
            vsDirectory = vsDirectory.GetDirectories().FirstOrDefault(dir => dir.Name == "Community" || dir.Name == "Professional" || dir.Name == "Enterprise");
            if (vsDirectory == null)
            {
                Console.WriteLine($"Neither Visual Studio Community, Professional nor Enterprise can be found in {devEnvPath}");
                return;
            }
            devEnvPath = vsDirectory.GetDirectories(@"Common7\IDE").First().FullName;
            // Call VS in a new process and return to the shell
            Console.WriteLine($"{sln.Name,-20} : Opening this file! ");

            // Try to impersonate the current logged in user if no username
            var asUser = args.Contains(_commands[2], StringComparer.Ordinal)
                || args.Contains(_commands[4], StringComparer.Ordinal);

            var userName = args.Contains(_commands[4], StringComparer.Ordinal)
                ? args.ElementAt((args as List<string>).IndexOf(_commands[4]) + 1)
                : $"{Environment.UserName}@{Environment.UserDomainName}";
            var startInfo = asUser ? new ProcessStartInfo(@$"{devEnvPath}\devenv")
            {
                Verb = "runas",
                UserName = userName,
                ErrorDialog = true,
                Domain = null
            }
            : new ProcessStartInfo(@$"{devEnvPath}\devenv");

            using var pwd = new SecureString();
            if (asUser)
            {
                Console.WriteLine($"Opening as {userName}");
                Console.WriteLine();
                Console.Write("Enter password: ");
                var validInputKeys = new List<int>(
                    Enumerable.Range(48, 10)    // 0 - 9
                    .Concat(Enumerable.Range(65, 26)) // A - Z
                    .Concat(Enumerable.Range(96, 10))); //0 - 9 (num pad)

                ConsoleKeyInfo key;
                do
                {
                    key = Console.ReadKey(true);

                    // Ignore any key out of range.
                    if (validInputKeys.Contains((int)key.Key)) pwd.AppendChar(key.KeyChar);

                    // Handle backspace
                    // TODO - Handle delete key
                    if (key.Key == ConsoleKey.Backspace && pwd.Length > 0) pwd.RemoveAt(pwd.Length - 1);

                } while (key.Key != ConsoleKey.Enter);
                Console.WriteLine();
                if (pwd.Length == 0) Console.WriteLine("WARNING: No password entered...");
                Console.WriteLine(pwd.ToString());
                startInfo.Password = pwd;
            }

            // Enclose single argument in "" if file path or sln name includes a space
            startInfo.Arguments = sln.FullName.Contains(' ', StringComparison.Ordinal)
                ? $"\"{sln.FullName}\""
                : sln.FullName;
#if DEBUG
            Console.WriteLine($"Executing : {startInfo.FileName + " " + startInfo.Arguments,+20}");
#endif
            Process.Start(startInfo);
        }
    }
}