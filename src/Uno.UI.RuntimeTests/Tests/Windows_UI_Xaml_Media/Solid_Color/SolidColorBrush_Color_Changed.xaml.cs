using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples;
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

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media
{
		public sealed partial class SolidColorBrush_ColorChanged : UserControl
	{
		public SolidColorBrush_ColorChanged()
		{
			this.InitializeComponent();
		}

		public void PaintItBlue()
		{
			(ToggleableBorder.BorderBrush as SolidColorBrush).Color = Colors.Blue;
			(ToggleableGrid.BorderBrush as SolidColorBrush).Color = Colors.Blue;
			(ToggleableEllipse.Stroke as SolidColorBrush).Color = Colors.Blue;
			(ToggleableFillEllipse.Fill as SolidColorBrush).Color = Colors.Blue;
			StatusTextBlock.Text = "Set";
		}
	}
}
