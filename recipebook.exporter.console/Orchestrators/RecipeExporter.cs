using recipebook.core.Managers;
using recipebook.core.Models;

namespace recipebook.exporter.console.Orchestrators
{
    public class RecipeExporter
    {
        private readonly RecipeManager recipeManager;

        public RecipeExporter(RecipeManager recipe)
        {
            this.recipeManager = recipe;
        }
        public async Task Execute()
        {
            var recipes = await this.recipeManager.Search(null, null);

            var processingTasks = recipes
                .Select(WriteRecipeToDocument)
                .AsParallel();

            await Task.WhenAll(processingTasks);
        }

        private async Task WriteRecipeToDocument(Recipe recipe)
        {
            System.Console.WriteLine(recipe.Name);
        }
    }
}
