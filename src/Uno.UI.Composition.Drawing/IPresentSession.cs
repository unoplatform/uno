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
	/// True when what the previous frame composed is still in the surface this session draws into, so only the
	/// damaged region has to be repainted. A backend that composes through a retained offscreen reports true even
	/// on a host whose swapchain discards its contents, because the offscreen carries the pixels forward instead.
	/// </summary>
	bool PreservesContents => false;
}
