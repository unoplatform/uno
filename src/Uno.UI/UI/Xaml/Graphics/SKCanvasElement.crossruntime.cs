#if !__SKIA__
using System;
using Windows.Foundation;
using SkiaSharp;

namespace Microsoft.UI.Xaml.Controls;

public abstract partial class SKCanvasElement : FrameworkElement
{
	protected SKCanvasElement()
	{
		throw new PlatformNotSupportedException($"${nameof(SKCanvasElement)} is only available on skia targets.");
	}

	protected abstract void RenderOverride(SKCanvas canvas, Size area);
}
#endif
