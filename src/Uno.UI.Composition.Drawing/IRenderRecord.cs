#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Opaque, backend-defined retained state produced by a recording (<see cref="ICommandRecorder.Finish"/>) and
/// replayed with <see cref="Replay"/>. Not necessarily a display list (a backend may store an <c>SKPicture</c>,
/// a texture, a command buffer, …). Composition holds it opaquely and never inspects it.
/// </summary>
public interface IRenderRecord : IDisposable
{
	/// <summary>
	/// Replays this recorded content into <paramref name="into"/>. Backend-bound: a native impl downcasts
	/// <paramref name="into"/> to its own session type and only works with a session from the backend that recorded
	/// it (single-registered-backend invariant). Only the command-list fallback replays onto any <see cref="IDrawingSession"/>.
	/// </summary>
	void Replay(IDrawingSession into);
}
