﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using recipebook.core.Models;
using recipebook.functions.Functions;
using recipebook.functions.test.TestUtility;
using recipebook.functions.test.TestUtility.Extensions;
using Xunit;

namespace recipebook.functions.test
{
    public class RecipeFunctionTests
    {
        [Fact]
        public async Task GetAll_NoParameters_ReturnsAll()
        {
            // Given
            var root = TestCompositionRoot.Create();

            root.WithRecipe("recipe-one");
            root.WithRecipe("recipe-two");

            var api = root.Get<RecipeFunction>();

            // When
            var result = await api.GetAll(root.GetRequest(), root.CoreLogger(), root.AuthenticatedUser());

            // Then
            var data = result.AssertIsOkResultWithValue<IReadOnlyList<Recipe>>();
            Assert.Equal(2,data.Count);
        }

        [Fact]
        public async Task GetAll_Category_ReturnsRecipesInCategory()
        {
            // Given
            var root = TestCompositionRoot.Create();

            root.WithRecipe("recipe-one", category:"cat-1");
            root.WithRecipe("recipe-two");

            var api = root.Get<RecipeFunction>();

            var request = root.GetRequest()
                .WithCategoryParameter("cat-1");

            // When
            var result = await api.GetAll(request, root.CoreLogger(), root.AuthenticatedUser());

            // Then
            var data = result.AssertIsOkResultWithValue<IReadOnlyList<Recipe>>();
            Assert.Equal(1, data.Count);
            Assert.Equal("recipe-one", data[0].Name);
        }

        [Theory]
        [InlineData("two")]
        [InlineData("TWO")]
        [InlineData("tWo")]
        public async Task GetAll_Criteria_ReturnsRecipesMatchingName(string searchCriteria)
        {
            // Given
            var root = TestCompositionRoot.Create();

            root.WithRecipe("recipe-one", category: "cat-1");
            root.WithRecipe("recipe-two", category:"cat-2");

            var api = root.Get<RecipeFunction>();

            var request = root.GetRequest()
                .WithSearchCriteriaParameter(searchCriteria);
            // When
            var result = await api.GetAll(request, root.CoreLogger(), root.AuthenticatedUser());

            // Then
            var data = result.AssertIsOkResultWithValue<IReadOnlyList<Recipe>>();
            Assert.Equal(1, data.Count);
            Assert.Equal("recipe-two", data[0].Name);
        }

        [Fact]
        public async Task GetAll_CriteriaAndCategory_ReturnsRecipesMatchingBoth()
        {
            // Given
            var root = TestCompositionRoot.Create();

            root.WithRecipe("recipe-one", category: "cat-1");
            root.WithRecipe("recipe-two", category:"something-else");
            root.WithRecipe("recipe-three", category: "cat-1");

            var api = root.Get<RecipeFunction>();

            var request = root.GetRequest()
                .WithSearchCriteriaParameter("recipe")
                .WithCategoryParameter("cat-1");

            // When
            var result = await api.GetAll(request, root.CoreLogger(), root.AuthenticatedUser());

            // Then
            var data = result.AssertIsOkResultWithValue<IReadOnlyList<Recipe>>();
            Assert.Equal(2, data.Count);
            Assert.Contains("recipe-one", data.Select(r => r.Name));
            Assert.Contains("recipe-three", data.Select(r => r.Name));
        }

        [Fact]
        public async Task Get_IdExists_ReturnsItem()
        {
            // Given
            var root = TestCompositionRoot.Create();

            root.WithRecipe("recipe-one");
            var recipe2Id = root.WithRecipe("recipe-two",
                "something to add",
                directions:"what to do",
                servings:1,
                source:"the-test",
                rating:3,
                category:"the-category");

            var api = root.Get<RecipeFunction>();

            // When
            var result = await api.GetItem(root.GetRequest(), recipe2Id, root.CoreLogger(), root.AuthenticatedUser());

            // Then
            var data = result.AssertIsOkResultWithValue<Recipe>();
            Assert.False(string.IsNullOrWhiteSpace(data.Id));
            Assert.Equal("recipe-two", data.Name);
            Assert.Equal("something to add", data.Ingredients);
            Assert.Equal("what to do", data.Directions);
            Assert.Equal(1, data.Servings);
            Assert.Equal("the-test", data.Source);
            Assert.Equal(3, data.Rating);
            Assert.Equal("the-category", data.Category);
        }

        [Fact]
        public async Task Create_AuthenticatedUserAndValidRecipe_SuccessfullySaves()
        {
            // Given
            var root = TestCompositionRoot.Create();
            root.WithAuthenticatedUser("the-user");

            var api = root.Get<RecipeFunction>();
            var postData = new Recipe
            {
                Name = "the-new-item",
                Ingredients = "something to add",
                Directions = "what to do",
                Servings = 1,
                Source = "the-test",
                Rating= 3,
                Category = "the-category"
            };

            // When
            var createResult = await api.Create(root.PostRequest(postData), root.CoreLogger(), root.AuthenticatedUser());

            // Then
            var getResult = await api.GetAll(root.GetRequest(), root.CoreLogger(), root.AuthenticatedUser());
            var getData = getResult.AssertIsOkResultWithValue<ICollection<Recipe>>();

            var matchingResult = getData.SingleOrDefault(r => r.Name == "the-new-item");
            Assert.NotNull(matchingResult);
            Assert.Equal("something to add", matchingResult.Ingredients);
            Assert.Equal("what to do", matchingResult.Directions);
            Assert.Equal(1, matchingResult.Servings);
            Assert.Equal("the-test", matchingResult.Source);
            Assert.Equal(3, matchingResult.Rating);
            Assert.Equal("the-category", matchingResult.Category);
        }


