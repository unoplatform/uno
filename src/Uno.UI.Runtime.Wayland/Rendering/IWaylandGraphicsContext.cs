#nullable enable

using System;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Runtime.Wayland.Rendering;

/// <summary>
/// A swapchain presenting to a wl_surface. Before committing a frame it asks its owner to prepare the surface
/// state that must land in the same commit (buffer scale, viewport, configure acknowledgement), or to hold the
/// frame back, keeping the previous buffer on screen.
/// </summary>
internal interface IWaylandGraphicsContext : ISwapChain
{
	/// <summary>Whether the last <see cref="ISwapChain.Present"/> committed a buffer to the surface.</summary>
	bool Committed { get; }
}

/// <summary>
/// Implemented by contexts owning a GL/EGL context. The backend factory's GPU resources can only be released
/// while that context is current, so teardown makes it current first.
/// </summary>
internal interface IWaylandGpuTeardownContext
{
	/// <summary>Makes the context current to release GPU resources; false when it was lost.</summary>
	bool TryMakeCurrentForTeardown();
}

/// <summary>
/// Called with the pixel size of the frame about to be committed; false keeps the previous buffer on screen.
/// </summary>
internal delegate bool WaylandCommitGate(int width, int height, Func<bool> submit);
