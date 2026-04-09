// <copyright file="JourneyMode.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Data.Model
{
    /// <summary>Specifies how a journey route is displayed on the map.</summary>
    public enum JourneyMode
    {
        /// <summary>Direct straight line between two points.</summary>
        CrowFlies = 0,

        /// <summary>Walking route.</summary>
        Walking = 1,

        /// <summary>Car driving route.</summary>
        Car = 2,

        /// <summary>Train route.</summary>
        Train = 3,

        /// <summary>Boat route.</summary>
        Boat = 4,
    }
}
