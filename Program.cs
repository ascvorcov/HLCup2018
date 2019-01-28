using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using hlcup2018.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using hlcup2018.Controllers;
using System.Linq;

public class Program
{
    public static void Main(string[] args)
    {
        var folder = args.Length > 0 ? args[0] : null;
        var loader = new StorageLoader(Storage.Instance);
        loader.Load(folder == null ? "/tmp/data/" : @"C:\Old\MyProjects\core\hlcup2018\" + folder + @"\data");
        GetHostBuilder().Build().Run();
    }

    public static IWebHostBuilder GetHostBuilder(string port = "80") => new WebHostBuilder()
        .UseKestrel(o => o.Limits.MaxConcurrentConnections = 1000)
        .UseUrls($"http://*:{port}")
        .Configure(cfg => cfg.Run(ctx => HandleRequest(ctx)));

    private static AccountsController controller = new AccountsController();

    private static Task HandleRequest(HttpContext ctx) {
        var body = AccountsController.empty;

        var parts = ctx.Request.Path.Value.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var l = parts.Length;

        if (parts[0] != "accounts") 
        {
            ctx.Response.StatusCode = StatusCodes.Status404NotFound;
            return Task.CompletedTask;
        }

        bool get = ctx.Request.Method == "GET";
        bool post = ctx.Request.Method == "POST";
        var p1 = parts.Last();

        switch (p1) {
            case "filter" when get:
                body = controller.Filter(ctx); // accounts/filter
                break;

            case "group" when get:
                body = controller.Group(ctx); // accounts/group
                break;

            case "recommend" when get:
                body = controller.Recommend(ctx, parts[1]); // accounts/5/recommend
                break;

            case "suggest" when get:
                body = controller.Suggest(ctx, parts[1]); // accounts/5/suggest
                break;

            case "new" when post:
                body = controller.New(ctx); // accounts/new
                break;

            case "likes" when post:
                body = controller.Likes(ctx);// accounts/likes
                break;

            default:
                if (l == 2 && p1[0] <= '9')
                    body = controller.Update(ctx, parts[1]);
                else
                    ctx.Response.StatusCode = StatusCodes.Status404NotFound;
                break;
        }

        if (body.Length > 0) {
            ctx.Response.ContentLength = body.Length;
            ctx.Response.Body.Write(body, 0, body.Length);
        }

        return Task.CompletedTask;
    }
}

