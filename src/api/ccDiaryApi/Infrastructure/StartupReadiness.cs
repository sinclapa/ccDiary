// <copyright file="StartupReadiness.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Infrastructure
{
    /// <summary>
    /// Records whether this process has finished preparing storage, for the startup and readiness probes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The probes used to poll the Steeltoe health endpoint. That cost two storage round trips
    /// and a first-hit JIT of the actuator pipeline on the path a cold-starting user is waiting
    /// on, and it routinely missed its first attempt. It also proved bootstrap only indirectly,
    /// through the app info row, which outlives the process that wrote it.
    /// </para>
    /// <para>
    /// A flag set by <see cref="StorageBootstrapper"/> answers the question the probes are
    /// actually asking, which is whether this process is ready, without any I/O. Today the host
    /// starts hosted services before Kestrel, so by the time anything can reach the endpoint the
    /// flag is already set and the 503 branch is never taken. The flag is there so readiness
    /// doesn't quietly depend on that ordering. Starting hosted services concurrently, or moving
    /// bootstrap into a background service, would change it.
    /// </para>
    /// </remarks>
    public sealed class StartupReadiness
    {
        private int _ready;

        /// <summary>Gets a value indicating whether storage bootstrap has completed.</summary>
        public bool IsReady => Volatile.Read(ref _ready) == 1;

        /// <summary>Marks bootstrap as complete. Calling it again has no further effect.</summary>
        public void MarkReady() => Interlocked.Exchange(ref _ready, 1);
    }
}
