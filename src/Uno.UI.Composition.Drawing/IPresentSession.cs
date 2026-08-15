#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A scoped composition onto a present target, returned by <see cref="IDrawingFactory.BeginPresent"/>. The
/// render cycle replays the recorded frame into it (<see cref="IRenderData.Replay"/>) and draws any overlay
/// content as peer draws, then disposes it to finalize (present) the result onto the surface.
/// </summary>
public interface IPresentSession : IDrawingSession, IDisposable
{
}
