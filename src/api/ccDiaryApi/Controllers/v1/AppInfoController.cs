// <copyright file="AppInfoController.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Controllers.v1
{
    using Asp.Versioning;
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]/[action]")]
    [ApiVersion("1.0")]
    [AllowAnonymous]
    public class AppInfoController : ControllerBase
    {
        private readonly IAppInfoService _appInfoService;

        public AppInfoController(IAppInfoService appInfoService)
        {
            _appInfoService = appInfoService;
        }

        [HttpGet]
        public async Task<ActionResult<AppInfoDTO>> Get()
        {
            var appInfo = await _appInfoService.GetAppInfoAsync();
            if (appInfo == null)
            {
                return NotFound();
            }

            return Ok(appInfo);
        }
    }
}
