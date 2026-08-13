#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A scoped composition onto a present target, returned by <see cref="IRenderer.BeginPresent"/>. The
/// render cycle replays the recorded frame (through <c>RetainedRenderingSession.For</c>, which uses the
/// backend's native replay when its present session implements <see cref="IRetainedRenderingSession"/> and a
/// command-list fallback otherwise) and draws any overlay content into it as peer draws, then disposes it to
/// finalize (present) the result onto the surface.
/// </summary>
/// <remarks>
/// <see cref="IRetainedRenderingSession"/> is not a required base: a backend opts into native replay by
/// implementing it, but composition can always retain regardless.
/// </remarks>
public interface IPresentSession : IDrawingSession, IDisposable
{
}
