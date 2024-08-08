#if !__SKIA__
using System;

namespace Microsoft.UI.Xaml.Controls;

public abstract class SKCanvasElement : FrameworkElement
{
	protected SKCanvasElement()
	{
		throw new PlatformNotSupportedException($"${nameof(SKCanvasElement)} is only available on skia targets.");
	}
}
#endif
