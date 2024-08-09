using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static System.Math;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Windows_UI_Input.PointersTests
{
	[Sample("Pointers")]
	public sealed partial class ScrollViewer_PointerMoved : UserControl
	{
		public ScrollViewer_PointerMoved()
		{
			this.InitializeComponent();
		}

		private Point? _downPosition;

		private void HostGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			_downPosition = e.GetCurrentPoint(this).Position;
		}

		private void HostGrid_PointerMoved(object sender, PointerRoutedEventArgs e)
		{
			if (_downPosition is Point downPosition)
			{
				var currentPosition = e.GetCurrentPoint(this).Position;
				var yOffset = currentPosition.Y - downPosition.Y;

				var lerp = Min(255, Abs((int)yOffset * 3));

				StatusTextBlock.Text = lerp == 255 ?
					"DragFull" :
					"DragPartial";

				var newColor = yOffset > 0 ?
					Color.FromArgb(
						255,
						(byte)lerp,
						0,
						255
					) :
					Color.FromArgb(
						255,
						0,
						(byte)lerp,
						255
					);
				(HostGrid.Background as SolidColorBrush).Color = newColor;
			}
		}

		private void HostGrid_PointerReleased(object sender, PointerRoutedEventArgs e)
		{
			_downPosition = null;
		}

		private void HostGrid_PointerCanceled(object sender, PointerRoutedEventArgs e)
		{
			_downPosition = null;
		}

		private void HostGrid_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			_downPosition = null;
		}

		private void ResetColorButton_Click(object sender, RoutedEventArgs e)
		{
			(HostGrid.Background as SolidColorBrush).Color = Colors.Blue;
			_downPosition = null;
			StatusTextBlock.Text = "Not dragged";
		}
	}
}
