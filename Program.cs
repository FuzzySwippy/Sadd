using System.Diagnostics;

namespace Sadd;

static class Program
{
    static void Main(string[] args) => Environment.Exit(RouteArguments(args));

    static int RouteArguments(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("No arguments provided.");
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
            Console.WriteLine("This program can only be run on Linux.");
            return 1;
        }

        if (Environment.GetEnvironmentVariable("USER") != "root")
        {
            Console.WriteLine("This program requires root privileges.");
            return 1;
        }

        //Get URL and output path
        string? url = GetArgumentValue(args, "--url") ?? GetArgumentValue(args, "-u");
        if (url == null)
        {
			if (args.Length == 2)
				return DownloadAndInstall(args[0], args[1]);

			Console.WriteLine("No URL provided.");
            return 1;
        }

        string output = (GetArgumentValue(args, "--output") ?? GetArgumentValue(args, "-o")) ?? Environment.CurrentDirectory;
        return DownloadAndInstall(url, output);
    }

    static string? GetArgumentValue(string[] args, string argumentName)
    {
        var index = Array.IndexOf(args, argumentName);
        if (index == -1) //Argument not found
            return null;

        if (index == args.Length - 1) //Argument is the last one
        {
            Console.WriteLine($"No value provided for argument '{argumentName}'.");
            return null;
        }

        return args[index + 1];
    }

    static void PrintHelp()
    {
        Console.WriteLine("Sadd. Downloads a .deb file from a given URL and installs it using 'dpkg'. Requires root privileges.");
        Console.WriteLine("Usage: sadd [options]");
        Console.WriteLine("sadd [url] [output]");
        Console.WriteLine("Options:");
        Console.WriteLine("  -u, --url <url>      URL to download the .deb file from. Required.");
        Console.WriteLine("  -o, --output <file>  Downloaded file path. If not provided, the file will be downloaded to the current directory.");
        Console.WriteLine("  -h, --help           Show help. Can only be used by itself without any other arguments.");
		
    }

    static int DownloadAndInstall(string url, string output)
    {
        try
        {
            int downloadResult = Download(url, output).GetAwaiter().GetResult();
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
            Console.WriteLine(ex.Message);
            return 1;
        }
    }

    static async Task<int> Download(string url, string output)
    {
        try
        {
			if (Directory.Exists(output))
				output += $"{DateTimeOffset.UtcNow.UtcTicks}.deb";


			Console.WriteLine($"Downloading from: {url}...");
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
            Console.WriteLine($"Failed to download file from {url}. {ex.Message}");
            return 1;
        }
    }

    static int Install(string path)
    {
        try
        {
            Process process = new();
            process.StartInfo.FileName = "dpkg";
            process.StartInfo.Arguments = $"-i \"{path}\"";
            process.Start();
            process.WaitForExit();

            Console.WriteLine($"dpkg exited with code {process.ExitCode}.");
            return process.ExitCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return 1;
        }
    }
}