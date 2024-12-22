using Google.Apis.Docs.v1;
using Google.Apis.Docs.v1.Data;
using Google.Apis.Drive.v3;
using recipebook.core.Managers;
using recipebook.core.Models;
using System.ComponentModel.DataAnnotations;
using System.Text;

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
                .Select(r => WriteRecipeToDocument(r, parentFolder));

            await Task.WhenAll(processingTasks);
        }

        private async Task WriteRecipeToDocument(Recipe recipe, string parentFolder)
        {
            var folderId = GetOrCreateFolder(recipe.Category, parentFolder);
            var docId = CreateDoc(recipe.Name, folderId);
            AddContentToDoc(docId, recipe);

            Console.WriteLine($"Exported {recipe.Category}/{recipe.Name} to doc {docId}");

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

        private void AddContentToDoc(string docId, Recipe recipe)
        {

            var request = new BatchUpdateDocumentRequest
            {
                Requests = WriteContent(recipe)
            };

            var response = docService.Documents
                .BatchUpdate(request, docId)
                .Execute();

            var document = docService.Documents.Get(docId).Execute();
        }


        private static Request[] WriteContent(Recipe recipe)
        {
            var titleStartIndex = 1;
            var titleEndIndex = recipe.Name.Length+2;

            var servingsStartIndex = titleEndIndex + 1;
            var servingsEndIndex = servingsStartIndex + $"Servings: {recipe.Servings}".Length+2;

            var ingredientsHeaderStartIndex = servingsEndIndex + 1;
            var ingredientsHeaderEndIndex = ingredientsHeaderStartIndex + "Ingredients".Length+2;

            var ingredientsContentStartIndex = ingredientsHeaderEndIndex + 1;
            var ingredientsContentEndIndex = ingredientsContentStartIndex + recipe.Ingredients?.Trim().Length+2;

            var directionsHeaderStartIndex = ingredientsContentEndIndex + 1;
            var directionsHeaderEndIndex = directionsHeaderStartIndex + "Directions".Length + 2;

            return new[]
                            {
                    new Request
                    {
                        InsertText = new InsertTextRequest
                        {
                            Location = new Location { Index = 1 },
                            Text = FormatRecipeContent(recipe)
                        }
                    },
                    new Request
                    {
                        UpdateParagraphStyle = new UpdateParagraphStyleRequest
                        {
                            Range = new Google.Apis.Docs.v1.Data.Range { StartIndex = titleStartIndex, EndIndex = titleEndIndex },
                            ParagraphStyle = new ParagraphStyle
                            {
                                NamedStyleType = "TITLE"
                            },
                            Fields = "namedStyleType"
                        }
                    },
                    new Request
                    {
                        UpdateParagraphStyle = new UpdateParagraphStyleRequest
                        {
                            Range = new Google.Apis.Docs.v1.Data.Range { StartIndex = servingsStartIndex, EndIndex = servingsEndIndex },
                            ParagraphStyle = new ParagraphStyle
                            {
                                NamedStyleType = "SUBTITLE"
                            },
                            Fields = "namedStyleType"
                        }
                    },
                    new Request
                    {
                        UpdateParagraphStyle = new UpdateParagraphStyleRequest
                        {
                            Range = new Google.Apis.Docs.v1.Data.Range { StartIndex = ingredientsHeaderStartIndex, EndIndex = ingredientsHeaderEndIndex },
                            ParagraphStyle = new ParagraphStyle
                            {
                                NamedStyleType = "HEADING_1"
                            },
                            Fields = "namedStyleType"
                        }
                    },
                    new Request
                    {
                        CreateParagraphBullets = new CreateParagraphBulletsRequest
                        {
                            Range = new Google.Apis.Docs.v1.Data.Range { StartIndex = ingredientsContentStartIndex, EndIndex = ingredientsContentEndIndex },
                            BulletPreset = "BULLET_DISC_CIRCLE_SQUARE"
                        }
                    },
                    new Request
                    {
                        UpdateParagraphStyle = new UpdateParagraphStyleRequest
                        {
                            Range = new Google.Apis.Docs.v1.Data.Range { StartIndex = directionsHeaderStartIndex, EndIndex = directionsHeaderEndIndex },
                            ParagraphStyle = new ParagraphStyle
                            {
                                NamedStyleType = "HEADING_1"
                            },
                            Fields = "namedStyleType"
                        }
                    },
                };
        }

        private static string FormatRecipeContent(Recipe recipe)
        {
            var builder = new StringBuilder();

            builder.AppendLine(recipe.Name);
            builder.AppendLine($"Servings: {recipe.Servings}");
            builder.AppendLine("Ingredients");
            builder.AppendLine(recipe.Ingredients?.Trim());
            builder.AppendLine("Directions");
            builder.AppendLine(recipe.Directions);
            builder.AppendLine("Source");
            builder.AppendLine(recipe.Source);
            

            return builder.ToString();
        }
    }
}
