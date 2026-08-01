#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A scoped composition onto a present target, returned by <see cref="IRenderer.BeginPresent"/>. The
/// render cycle composes the recorded frame (via <see cref="IRetainedRenderingSession.Replay"/>) and any
/// overlay content into it as peer draws, then disposes it to finalize (present) the result onto the surface.
/// </summary>
public interface IPresentSession : IDrawingSession, IRetainedRenderingSession, IDisposable
{
}
