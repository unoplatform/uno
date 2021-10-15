namespace Windows.UI.Xaml.Controls
{
	public sealed partial class ScrollViewerViewChangingEventArgs
	{
		internal ScrollViewerViewChangingEventArgs(bool isInertial, ScrollViewerView finalView, ScrollViewerView nextView)
		{
			IsInertial = isInertial;
			FinalView = finalView;
			NextView = nextView;
		}

		public ScrollViewerView FinalView { get; }

		public bool IsInertial { get; }

		public ScrollViewerView NextView { get; }
	}
}
