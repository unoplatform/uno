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
	public sealed partial class RevealBrush_Fallback : UserControl
	{
		public RevealBrush_Fallback()
		{
			this.InitializeComponent();
		}

		public void MakeItGreen()
		{
			OrangeCrush.Color = Colors.ForestGreen;
			OrangeCrush.FallbackColor = Colors.ForestGreen;
			StatusTextBlock.Text = "Color changed";
		}
	}
}
