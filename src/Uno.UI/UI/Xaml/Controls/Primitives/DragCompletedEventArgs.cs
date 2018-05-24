using System;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Foundation.Metadata;
namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class DragCompletedEventArgs : RoutedEventArgs
	{
		public bool Canceled
		{
			get;
		}

		public double HorizontalChange
		{
			get;
		}

		public double VerticalChange
		{
			get;
		}

		public DragCompletedEventArgs(double horizontalChange, double verticalChange, bool canceled)
		{
			Canceled = canceled;
			HorizontalChange = horizontalChange;
			VerticalChange = verticalChange;
		}
	}
}
