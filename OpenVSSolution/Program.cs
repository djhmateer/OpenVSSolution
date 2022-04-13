using System.Diagnostics;

// https://davemateer.com/coding/2018/11/08/Publish-dot-net-core-console-application.html for more information

var currentPath = Directory.GetCurrentDirectory();

// To debug a certain sln file hard code in the path to test
//var currentPath = @"c:\dev\test\dm-restaurant";

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

// Prefer VS2022, VS2019 then VS2017
var devEnvPath = @"C:\Program Files\Microsoft Visual Studio\";
if (Directory.Exists(devEnvPath))
{
    // VS2022 is x64 only
    var vsDirectoryVersion64 = new DirectoryInfo(devEnvPath).GetDirectories();
    if (vsDirectoryVersion64.Any(x => x.Name == "2022"))
    {
        devEnvPath += @"2022\";
    }
}
else
{
    // x86
    devEnvPath = @"C:\Program Files (x86)\Microsoft Visual Studio\";
    var vsDirectoryVersion = new DirectoryInfo(devEnvPath).GetDirectories();
    if (vsDirectoryVersion.Any(x => x.Name == "2019"))
        devEnvPath += @"2019\";
    else if (vsDirectoryVersion.Any(x => x.Name == "2017"))
        devEnvPath += @"2017\";
    else
    {
        Console.WriteLine(
            $"Neither Visual Studio Community, Professional nor Enterprise can be found");
        return;
    }
}


var vsDirectory = new DirectoryInfo(devEnvPath).GetDirectories();
if (vsDirectory.Any(x => x.Name == "Community"))
    devEnvPath += @"Community\Common7\IDE\";
else if (vsDirectory.Any(x => x.Name == "Professional"))
    devEnvPath += @"Professional\Common7\IDE\";
else if (vsDirectory.Any(x => x.Name == "Enterprise"))
    devEnvPath += @"Enterprise\Common7\IDE\";
else
{
    Console.WriteLine(
        $"Neither Visual Studio Community, Professional nor Enterprise can be found in {devEnvPath}");
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