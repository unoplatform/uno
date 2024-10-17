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

		internal DragDeltaEventArgs(object originalSource, double horizontalChange, double verticalChange, double totalHorizontalChange, double totalVerticalChange)
			: base(originalSource)
		{
			HorizontalChange = horizontalChange;
			VerticalChange = verticalChange;
			TotalHorizontalChange = totalHorizontalChange;
			TotalVerticalChange = totalVerticalChange;
		}

		public double HorizontalChange { get; }

		public double VerticalChange { get; }

		/// <summary>
		/// Internal helper property that contains the total horizontal change since the DragStarted event
		/// </summary>
		/// <remarks>Be aware that this property will be filled only when the args are built internally by Uno</remarks>
		internal double TotalHorizontalChange { get; }

		/// <summary>
		/// Internal helper property that contains the total vertical change since the DragStarted event
		/// </summary>
		/// <remarks>Be aware that this property will be filled only when the args are built internally by Uno</remarks>
		internal double TotalVerticalChange { get; }

	}
}
