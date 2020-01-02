using System;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Foundation.Metadata;
namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class DragCompletedEventArgs : RoutedEventArgs
	{
		public DragCompletedEventArgs(double horizontalChange, double verticalChange, bool canceled)
		{
			Canceled = canceled;
			HorizontalChange = horizontalChange;
			VerticalChange = verticalChange;
		}

		public DragCompletedEventArgs(object originalSource, double horizontalChange, double verticalChange, bool canceled)
			: base(originalSource)
		{
			Canceled = canceled;
			HorizontalChange = horizontalChange;
			VerticalChange = verticalChange;
		}

		public bool Canceled { get; }

		public double HorizontalChange { get; }

		public double VerticalChange { get; }
	}
}
