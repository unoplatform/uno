#nullable enable

using Windows.UI.Composition;
using SkiaSharp;

namespace Uno.UI.Composition
{
	internal interface ISkiaSurface
	{
		internal SKSurface? Surface { get; }
		internal void UpdateSurface(bool recreateSurface = false);
		internal void UpdateSurface(in Visual.PaintingSession session);
	}
}
