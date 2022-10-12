using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public sealed partial class Page_UpdateBackground : UserControl
	{
		public Page_UpdateBackground()
		{
			this.InitializeComponent();
		}

		public void AdvanceTestButton_Click()
		{
			(TargetPage.Background as SolidColorBrush).Color = Colors.Green;
			StatusTextBlock.Text = "Color changed";
		}
	}
}
