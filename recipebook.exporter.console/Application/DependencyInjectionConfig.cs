using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using recipebook.core.Managers;
using recipebook.core.Repositories;
using recipebook.entityframework;
using recipebook.exporter.console.Orchestrators;

namespace recipebook.exporter.console.Application
{
    public class DependencyInjectionConfig
    {
        public static void Configure(IServiceCollection builderServices)
        {

            builderServices.AddTransient<CategoryManager>();
            builderServices.AddTransient<CategoryRepository>();

            builderServices.AddTransient<RecipeManager>();
            builderServices.AddTransient<RecipeRepository>();

            builderServices.AddTransient<HealthManager>();
            builderServices.AddTransient<AuthorizationManager>();

            builderServices.AddTransient<RecipeExporter>();


            builderServices.AddDbContext<RecipeBookDbContext>((provider, builder) =>
            {
                var configuration = provider.GetService<IConfiguration>();
                var accountEndpoint = configuration["RecipeBookDb:Endpoint"];
                var accountKey = configuration["RecipeBookDb:AccountKey"];
                var databaseName = configuration["RecipeBookDb:DatabaseName"];

                builder.UseCosmos(accountEndpoint, accountKey, databaseName);

            });

        }
    }
}
