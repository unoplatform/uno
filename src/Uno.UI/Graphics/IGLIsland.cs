#nullable enable

using Microsoft.UI.Xaml;

namespace Uno.Graphics;

/// <summary>
/// An element that renders into its own offscreen OpenGL framebuffer and composites the result.
/// Provided by Uno.WinUI.Graphics3DGL when the app references it; create one with
/// <c>ApiExtensibility.CreateInstance&lt;IGLIsland&gt;(renderer, out var island)</c>, where the owner is an
/// <see cref="IGLIslandRenderer"/>.
/// </summary>
public interface IGLIsland
{
	/// <summary>The element to add to the visual tree.</summary>
	FrameworkElement Element { get; }

	/// <summary>Requests a redraw.</summary>
	void Invalidate();
}
