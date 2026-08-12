#nullable enable

using Microsoft.UI.Xaml;

namespace Uno.UI.Runtime.Skia;

/// <summary>
/// Information about a window being created, passed to the
/// <see cref="HeadlessHostBuilder.ConfigureWindow"/> configurator so callers can return
/// per-window <see cref="HeadlessWindowOptions"/>.
/// </summary>
public readonly struct HeadlessWindowContext
{
	internal HeadlessWindowContext(int index, Window window)
	{
		Index = index;
		Window = window;
	}

	/// <summary>Zero-based creation order of the window (the first window is <c>0</c>).</summary>
	public int Index { get; }

	/// <summary>The <see cref="Microsoft.UI.Xaml.Window"/> being created.</summary>
	public Window Window { get; }
}
