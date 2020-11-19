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
        private static bool _verbose;

        internal const string Csproj = ".csproj";
        internal const string Vbproj = ".vbproj";
        private const string Sln = ".sln";


        /// <summary>
        /// The current options.
        /// </summary>
        private static readonly string[] _commands = new[]
        {
            "-d", //Specify directory
            "-t", //Specify sln name
            "-p", //Specify csproj name
            "-i", //Run as user
            "-h", //Print help & exit
            "-u", //Username
            "-v" // Verbosity switch
        };

        static void Main(string[] args)
        {
            Console.WriteLine("\n\td for dev[env].exe\n");
            _verbose = args != null && args.Any(arg =>
                    arg.Equals(_commands[^1], StringComparison.OrdinalIgnoreCase)
                    || arg.Equals("-verbose", StringComparison.OrdinalIgnoreCase));
#if DEBUG
            _verbose = true;
#endif
            var currentPath = Directory.GetCurrentDirectory();
            if (_verbose)
            {
                Console.WriteLine("INFO:: Argument List:\n" +
                    $"{string.Join("INFO:: \n\t", args ?? new string[0])}");
            }

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
            var wantDirectory = args.Contains(_commands[0], StringComparer.OrdinalIgnoreCase) || args.Contains("--dir", StringComparer.Ordinal);
            var wantSln = args.Contains(_commands[1], StringComparer.OrdinalIgnoreCase) || args.Contains("--target", StringComparer.Ordinal);
            var wantProj = args.Contains(_commands[2], StringComparer.OrdinalIgnoreCase) || args.Contains("--project", StringComparer.Ordinal);
            var wantImpersonate = args.Contains(_commands[3], StringComparer.OrdinalIgnoreCase) || args.Contains("--impersonate", StringComparer.Ordinal);
            var wantHelp = args.Contains(_commands[4], StringComparer.OrdinalIgnoreCase) || args.Contains("--help", StringComparer.Ordinal);
            var wantUser = args.Contains(_commands[5], StringComparer.OrdinalIgnoreCase) || args.Contains("--user", StringComparer.Ordinal);
            // --help -h
            if (wantHelp)
            {
                Console.WriteLine("USAGE: d [options] \n Open a .sln/.vbproj/.csproj file with VisualStudio 2019/2017"
                    + "\n -h, -H, --help\n\t\t Display this help text."
                    + "\n -v, -V, --verbose\n\t\t Enables debug output."
                    + "\n -d, --dir [DIRECTORY]\n\t\t use [DIRECTORY] instead of current."
                    + "\n -i, -I, --impersonate\n\t\t Use current logged in user's identity to open."
                    + "\n -u, -U, --user [USER[@DOMAIN]]\n\t\t Use the identity of [USER] and prompt for password. Overrides -i."
                    + "\n -t, -T, --target [NAME]\n\t\t open the [NAME].sln file."
                    + "\n -p, -P, --project [NAME]\n\t\t open the [NAME].(cs/vb)proj file. Overrides --target.");
                return;
            }

            FileInfo target;
            //  --dir -d
            if (wantDirectory)
            {
                // If we have the directory option then we should use that directory as the starting point instead of the current directory
                var directoryArgIndex = (args as List<string>).FindIndex(s => _commands[0].Equals(s, StringComparison.OrdinalIgnoreCase));
                if (directoryArgIndex < 0) directoryArgIndex = (args as List<string>).FindIndex(s => s == "--dir");
                directoryArgIndex += 1;

                // Not sure how this will work on WSL, but it gets weird using git-bash
                currentPath = args.ElementAt(directoryArgIndex);
                // For cygwin, git-bash, etc.
                currentPath = currentPath.Replace('/', '\\');
                foreach (var drive in Environment.GetLogicalDrives())
                {
                    // TODO - how do you handle /some_root_directory if you're using WSL or MSYS...
                    var fakeRootPath = $@"\{drive.Replace(":", string.Empty)}";
                    if (currentPath.StartsWith(fakeRootPath, StringComparison.OrdinalIgnoreCase))
                        currentPath = currentPath.Replace(fakeRootPath, $@"{drive}", StringComparison.OrdinalIgnoreCase);

                }
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
                if (_verbose) Console.WriteLine($"INFO:: Searching directory {currentPath}");
            }
            else if (_verbose) Console.WriteLine("INFO:: Searching current directory");

            // To debug a certain sln file hard code in the path to test
            // currentPath = @"c:\dev\test\WebApplication5"

            // --target --project
            // Get the most recently accessed solution/project file or return null if none
            var fileName = "";
            if (wantSln || wantProj)
            {
                int argIndex = -1;
                if (wantSln)
                {
                    argIndex = (args as List<string>).FindIndex(s => _commands[1].Equals(s, StringComparison.OrdinalIgnoreCase));
                    if (argIndex < 0) argIndex = (args as List<string>).FindIndex(s => s == "--target");
                }

                if (wantProj)
                {
                    argIndex = (args as List<string>).FindIndex(s => _commands[2].Equals(s, StringComparison.OrdinalIgnoreCase));
                    if (argIndex < 0) argIndex = (args as List<string>).FindIndex(s => s == "--project");
                }
                // if we have the --target option then we should try finding what was passed in
                if (argIndex > -1) fileName = args.ElementAt(argIndex + 1);

                var projectFiles = new[] { Csproj, Vbproj };
                if (_verbose)
                {
                    Console.WriteLine($"Looking for {fileName}{(wantSln ? Sln : ".[cs/vb]proj")}");
                    var potentialMatches = new DirectoryInfo(currentPath).GetFiles()
                    .Where(x => wantSln && x.Extension == Sln || wantProj && (x.Extension == Csproj || x.Extension == Vbproj))
                    .OrderByDescending(x => wantProj && projectFiles.Contains(x.Extension) || !wantProj && x.Extension == Sln) // If we are looking for a project file, have those in order first (t in true is > f in false)
                    .ThenBy(x => x.LastAccessTimeUtc)
                    .Select(x => x.Name.Replace(x.Extension, string.Empty));
                    foreach (var f in potentialMatches) Console.WriteLine(f.Equals(fileName, StringComparison.OrdinalIgnoreCase) ? $"{f,-20} <MATCH>" : f);
                }

                target = new DirectoryInfo(currentPath).GetFiles()
                .Where(x => wantSln && x.Extension == Sln || wantProj && x.Extension == Csproj || x.Extension == Vbproj)
                .OrderByDescending(x => wantProj && projectFiles.Contains(x.Extension) || !wantProj && x.Extension == Sln)
                .ThenBy(x => x.LastAccessTimeUtc)
                .FirstOrDefault(x => x.Name.Replace(x.Extension, string.Empty)
                    .Equals(fileName, StringComparison.OrdinalIgnoreCase)
                    || x.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                target = new DirectoryInfo(currentPath).GetFiles()
                    .Where(x => x.Extension == Sln)
                    .OrderBy(x => x.LastAccessTimeUtc)
                    .FirstOrDefault();
            }

            if (target == null)
            {
                Console.WriteLine($"ERROR:: Could not find file {target}.");
                return;
            }

            // Prefer VS2019 then downgrade to VS2017 if not there
            // "C:\Program Files (x86)\Microsoft Visual Studio"
            var devEnvPath = @$"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)}\Microsoft Visual Studio";
            var vsDirectory = new DirectoryInfo(devEnvPath).GetDirectories().OrderByDescending(d => d.Name)
                .FirstOrDefault(d => d.Name == "2019" || d.Name == "2017");
            if (vsDirectory == null)
            {
                Console.WriteLine($"ERROR:: Cannot find Visual Studio Community, Professional nor Enterprise 2019/2017 in {devEnvPath}");
                return;
            }

            // Where is VS - Community, Professional or Enterprise?
            vsDirectory = vsDirectory.GetDirectories().FirstOrDefault(dir => dir.Name == "Community" || dir.Name == "Professional" || dir.Name == "Enterprise");
            if (vsDirectory == null)
            {
                Console.WriteLine($"ERROR:: Neither Visual Studio Community, Professional nor Enterprise can be found in {devEnvPath}");
                return;
            }
            devEnvPath = vsDirectory.GetDirectories(@"Common7\IDE").First().FullName;
            // Call VS in a new process and return to the shell
            Console.WriteLine($"{target.Name,-20} : Opening this file! ");

            // Try to impersonate the current logged in user if no username
            var asUser = wantImpersonate || wantUser;

            var userName = wantUser
                ? args.ElementAt((args as List<string>).IndexOf(_commands[5]) + 1)
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
                if (_verbose) Console.WriteLine($"INFO:: Opening as {userName}");
                Console.WriteLine();
                Console.Write($"Enter password for {userName}: ");
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
            startInfo.Arguments = target.FullName.Contains(' ', StringComparison.Ordinal)
                ? $"\"{target.FullName}\""
                : target.FullName;

            if (_verbose) Console.WriteLine($"INFO:: Executing : {startInfo.FileName + " " + startInfo.Arguments,+20}");
            Process.Start(startInfo);
        }
    }
}