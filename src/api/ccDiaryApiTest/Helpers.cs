// <copyright file="Helpers.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest
{
    using Microsoft.AspNetCore.Mvc;

    public static class Helpers
    {
        public static T? GetObjectResult<T>(this ActionResult<T> result)
        {
            if (result.Result != null)
            {
                return (T?)((ObjectResult)result.Result).Value;
            }

            return result.Value;
        }
    }
}
