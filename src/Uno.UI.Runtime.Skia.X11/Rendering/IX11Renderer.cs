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
