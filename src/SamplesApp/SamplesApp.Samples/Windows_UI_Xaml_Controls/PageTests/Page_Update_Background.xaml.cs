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

namespace UITests.Windows_UI_Xaml_Controls.PageTests
{
	[Sample("Page")]
	public sealed partial class Page_Update_Background : UserControl
	{
		public Page_Update_Background()
		{
			this.InitializeComponent();
		}

		private void AdvanceTestButton_Click(object sender, RoutedEventArgs e)
		{
			(TargetPage.Background as SolidColorBrush).Color = Colors.Green;
			StatusTextBlock.Text = "Color changed";
		}
	}
}
