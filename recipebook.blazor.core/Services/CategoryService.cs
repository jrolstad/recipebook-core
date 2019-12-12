﻿using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using recipebook.blazorclient.Application.Models;

namespace recipebook.blazorclient.Application.Services
{
    public interface ICategoryService
    {
        Task<ICollection<Category>> Get();
    }

    public class CategoryService : ICategoryService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfigurationService _configurationService;

        public CategoryService(HttpClient httpClient, IConfigurationService configurationService)
        {
            _httpClient = httpClient;
            _configurationService = configurationService;
        }

        public async Task<ICollection<Category>> Get()
        {
            var uri = $"{_configurationService.CategoryApiUrl()}";
            var data = await _httpClient.GetJsonAsync<ICollection<Category>>(uri);

            return data;
        }
    }

    public class CachedCategoryService:ICategoryService
    {
        private readonly CategoryService _categoryService;

        private ICollection<Category> _cachedData = null;

        public CachedCategoryService(CategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        public async Task<ICollection<Category>> Get()
        {
            return _cachedData ?? (_cachedData = await _categoryService.Get());
        }
    }

    
}