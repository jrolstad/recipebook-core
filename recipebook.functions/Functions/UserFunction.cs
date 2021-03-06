﻿using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using recipebook.core.Managers;

namespace recipebook.functions.Functions
{
    public class UserFunction
    {
        private readonly UserManager _manager;

        public UserFunction(UserManager manager)
        {
            _manager = manager;
        }

        [FunctionName("api-user")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user")] HttpRequest req,
            ILogger log,
            ClaimsPrincipal user)
        {
            var result = _manager.Get(user);

            return new OkObjectResult(result);
        }
    }
}
