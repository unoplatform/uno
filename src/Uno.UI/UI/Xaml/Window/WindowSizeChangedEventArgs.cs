using System;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Microsoft.UI.Xaml;

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
