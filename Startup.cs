using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMvc()
        .AddMvcOptions(options =>
        {
            options.AllowEmptyInputInBodyModelBinding = true;
        })
        .AddJsonOptions(options =>
        {
            //options.SerializerSettings.StringEscapeHandling = Newtonsoft.Json.StringEscapeHandling.EscapeNonAscii;
        });
    }

    public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
    {
        var serverAddressesFeature = app.ServerFeatures.Get<IServerAddressesFeature>();
        app.UseMvc();
    }
}
