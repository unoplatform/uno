using System;
using System.Windows.Media;
using SkiaSharp;

namespace Uno.UI.Runtime.Skia.Wpf.Rendering;

internal interface IWpfRenderer : IDisposable
{
	SKColor BackgroundColor { get; set; }

	void Render(DrawingContext drawingContext);
	bool Initialize();

	SKSize CanvasSize { get; }
}
