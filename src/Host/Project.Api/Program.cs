using Project.ServiceDefaults;
using Project.Server;
using Project.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateOnBuild = false;
    options.ValidateScopes = false;
});

builder.AddServiceDefaults();
builder.AddModules(new LegacyModule());

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("Default");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    // OpenAPI document via Microsoft.AspNetCore.OpenApi (Fase 1 plan). Scalar UI
    // mounted later when we replace the SPA pipeline.
}

app.MapModules();
app.MapDefaultEndpoints();
app.MapFallbackToFile("/index.html");

app.Run();

public partial class Program;