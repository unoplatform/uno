using System;

namespace Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// The drawing backend a Skia-tier target renders through. Orthogonal to <see cref="RuntimeTestPlatforms"/>: the
/// same platform (e.g. SkiaWin32) runs under either backend, chosen at startup, so a test that depends on one
/// conditions on this instead of on the platform.
/// </summary>
[Flags]
public enum RuntimeTestBackends
{
	/// <summary>No drawing backend — the native targets, which do not render through the drawing seam.</summary>
	None = 0,

	Skia = 1 << 0,
	WebGpu = 1 << 1,

	All = Skia | WebGpu,
}
