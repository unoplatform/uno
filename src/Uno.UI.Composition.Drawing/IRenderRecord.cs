#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Opaque, backend-defined retained state produced by a recording (<see cref="ICommandRecorder.Finish"/>) and
/// replayed with <see cref="Replay"/>. It is <em>not</em> necessarily a display list — the SkiaSharp backend
/// stores an <c>SKPicture</c>, another backend may store a texture, a command buffer, or any metadata it
/// maintains. Composition holds it opaquely and never inspects it.
/// </summary>
public interface IRenderRecord : IDisposable
{
	/// <summary>
	/// Replays this recorded content into <paramref name="into"/>. The data is <b>backend-bound</b>: a native
	/// impl downcasts <paramref name="into"/> to its own backend's session type and only works with a session
	/// produced by the backend that recorded it (guaranteed by the single-registered-backend invariant). The
	/// command-list fallback is the one impl whose replay is genuinely session-neutral (it re-issues the neutral
	/// verbs onto any <see cref="IDrawingSession"/>).
	/// </summary>
	void Replay(IDrawingSession into);
}
