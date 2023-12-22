#if HAS_UNO_WINUI
using System;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Microsoft/* UWP don't rename */.UI.Xaml;

public sealed partial class WindowSizeChangedEventArgs
{
	public WindowSizeChangedEventArgs(Size newSize)
	{
		Size = newSize;
	}

	public bool Handled
	{
		get;
		set;
	}

	public Size Size
	{
		get;
	}
}
#endif
