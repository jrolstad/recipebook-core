using Google.Apis.Auth.OAuth2;
using Google.Apis.Docs.v1;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using recipebook.core.Managers;
using recipebook.core.Repositories;
using recipebook.entityframework;
using recipebook.exporter.console.Orchestrators;
using System.Text;

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

            builderServices.AddTransient((provider) =>
            {
                var configuration = provider.GetService<IConfiguration>();
                var appName = configuration["RecipebookExporter:GoogleAppName"];
                var apiKey = configuration["RecipebookExporter:GoogleApiKey"];

                var credential = CreateGoogleCredentials(apiKey);

                var service = new DriveService(new BaseClientService.Initializer
                {
                    ApplicationName = appName,
                    HttpClientInitializer = credential,
                });

                return service;
            });

            builderServices.AddTransient((provider) => {
                var configuration = provider.GetService<IConfiguration>();
                var appName = configuration["RecipebookExporter:GoogleAppName"];
                var apiKey = configuration["RecipebookExporter:GoogleApiKey"];

                var credential = CreateGoogleCredentials(apiKey);

                var service = new DocsService(new BaseClientService.Initializer
                {
                    ApplicationName = appName,
                    HttpClientInitializer = credential,

                });

                return service;
            });

        }

        private static UserCredential CreateGoogleCredentials(string? apiKey)
        {
            UserCredential credential;
            string[] scopes = { DriveService.Scope.Drive };
            string credPath = "token.json";
            credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(GenerateStreamFromString(apiKey)).Secrets,
                scopes,
                "user",
                CancellationToken.None,
                new FileDataStore(credPath, true)).Result;
            return credential;
        }

        private static Stream GenerateStreamFromString(string s)
        {
            // Create a memory stream
            MemoryStream stream = new MemoryStream();

            // Create a StreamWriter for writing to the memory stream
            using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(s);
                writer.Flush(); // Make sure all data is written to the memory stream

                // Reset the position of the stream to the beginning
                stream.Position = 0;
            }

            // Return the stream with the string content
            return stream;
        }
    }
}
