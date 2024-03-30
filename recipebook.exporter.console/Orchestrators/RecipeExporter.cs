using Google.Apis.Docs.v1;
using Google.Apis.Drive.v3;
using recipebook.core.Managers;
using recipebook.core.Models;

namespace recipebook.exporter.console.Orchestrators
{
    public class RecipeExporter
    {
        private readonly RecipeManager recipeManager;
        private readonly DriveService driveService;
        private readonly DocsService docService;

        public RecipeExporter(RecipeManager recipe, 
            DriveService driveService, 
            DocsService docService)
        {
            this.recipeManager = recipe;
            this.driveService = driveService;
            this.docService = docService;
        }
        public async Task Execute(string parentFolder)
        {
            var recipes = await this.recipeManager.Search(null, null);

            var processingTasks = recipes
                .Take(3)
                .Select(r=>WriteRecipeToDocument(r,parentFolder));

            await Task.WhenAll(processingTasks);
        }

        private async Task WriteRecipeToDocument(Recipe recipe, string parentFolder)
        {
            var folderId = GetOrCreateFolder(recipe.Category, parentFolder);
            var docId = CreateDoc(recipe.Name,folderId);
            
        }

        private string GetOrCreateFolder(string folderName, string parentFolderId)
        {
            var existing = GetExstingFolders(folderName, parentFolderId);

            if (!existing.Any())
            {
                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = folderName,
                    MimeType = "application/vnd.google-apps.folder",
                    Parents = string.IsNullOrEmpty(parentFolderId) ? null : new List<string> { parentFolderId }
                };

                var request = driveService.Files.Create(fileMetadata);
                request.Fields = "id";
                var file = request.Execute();
                return file.Id;
            }
            else
            {
                return existing.First().Id;
            }
        }

        private IList<Google.Apis.Drive.v3.Data.File> GetExstingFolders(string folderName, string parentFolderId)
        {
            var listRequest = driveService.Files.List();
            listRequest.Q = $"mimeType='application/vnd.google-apps.folder' and name='{folderName}'" +
                            (!string.IsNullOrEmpty(parentFolderId) ? $" and '{parentFolderId}' in parents" : "") +
                            " and trashed=false";
            listRequest.Spaces = "drive";
            listRequest.Fields = "files(id, name)";
            var files = listRequest.Execute().Files;
            return files;
        }

        private string CreateDoc(string documentName, string parentFolderId)
        {
            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = documentName,
                MimeType = "application/vnd.google-apps.document",
                Parents = [parentFolderId]
            };

            var request = driveService.Files.Create(fileMetadata);
            request.Fields = "id";
            var doc = request.Execute();
            return doc.Id;
        }
    }

}
