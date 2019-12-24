using System;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Foundation.Metadata;
namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class DragDeltaEventArgs : RoutedEventArgs
	{
		public DragDeltaEventArgs(double horizontalChange, double verticalChange)
		{
			HorizontalChange = horizontalChange;
			VerticalChange = verticalChange;
		}

		internal DragDeltaEventArgs(object originalSource, double horizontalChange, double verticalChange)
			: base(originalSource)
		{
			HorizontalChange = horizontalChange;
			VerticalChange = verticalChange;
		}

		public double HorizontalChange { get; }

		public double VerticalChange { get; }

	}
}
