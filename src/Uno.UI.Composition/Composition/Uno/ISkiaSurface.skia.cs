#nullable enable

using SkiaSharp;

namespace Uno.UI.Composition
{
	internal interface ISkiaSurface
	{
		internal SKSurface? Surface { get; }
		internal void UpdateSurface(bool recreateSurface = false);
		internal void UpdateSurface(in PaintingSession session);
	}
}
