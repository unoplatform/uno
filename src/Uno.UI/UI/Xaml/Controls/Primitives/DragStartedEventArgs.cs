using System;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Foundation.Metadata;
namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class DragStartedEventArgs : RoutedEventArgs
	{
		public double HorizontalOffset
		{
			get;
		}

		public double VerticalOffset
		{
			get;
		}

		public  DragStartedEventArgs(double horizontalOffset, double verticalOffset)
		{
			HorizontalOffset = horizontalOffset;
			VerticalOffset = verticalOffset;
		}
	}
}
