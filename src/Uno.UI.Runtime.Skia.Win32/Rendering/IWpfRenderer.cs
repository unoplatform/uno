using System;
using System.Windows.Media;
using Uno.UI.Rendering;

namespace Uno.UI.Runtime.Skia.Win32.Rendering;

internal interface IWpfRenderer : IRenderer, IDisposable
{
	bool TryInitialize();

	void Render(DrawingContext drawingContext);
}
