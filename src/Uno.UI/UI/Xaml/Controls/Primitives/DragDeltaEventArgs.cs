using System;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Foundation.Metadata;
namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class DragDeltaEventArgs : RoutedEventArgs
	{
		public double HorizontalChange
		{
			get;
		}

		public double VerticalChange
		{
			get;
		}

		public DragDeltaEventArgs(double horizontalChange, double verticalChange)
		{
			HorizontalChange = horizontalChange;
			VerticalChange = verticalChange;
		}
	}
}
