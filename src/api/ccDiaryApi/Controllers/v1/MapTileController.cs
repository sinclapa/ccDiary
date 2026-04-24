// <copyright file="MapTileController.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Controllers.v1
{
    using Asp.Versioning;
    using ccDiaryApi.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Net.Http.Headers;

    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]/[action]")]
    [ApiVersion("1.0")]
    [AllowAnonymous]
    public class MapTileController : ControllerBase
    {
        private readonly IMapTileService _mapTileService;

        public MapTileController(IMapTileService mapTileService)
        {
            _mapTileService = mapTileService;
        }

        [HttpGet("{source}/{z:int}/{x:int}/{y:int}")]
        public async Task<IActionResult> Tile(string source, int z, int x, int y)
        {
            var result = await _mapTileService.GetTileAsync(source, z, x, y);
            if (result == null)
            {
                SetNoCache();
                return NotFound();
            }

            SetCacheSeconds(86400);
            return File(result.Value.Data, result.Value.ContentType);
        }

        [HttpGet]
        public async Task<IActionResult> Geocode([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                SetNoCache();
                return BadRequest();
            }

            var result = await _mapTileService.GeocodeAsync(q);
            if (result == null)
            {
                SetNoCache();
                return NotFound();
            }

            SetCacheSeconds(604800);
            return Ok(new { lat = result.Value.Lat, lon = result.Value.Lon });
        }

        [HttpGet]
        public async Task<IActionResult> Route(
            [FromQuery] double fromLat,
            [FromQuery] double fromLon,
            [FromQuery] double toLat,
            [FromQuery] double toLon,
            [FromQuery] string profile)
        {
            var result = await _mapTileService.GetRouteAsync(fromLat, fromLon, toLat, toLon, profile);
            if (result == null)
            {
                SetNoCache();
                return NotFound();
            }

            SetCacheSeconds(86400);
            return Ok(result);
        }

        private void SetCacheSeconds(int seconds) =>
            Response.Headers[HeaderNames.CacheControl] = $"public, max-age={seconds}";

        private void SetNoCache() =>
            Response.Headers[HeaderNames.CacheControl] = "no-store";
    }
}
