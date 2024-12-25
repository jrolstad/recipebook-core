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
                .OrderBy(r=>r.Category).ThenBy(r=>r.Name)
                //.Take(3)
                //.AsParallel()
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

            var formatRequest = new BatchUpdateDocumentRequest
            {
                Requests = FormatContent(document,recipe)
            };
            var formatResponse = docService.Documents
                .BatchUpdate(formatRequest, docId)
                .Execute();

            var formatIngredientHeaderRequest = new BatchUpdateDocumentRequest
            {
                Requests = FormatIngredientsHeaders(document, recipe)
            };
            if (formatIngredientHeaderRequest.Requests.Any())
            {
                var formatIngredientsHeadersResponse = docService.Documents
                    .BatchUpdate(formatIngredientHeaderRequest, docId)
                    .Execute();
            }

          
        }


        private static Request[] WriteContent(Recipe recipe)
        {
            return new[]
                {
                    new Request
                    {
                        InsertText = new InsertTextRequest
                        {
                            Location = new Location { Index = 1 },
                            Text = WriteRecipeContent(recipe)
                        }
                    },
                };
        }

        private static string WriteRecipeContent(Recipe recipe)
        {
            var builder = new StringBuilder();

            builder.AppendLine(recipe.Name);
            builder.AppendLine($"Servings: {recipe.Servings}");
            builder.AppendLine("Ingredients");
            builder.AppendLine(recipe.Ingredients?.Trim());
            builder.AppendLine("Directions");
            builder.AppendLine(RemoveEmptyLines(recipe.Directions));
            builder.AppendLine("Source");
            if (string.IsNullOrWhiteSpace(recipe.Source))
            {
                builder.AppendLine($"https://recipes.rolstadfamily.com/recipes/{recipe.Id}");
            }
            else
            {
                builder.AppendLine(recipe.Source);
            }


            return builder.ToString();
        }
        private static string RemoveEmptyLines(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            string result = string.Join("\n", value.Split(new[] { '\n' }, StringSplitOptions.None)
                                                   .Where(line => !string.IsNullOrWhiteSpace(line)));
            return result;
        }

        private static Request[] FormatContent(Document document, Recipe recipe)
        {
            var servingsHeader = GetHeader("Servings", document.Body.Content);
            var ingredientsHeader = GetHeader("Ingredients", document.Body.Content);
            var directionsHeader = GetHeader("Directions", document.Body.Content);
            var sourceHeader = GetHeader("Source",document.Body.Content);

            var titleStartIndex = document.Body.Content[1].StartIndex;
            var titleEndIndex = document.Body.Content[1].EndIndex;

            var servingsStartIndex = servingsHeader.StartIndex;
            var servingsEndIndex = servingsHeader.EndIndex;

            var ingredientsHeaderStartIndex = ingredientsHeader.StartIndex;
            var ingredientsHeaderEndIndex = ingredientsHeader.EndIndex;

            var ingredientsContentStartIndex = ingredientsHeader.EndIndex + 1;
            var ingredientsContentEndIndex = directionsHeader.StartIndex - 1;

            var directionsHeaderStartIndex = directionsHeader.StartIndex;
            var directionsHeaderEndIndex = directionsHeader.EndIndex;

            var directionsContentStartIndex = directionsHeader.EndIndex + 1;
            var directionsContentEndIndex = sourceHeader.StartIndex - 1;

            var sourceHeaderStartIndex = sourceHeader.StartIndex;
            var sourceHeaderEndIndex = sourceHeader.EndIndex;

            
            var result = new List<Request>()
                {
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
                    new Request
                    {
                        CreateParagraphBullets = new CreateParagraphBulletsRequest
                        {
                            Range = new Google.Apis.Docs.v1.Data.Range { StartIndex = directionsContentStartIndex, EndIndex = directionsContentEndIndex },
                            BulletPreset = "NUMBERED_DECIMAL_NESTED"
                        },
                       
                    },
                    new Request
                    {
                        UpdateParagraphStyle = new UpdateParagraphStyleRequest
                        {
                            Range = new Google.Apis.Docs.v1.Data.Range { StartIndex = sourceHeaderStartIndex, EndIndex = sourceHeaderEndIndex },
                            ParagraphStyle = new ParagraphStyle
                            {
                                NamedStyleType = "HEADING_1"
                            },
                            Fields = "namedStyleType"
                        }
                    },
                };

            return result.ToArray();
        }
        private static Request[] FormatIngredientsHeaders(Document document, Recipe recipe)
        {
            var ingredientsHeaders = GetIngredientsHeaders(document.Body.Content);

            return ingredientsHeaders
                .Select(h => new[]
                {
                    new Request
                    {
                        DeleteParagraphBullets = new DeleteParagraphBulletsRequest
                        {
                            Range = new Google.Apis.Docs.v1.Data.Range { StartIndex = h.StartIndex, EndIndex = h.EndIndex }
                        }
                    },
                    new Request
                    {

                        UpdateParagraphStyle = new UpdateParagraphStyleRequest
                        {
                            Range = new Google.Apis.Docs.v1.Data.Range { StartIndex = h.StartIndex, EndIndex = h.EndIndex },
                            ParagraphStyle = new ParagraphStyle
                            {
                                NamedStyleType = "HEADING_2"
                            },
                            Fields = "namedStyleType"
                        }
                    }
                })
                .SelectMany(h => h)
                .ToArray();
        }
        private static Request[] RemoveHashtagFromHeaders(Document document, Recipe recipe)
        {
            var ingredientsHeaders = GetIngredientsHeaders(document.Body.Content);

            return ingredientsHeaders
                .Select(h => new[]
                {
                    new Request
                    {
                        ReplaceAllText = new ReplaceAllTextRequest
                        {
                            ContainsText = new SubstringMatchCriteria
                            {
                                Text = h.Paragraph.Elements[0].TextRun.Content,
                                MatchCase = true
                            },
                            ReplaceText = h.Paragraph.Elements[0].TextRun.Content.Substring(1) // Remove the '#' character
                        }
                    },
                    new Request
                    {
                        DeleteParagraphBullets = new DeleteParagraphBulletsRequest
                        {
                            Range = new Google.Apis.Docs.v1.Data.Range { StartIndex = h.StartIndex, EndIndex = h.EndIndex-1 }
                        }
                    },
                })
                .SelectMany(h => h)
                .ToArray();
        }

        private static StructuralElement GetHeader(string name, IList<StructuralElement> elements)
        {
            return elements
                .Where(e=>e.Paragraph?.Elements!=null)
                .First(e => e.Paragraph.Elements.Any(pe => pe.TextRun.Content.StartsWith(name)));
        }
        private static ICollection<StructuralElement> GetIngredientsHeaders(IList<StructuralElement> elements)
        {
            return elements
                .Where(e => e.Paragraph?.Elements != null)
                .Where(e => e.Paragraph.Elements.Any(pe => pe.TextRun.Content.StartsWith("#")))
                .ToList();
        }
    }
}
