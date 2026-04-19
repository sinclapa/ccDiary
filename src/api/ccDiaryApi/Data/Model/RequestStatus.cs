// <copyright file="RequestStatus.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Data.Model
{
    public enum RequestStatus
    {
        /// <summary>Access request is awaiting admin review.</summary>
        Pending = 0,

        /// <summary>Access request has been approved.</summary>
        Approved = 1,

        /// <summary>Access request has been declined.</summary>
        Declined = 2,
    }
}
