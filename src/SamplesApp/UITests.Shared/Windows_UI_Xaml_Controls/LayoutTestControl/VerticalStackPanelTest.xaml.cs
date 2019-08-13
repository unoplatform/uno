using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
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

namespace UITests.Shared.Windows_UI_Xaml_Controls.LayoutTestControl
{
	[SampleControlInfo("LayoutTestControl", nameof(VerticalStackPanelTest))]
	public sealed partial class VerticalStackPanelTest : UserControl
	{
		public VerticalStackPanelTest()
		{
			this.InitializeComponent();
		}

		private double[] _widths = new double[] { 20, 25, 40, 50 };
		private int _currentWidth = 0;

		private void ButtonClick(object sender, RoutedEventArgs e)
		{
			// change the width of the rectangle
			this.MyRectangle.Width = _widths[_currentWidth++ % (_widths.Length - 1)];
		}
	}
}
