
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using recipebook.exporter.console.Application;
using recipebook.exporter.console.Orchestrators;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
DependencyInjectionConfig.Configure(builder.Services);
using IHost host = builder.Build();

var exporter = host.Services.GetRequiredService<RecipeExporter>();

string existingFolderId = "1OQkf--V2KWEKHf-IfChQwtC_46S_fZ1A";
await exporter.Execute(existingFolderId);

await host.RunAsync();
