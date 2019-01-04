using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;
using Microsoft.Extensions.Logging;
using hlcup2018.Models;

public class Program
{
    public static void Main(string[] args)
    {
        var folder = args[0];
        var loader = new StorageLoader(Storage.Instance);
        loader.Load(@"C:\Old\MyProjects\core\hlcup2018\" + folder + @"\data");
        BuildWebHost(args).Run();
    }

    public static IWebHost BuildWebHost(string[] args) =>
        WebHost.CreateDefaultBuilder(args)
            .UseStartup<Startup>()
            .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.None))
            .UseUrls("http://0.0.0.0:80")
            .Build();
}

