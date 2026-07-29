#nullable enable

using System;
using Windows.UI;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// The host-facing contract for an X11 renderer, kept backend-agnostic: the host only invalidates
/// (<see cref="Render"/>), sets the window background, and disposes. The Skia-shaped <see cref="X11Renderer"/>
/// implements it, as does the neutral <see cref="X11SoftwareGraphicsRenderer"/> that drives the pluggable
/// graphics pipeline — so the host names no GPU-library type.
/// </summary>
internal interface IX11Renderer : IDisposable
{
	void SetBackgroundColor(Color color);

	void Render();
}
