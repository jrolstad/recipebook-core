
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using recipebook.exporter.console.Application;
using recipebook.exporter.console.Orchestrators;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
DependencyInjectionConfig.Configure(builder.Services);
using IHost host = builder.Build();

var exporter = host.Services.GetRequiredService<RecipeExporter>();
await exporter.Execute();

await host.RunAsync();
