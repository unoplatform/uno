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
	[SampleControlInfo("LayoutTestControl", nameof(HorizontalStackPanelTest))]
	public sealed partial class HorizontalStackPanelTest : UserControl
	{
		public HorizontalStackPanelTest()
		{
			this.InitializeComponent();
		}

		private double[] _heights = new double[] { 20, 25, 40, 50 };
		private int _currentHeight = 0;

		private void ButtonClick(object sender, RoutedEventArgs e)
		{
			// change the width of the rectangle
			this.MyRectangle.Height = _heights[_currentHeight++ % (_heights.Length - 1)];
		}
	}
}

