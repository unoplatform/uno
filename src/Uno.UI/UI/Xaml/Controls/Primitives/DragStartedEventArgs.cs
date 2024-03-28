using System;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Foundation.Metadata;
namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class DragStartedEventArgs : RoutedEventArgs
	{
		public DragStartedEventArgs(double horizontalOffset, double verticalOffset)
		{
			HorizontalOffset = horizontalOffset;
			VerticalOffset = verticalOffset;
		}

		internal DragStartedEventArgs(object originalSource, double horizontalOffset, double verticalOffset)
			: base(originalSource)
		{
			HorizontalOffset = horizontalOffset;
			VerticalOffset = verticalOffset;
		}


		public double HorizontalOffset { get; }

		public double VerticalOffset { get; }

	}
}
