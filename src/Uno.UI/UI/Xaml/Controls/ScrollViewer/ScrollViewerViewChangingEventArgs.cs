#nullable enable

namespace Microsoft.UI.Xaml.Controls;

public sealed partial class ScrollViewerViewChangingEventArgs
{
	internal ScrollViewerViewChangingEventArgs(ScrollViewerView nextView, ScrollViewerView finalView, bool isInertial)
	{
		NextView = nextView;
		FinalView = finalView;
		IsInertial = isInertial;
	}

	public ScrollViewerView NextView { get; }

	public ScrollViewerView FinalView { get; }

	public bool IsInertial { get; }
}
