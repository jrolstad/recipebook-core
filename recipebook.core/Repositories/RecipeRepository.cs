﻿using System;
using System.Collections.Generic;
using System.Linq;
using recipebook.core.Models;
using recipebook.entityframework;
using System.Collections.Immutable;

namespace recipebook.core.Repositories
{
    public class RecipeRepository
    {
        private readonly RecipeBookDbContext _dbContext;

        public RecipeRepository(RecipeBookDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public IReadOnlyCollection<Recipe> Get()
        {
            var data = _dbContext.Recipes
                .Select(Map)
                .ToImmutableList();

            return data;
        }

        public Recipe Get(string id)
        {
            var item = _dbContext.Recipes.Find(id);
            var mappedItem = Map(item);

            return mappedItem;
        }

        public Recipe Create(Recipe toCreate)
        {
            AssignIdentifier(toCreate);
            var dbModel = Map(toCreate);
            
            _dbContext.Recipes.Add(dbModel);
            _dbContext.SaveChanges();

            return Get(toCreate.Id);
        }

        private static void AssignIdentifier(Recipe toCreate)
        {
            toCreate.Id = Guid.NewGuid().ToString();
        }

        public Recipe Update(Recipe toUpdate)
        {
            var item = _dbContext.Recipes.Find(toUpdate.Id);
            if(item == null)
            {
                var response = Create(toUpdate);
                return response;
            }
            else
            {
                UpdateData(toUpdate, item);
                _dbContext.SaveChanges();

                var response = Map(item);
                return response;
            }

        }

        private static Recipe Map(entityframework.Models.Recipe toMap)
        {
            return new Recipe
            {
                Id = toMap.Id,
                Name = toMap.Name,
                Servings = toMap.Servings,
                Rating = toMap.Rating,
                Ingredients = toMap.Ingredients,
                Directions = toMap.Directions,
                Source = toMap.Source,
                Category = toMap.Category
            };
        }

        private static entityframework.Models.Recipe Map(Recipe toMap)
        {
            return new entityframework.Models.Recipe
            {
                Id = toMap.Id,
                Name = toMap.Name,
                Servings = toMap.Servings,
                Rating = toMap.Rating,
                Ingredients = toMap.Ingredients,
                Directions = toMap.Directions,
                Source = toMap.Source,
                Category = toMap.Category
            };
        }

        private static void UpdateData(Recipe from, entityframework.Models.Recipe to)
        {

            to.Name = from.Name;
            to.Servings = from.Servings;
            to.Rating = from.Rating;
            to.Ingredients = from.Ingredients;
            to.Directions = from.Directions;
            to.Source = from.Source;
            to.Category = from.Category;

        }
    }
}
