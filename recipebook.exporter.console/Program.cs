
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using recipebook.exporter.console.Application;
using recipebook.exporter.console.Orchestrators;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
DependencyInjectionConfig.Configure(builder.Services);
using IHost host = builder.Build();

var exporter = host.Services.GetRequiredService<RecipeExporter>();

string existingFolderId = "1DPFc6pPm_H_yrmAuAD5zNKYePFR8tEFW";
await exporter.Execute(existingFolderId);

await host.RunAsync();
