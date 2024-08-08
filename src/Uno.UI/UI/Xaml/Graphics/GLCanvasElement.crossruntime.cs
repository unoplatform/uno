#if !__SKIA__
using System;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

public abstract partial class GLCanvasElement : FrameworkElement
{
	protected GLCanvasElement(Size resolution)
	{
		throw new PlatformNotSupportedException($"${nameof(GLCanvasElement)} is only available on skia targets.");
	}
}
#endif
