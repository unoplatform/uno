#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A scoped composition onto a present target, returned by <see cref="IDrawingFactory.BeginPresent"/>. The
/// render cycle replays the recorded frame into it (<see cref="IRenderRecord.Replay"/>) and draws any overlay
/// content as peer draws, then disposes it to finalize (present) the result onto the surface.
/// </summary>
public interface IPresentSession : IDrawingSession, IDisposable
{
	/// <summary>
	/// True when the surface this session composes into keeps the previous frame's pixels (a persistent host
	/// framebuffer, or a backend-retained offscreen that is blitted to the swapchain on present), so the compositor
	/// may repaint only the damaged region. False (the default) for a fresh/undefined surface each frame, which
	/// requires a full repaint.
	/// </summary>
	bool PreservesContents => false;
}
