using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ccDiaryApiTest
{
    public static class Helpers
    {
        public static T? GetObjectResult<T>(this ActionResult<T> result)
        {
            if (result.Result != null)
                return (T?)((ObjectResult)result.Result).Value;
            return result.Value;
        }
    }
}
