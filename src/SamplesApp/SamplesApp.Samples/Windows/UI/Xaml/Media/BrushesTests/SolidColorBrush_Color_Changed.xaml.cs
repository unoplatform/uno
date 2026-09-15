using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Windows_UI_Xaml_Media.BrushesTests
{
	[Sample("Brushes")]
	public sealed partial class SolidColorBrush_Color_Changed : UserControl
	{
		public SolidColorBrush_Color_Changed()
		{
			this.InitializeComponent();
		}

		private void ChangeBorderColor(object sender, object args)
		{
			(ToggleableBorder.BorderBrush as SolidColorBrush).Color = Colors.Blue;
			(ToggleableGrid.BorderBrush as SolidColorBrush).Color = Colors.Blue;
			(ToggleableEllipse.Stroke as SolidColorBrush).Color = Colors.Blue;
			(ToggleableFillEllipse.Fill as SolidColorBrush).Color = Colors.Blue;
			StatusTextBlock.Text = "Set";
		}
	}
}
