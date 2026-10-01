// <copyright file="RequireVisibleDiaryAttribute.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Authorization
{
    using ccDiaryApi.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;

    /// <summary>
    /// Answers 404 when the action's <c>diaryId</c> names a hidden diary the caller may not see.
    /// </summary>
    /// <remarks>
    /// Put it on every read action keyed by a diary id, so a hidden diary's entries, dates and
    /// search results are as unreachable as the diary itself. 404 rather than 403, so a caller
    /// cannot use the endpoint to confirm that a hidden diary exists.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RequireVisibleDiaryAttribute : TypeFilterAttribute
    {
        /// <summary>Initializes a new instance of the <see cref="RequireVisibleDiaryAttribute"/> class.</summary>
        public RequireVisibleDiaryAttribute()
            : base(typeof(RequireVisibleDiaryFilter))
        {
        }

        /// <summary>The filter the attribute applies.</summary>
        internal sealed class RequireVisibleDiaryFilter : IAsyncActionFilter
        {
            /// <summary>The action argument holding the diary id.</summary>
            internal const string DiaryIdArgument = "diaryId";

            private readonly IDiaryService _diaryService;
            private readonly IDiaryVisibility _visibility;

            public RequireVisibleDiaryFilter(IDiaryService diaryService, IDiaryVisibility visibility)
            {
                _diaryService = diaryService;
                _visibility = visibility;
            }

            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                // An action this is put on without a diary id is a mistake; failing loudly beats
                // silently serving whatever it returns.
                if (!context.ActionArguments.TryGetValue(DiaryIdArgument, out var value) || value is not Guid diaryId)
                {
                    throw new InvalidOperationException(
                        $"{nameof(RequireVisibleDiaryAttribute)} needs a Guid '{DiaryIdArgument}' argument on {context.ActionDescriptor.DisplayName}.");
                }

                var diary = await _diaryService.GetDiaryAsync(diaryId);
                if (!await _visibility.CanViewAsync(context.HttpContext.User, diary))
                {
                    context.Result = new NotFoundResult();
                    return;
                }

                await next();
            }
        }
    }
}
