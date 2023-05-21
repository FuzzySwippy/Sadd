using System.Diagnostics;

namespace Sdownstall;

static class Program
{
    static void Main(string[] args)
    {
        Task<int> task = Task.Run(() => RouteArguments(args));
        task.Wait();
        Environment.Exit(task.Result);
    }

    static async Task<int> RouteArguments(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("No arguments provided.");
            return 1;
        }

        //Print help if requested
        if (args.Length == 1 && (args[0] == "--help" || args[0] == "-h"))
        {
            PrintHelp();
            return 0;
        }

        //Check for root privileges
        if (Environment.OSVersion.Platform != PlatformID.Unix)
        {
            Console.Error.WriteLine("This program can only be run on Linux.");
            return 1;
        }

        if (Environment.GetEnvironmentVariable("USER") != "root")
        {
            Console.Error.WriteLine("This program requires root privileges.");
            return 1;
        }

        //Get URL and output path
        string? url = GetArgumentValue(args, "--url") ?? GetArgumentValue(args, "-u");
        if (url == null)
        {
            Console.Error.WriteLine("No URL provided.");
            return 1;
        }

        string output = (GetArgumentValue(args, "--output") ?? GetArgumentValue(args, "-o")) ?? Environment.CurrentDirectory;
        return await DownloadAndInstall(url, output);
    }

    static string? GetArgumentValue(string[] args, string argumentName)
    {
        var index = Array.IndexOf(args, argumentName);
        if (index == -1) //Argument not found
            return null;

        if (index == args.Length - 1) //Argument is the last one
        {
            Console.Error.WriteLine($"No value provided for argument '{argumentName}'.");
            return null;
        }

        return args[index + 1];
    }

    static void PrintHelp()
    {
        Console.WriteLine("Sadd. Downloads a .deb file from a given URL and installs it using 'dpkg'. Requires root privileges.");
        Console.WriteLine("Usage: sdowntall [options]");
        Console.WriteLine("Options:");
        Console.WriteLine("  -u, --url <url>      URL to download the .deb file from. Required.");
        Console.WriteLine("  -o, --output <file>  Downloaded file path. If not provided, the file will be downloaded to the current directory.");
        Console.WriteLine("  -h, --help           Show help. Can only be used by itself without any other arguments.");
    }

    static async Task<int> DownloadAndInstall(string url, string output)
    {
        try
        {
            Console.WriteLine($"Downloading from: {url}...");
            int downloadResult = await Download(url, output);
            if (downloadResult != 0)
                return downloadResult;

            Console.WriteLine($"Installing from: {output}...");
            int installResult = Install(output);
            if (installResult != 0)
                return installResult;

            Console.WriteLine("Done! ^-^");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    static async Task<int> Download(string url, string output)
    {
        try
        {
            using HttpClient client = new();
            using HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            using Stream contentStream = await response.Content.ReadAsStreamAsync();
            using FileStream fileStream = File.Create(output);

            contentStream.CopyTo(fileStream);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to download file from {url}. {ex.Message}");
            return 1;
        }
    }

    static int Install(string path)
    {
        try
        {
            Process process = new();
            process.StartInfo.FileName = "dpkg";
            process.StartInfo.Arguments = $"-i {path} -v";
            process.Start();
            process.WaitForExit();

            Console.WriteLine($"dpkg exited with code {process.ExitCode}.");
            return process.ExitCode;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }
}

//JZP 954
//Voice