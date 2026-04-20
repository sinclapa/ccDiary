// <copyright file="IMapTileService.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Services
{
    public interface IMapTileService
    {
        Task<(byte[] Data, string ContentType)?> GetTileAsync(string source, int z, int x, int y);

        Task<(double Lat, double Lon)?> GeocodeAsync(string query);

        Task<IReadOnlyList<double[]>?> GetRouteAsync(double fromLat, double fromLon, double toLat, double toLon, string profile);
    }
}
