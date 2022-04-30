using System.Reflection;
using CvLab.TelegramBot.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace RQ.Bot.Extensions;

public static class RestApiExtension
{
    public static IHostBuilder UseRestAPI(this IHostBuilder builder, params string[] args) => builder
        .ConfigureServices(services =>
        {
            services.AddTransient<QuestionariesArchiveController>();
            
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
            });
        }).ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.CaptureStartupErrors(true);
            webBuilder.Configure(app =>
            {
                app.UseSwagger(c => { c.SerializeAsV2 = true; });
                app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "TG API V1"); });
                app.UseDeveloperExceptionPage();
                app.UseRouting();
                app.UseCors();
                
                
                app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
            });

            webBuilder.UseKestrel((ctx, opt) =>
            {
                var servicePort = ctx.Configuration["servicePort"];

                if (!int.TryParse(servicePort, out var port))
                    throw new InvalidProgramException("Specify --servicePort argument");

                opt.ListenAnyIP(port);
            });
        });
}