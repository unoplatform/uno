#nullable enable

using SkiaSharp;

namespace Uno.UI.Composition
{
	internal interface IOnlineBrush
	{
		internal bool IsOnline { get; }
		internal void Draw(in PaintingSession session, SKRect bounds = new());
	}
}
