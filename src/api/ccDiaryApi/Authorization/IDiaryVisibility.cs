// <copyright file="IDiaryVisibility.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Authorization
{
    using System.Security.Claims;
    using ccDiaryApi.Data.Model;

    /// <summary>
    /// Decides who may see a diary marked <see cref="DiaryDto.IsHidden"/>: an admin, or the
    /// contributor who owns it.
    /// </summary>
    public interface IDiaryVisibility
    {
        /// <summary>
        /// Works out, once per request, which hidden diaries the caller may see.
        /// </summary>
        /// <param name="user">The caller.</param>
        /// <returns>
        /// A test for a hidden diary: true when the caller may see it. Evaluating the policies
        /// once and returning a plain predicate keeps the diary list from awaiting per diary.
        /// </returns>
        Task<Func<DiaryDto, bool>> CanSeeHiddenAsync(ClaimsPrincipal user);

        /// <summary>
        /// Whether the caller may see this diary.
        /// </summary>
        /// <param name="user">The caller.</param>
        /// <param name="diary">The diary, or null when it does not exist.</param>
        /// <returns>
        /// False only for a hidden diary the caller may not see. A missing diary is left to the
        /// caller's existing not-found handling.
        /// </returns>
        Task<bool> CanViewAsync(ClaimsPrincipal user, DiaryDto? diary);
    }
}
