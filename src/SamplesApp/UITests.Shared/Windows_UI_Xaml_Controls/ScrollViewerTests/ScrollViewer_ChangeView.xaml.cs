using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.ScrollViewerTests
{
	[SampleControlInfo("ScrollViewer", "ScrollViewer_ChangeView")]
	public sealed partial class ScrollViewer_ChangeView : UserControl
	{
		public ScrollViewer_ChangeView()
		{
			this.InitializeComponent();

			VerticalOffsetTextBox.TextChanged += VerticalOffsetTextBox_TextChanged;
			ZoomFactorTextBox.TextChanged += ZoomFactorTextBox_TextChanged;
			ChangeViewScrollViewer.ViewChanged += ChangeViewScrollViewer_ViewChanged;
		}

		private void VerticalOffsetTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			double newOffset;
			if (double.TryParse(VerticalOffsetTextBox.Text, out newOffset))
			{
				ChangeViewScrollViewer.ChangeView(null, newOffset, null, (bool)DisableAnimationCheckBox.IsChecked);
			}
		}

		private void ZoomFactorTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			float newZoom;
			if (float.TryParse(ZoomFactorTextBox.Text, out newZoom))
			{
				ChangeViewScrollViewer.ChangeView(null, null, newZoom, (bool)DisableAnimationCheckBox.IsChecked);
			}
		}

		private void ChangeViewScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
		{
			var scrollView = sender as Windows.UI.Xaml.Controls.ScrollViewer;
			this.Log().Warn($"ChangeViewScrollViewer_ViewChanged: HorizontalOffset={scrollView.HorizontalOffset}, VerticalOffset={scrollView.VerticalOffset}, ZoomFactor={scrollView.ZoomFactor}");
		}
	}
}
