#nullable enable

namespace Uno.UI.Runtime.Skia;

/// <summary>
/// Per-window configuration returned by <see cref="HeadlessHostBuilder.ConfigureWindow"/>. Carries the
/// rasterization scale (which has no public WinUI equivalent). The window size is not set here — it
/// defaults to <see cref="HeadlessHostBuilder.WithSize"/> and can be changed at runtime via the standard
/// <c>AppWindow.Resize</c>.
/// </summary>
public sealed class HeadlessWindowOptions
{
	/// <summary>
	/// The rasterization scale (a.k.a. <c>RawPixelsPerViewPixel</c>). Logical bounds are
	/// <c>size / scale</c>. Defaults to <c>1.0</c>.
	/// </summary>
	public float Scale { get; init; } = 1f;
}
