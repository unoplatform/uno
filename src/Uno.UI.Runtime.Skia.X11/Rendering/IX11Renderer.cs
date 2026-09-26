#nullable enable

using System;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// Backend-agnostic host-facing contract for an X11 renderer: the host only invalidates (<see cref="Render"/>),
/// sets the window background, and disposes.
/// </summary>
internal interface IX11Renderer : IDisposable
{

	void Render();
}

/// <summary>
/// Implemented by contexts owning a GL/EGL context. The backend factory's GPU resources (GRContext, cached
/// surfaces) can only be released while that context is current, so teardown makes it current first.
/// </summary>
internal interface IX11GpuTeardownContext
{
	void MakeCurrentForTeardown();
}
