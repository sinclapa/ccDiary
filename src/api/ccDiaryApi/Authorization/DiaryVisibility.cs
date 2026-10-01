// <copyright file="DiaryVisibility.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Authorization
{
    using System.Security.Claims;
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Extensions;
    using Microsoft.AspNetCore.Authorization;

    /// <summary>
    /// A hidden diary is visible to an admin, and to its owner while they hold the contributor
    /// role. Every other caller - the public, a signed-in reader, another contributor - cannot see it.
    /// </summary>
    /// <remarks>
    /// Evaluating the registered policies, rather than testing role names here, keeps one
    /// definition of each role. Read endpoints are anonymous, but the UI sends its token on every
    /// call, so a signed-in owner is still recognised on them. An owner whose contributor role is
    /// withdrawn loses sight of their hidden diaries along with the right to edit them.
    /// </remarks>
    public class DiaryVisibility : IDiaryVisibility
    {
        /// <summary>The policy whose holders may see every hidden diary.</summary>
        public const string AdminPolicy = "DiaryAdmin";

        /// <summary>The policy an owner must hold to see their own hidden diaries.</summary>
        public const string ContributorPolicy = "DiaryContributor";

        private static readonly Func<DiaryDto, bool> All = _ => true;
        private static readonly Func<DiaryDto, bool> None = _ => false;

        private readonly IAuthorizationService _authorization;

        /// <summary>Initializes a new instance of the <see cref="DiaryVisibility"/> class.</summary>
        /// <param name="authorization">Evaluates the admin and contributor policies.</param>
        public DiaryVisibility(IAuthorizationService authorization)
        {
            _authorization = authorization;
        }

        /// <inheritdoc/>
        public async Task<Func<DiaryDto, bool>> CanSeeHiddenAsync(ClaimsPrincipal user)
        {
            if ((await _authorization.AuthorizeAsync(user, AdminPolicy)).Succeeded)
            {
                return All;
            }

            var oid = user.GetOid();
            if (string.IsNullOrEmpty(oid) || !(await _authorization.AuthorizeAsync(user, ContributorPolicy)).Succeeded)
            {
                return None;
            }

            return diary => string.Equals(diary.OwnerId, oid, StringComparison.Ordinal);
        }

        /// <inheritdoc/>
        public async Task<bool> CanViewAsync(ClaimsPrincipal user, DiaryDto? diary)
        {
            if (diary == null || !diary.IsHidden)
            {
                return true;
            }

            return (await CanSeeHiddenAsync(user))(diary);
        }
    }
}
