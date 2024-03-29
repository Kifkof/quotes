using Blazored.Toast;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.FileProviders;
using MudBlazor.Services;
using quotes_web.Domain.Authentication;
using quotes_web.Domain.ImportExport;
using quotes_web.Domain.Quoting;
using quotes_web.Persistence.Quoting;
using System.Net;

namespace quotes_web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddJsonFile("/config.json");
            builder.Services.AddRazorPages(opt => opt.RootDirectory = "/View");
            builder.Services.AddServerSideBlazor();

            builder.Services.AddMudServices();
            builder.Services.AddBlazoredToast();
            builder.Services.AddLocalization();

            builder.Services.AddQuoting(builder.Configuration);
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });
            builder.Services.AddAuthentication(builder.Configuration);
            builder.Services.AddImportExport();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }


            var imagePath = "/images";
            Directory.CreateDirectory(imagePath);
            var staticFileOptions = new StaticFileOptions
            {
                RequestPath = "/images",
                OnPrepareResponse = context =>
                {
                    context.Context.Response.Headers.Add("cache-control", new[] { "public,max-age=86400" });
                    context.Context.Response.Headers.Add("Expires", new[] { DateTime.UtcNow.AddDays(1).ToString("R") });
                },
                FileProvider = new PhysicalFileProvider(imagePath)
            };
            app.UseStaticFiles(staticFileOptions);
            app.UseStaticFiles();

            app.UseRouting();
            app.UseHttpsRedirection();
            app.UseForwardedHeaders();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseRequestLocalization("de-CH");

            app.MapBlazorHub();
            app.MapFallbackToPage("/_Host");

            EnsureDatabaseCreated(app);

            app.Run();

        }

        private static void EnsureDatabaseCreated(WebApplication app)
        {
            var serviceScopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();
            using var serviceScope = serviceScopeFactory.CreateScope();
            using var context = serviceScope.ServiceProvider.GetRequiredService<QuotesContext>();
            context.Database.EnsureCreated();
        }
    }
}