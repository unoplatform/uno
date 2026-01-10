using System;
using System.Drawing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Toolkit
{
	[Sample("Toolkit")]
	public sealed partial class ElevatedViewTests : Page
	{
		public ElevatedViewTests()
		{
			this.InitializeComponent();
		}

		private void SetElevationClick(object sender, RoutedEventArgs e)
		{
			if (sender is Button btn && btn.Tag != null)
			{
				elevation1.Value = Convert.ToDouble(btn.Tag);
			}
		}

		private void SetColor(object sender, RoutedEventArgs e)
		{
			if (sender is Button btn && btn.Tag != null)
			{
#if !WINAPPSDK
				var colorBrush = SolidColorBrushHelper.Parse(btn.Tag as string);
				colorBrush.Opacity = 0.25;
				elevation1.Tag = colorBrush.ColorWithOpacity;
#endif
			}
		}
	}
}