        [Fact]
        public async Task Create_UnAuthenticatedUserValidRecipe_ReturnsForbid()
        {
            // Given
            var root = TestCompositionRoot.Create();

            var api = root.Get<RecipeFunction>();
            var postData = new Recipe
            {
                Name = "the-new-item",
                Ingredients = "something to add",
                Directions = "what to do",
                Servings = 1,
                Source = "the-test",
                Rating = 3,
                Category = "the-category"
            };

            // When
            var createResult = await api.Create(root.PostRequest(postData), root.CoreLogger(), root.AuthenticatedUser());

            // Then
            Assert.IsAssignableFrom<ForbidResult>(createResult);
        }

        [Fact]
        public async Task Update_AuthenticatdUserValidRecipe_SuccessfullySaves()
        {
            // Given
            var root = TestCompositionRoot.Create();
            root.WithAuthenticatedUser("the-user");
            root.WithRecipe(id: "the-identifier", name: "the-old-name");

            var api = root.Get<RecipeFunction>();
            var postData = new Recipe
            {
                Id = "the-identifier",
                Name = "the-new-item",
                Ingredients = "something to add",
                Directions = "what to do",
                Servings = 1,
                Source = "the-test",
                Rating = 3,
                Category = "the-category"
            };

            // When
            var updateResult = await api.Update(root.PutRequest(postData), root.CoreLogger(), root.AuthenticatedUser());

            // Then
            var getResult = await api.GetAll(root.GetRequest(), root.CoreLogger(), root.AuthenticatedUser());
            var getData = getResult.AssertIsOkResultWithValue<ICollection<Recipe>>();

            var matchingResult = getData.SingleOrDefault(r => r.Name == "the-new-item");
            Assert.NotNull(matchingResult);
            Assert.Equal("the-identifier", matchingResult.Id);
            Assert.Equal("something to add", matchingResult.Ingredients);
            Assert.Equal("what to do", matchingResult.Directions);
            Assert.Equal(1, matchingResult.Servings);
            Assert.Equal("the-test", matchingResult.Source);
            Assert.Equal(3, matchingResult.Rating);
            Assert.Equal("the-category", matchingResult.Category);
        }

        [Fact]
        public async Task Update_UnAuthenticatdUserValidRecipe_ReturnsForbidden()
        {
            // Given
            var root = TestCompositionRoot.Create();
            root.WithRecipe(id: "the-identifier", name: "the-old-name");

            var api = root.Get<RecipeFunction>();
            var postData = new Recipe
            {
                Id = "the-identifier",
                Name = "the-new-item",
                Ingredients = "something to add",
                Directions = "what to do",
                Servings = 1,
                Source = "the-test",
                Rating = 3,
                Category = "the-category"
            };

            // When
            var updateResult = await api.Update(root.PutRequest(postData), root.CoreLogger(), root.AuthenticatedUser());

            // Then
            Assert.IsAssignableFrom<ForbidResult>(updateResult);
        }

        [Fact]
        public async Task Delete_UnAuthenticatdUserValidRecipe_ReturnsForbidden()
        {
            // Given
            var root = TestCompositionRoot.Create();
            root.WithRecipe(id: "the-identifier", name: "the-old-name");

            var api = root.Get<RecipeFunction>();
            // When
            var deleteResult = await api.Delete(root.DeleteRequest(), "the-identifier", root.CoreLogger(), root.AuthenticatedUser());

            // Then
            Assert.IsAssignableFrom<ForbidResult>(deleteResult);
        }

        [Fact]
        public async Task Delete_AuthenticatedUserValidRecipe_RecipeDoesNoShowInSearchResults()
        {
            // Given
            var root = TestCompositionRoot.Create();
            root.WithAuthenticatedUser("the-user");
            root.WithRecipe(id: "the-identifier", name: "the-old-name");

            var api = root.Get<RecipeFunction>();

            // When
            var deleteResult = await api.Delete(root.DeleteRequest(), "the-identifier", root.CoreLogger(), root.AuthenticatedUser());

            // Then
            Assert.IsAssignableFrom<OkResult>(deleteResult);

            var getResult = await api.GetAll(root.GetRequest(), root.CoreLogger(), root.AuthenticatedUser());
            var getData = getResult.AssertIsOkResultWithValue<ICollection<Recipe>>();

            Assert.DoesNotContain("the-identifier", getData.Select(r => r.Id).ToList());
        }
    }
}