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

namespace UITests.Shared.Windows_UI_Xaml_Controls.FlipViewTests
{
	[SampleControlInfo("FlipView", nameof(FlipView_SelectedIndex_Change), description: "FlipView whose SelectedIndex can be changed by small or large increments.")]
	public sealed partial class FlipView_SelectedIndex_Change : UserControl
	{
		public FlipView_SelectedIndex_Change()
		{
			this.InitializeComponent();

			TargetFlipView.ItemsSource = Enumerable.Range(0, 100).ToArray();
		}

		private void Button_Click_17(object sender, RoutedEventArgs e)
		{
			TargetFlipView.SelectedIndex = TargetFlipView.SelectedIndex + 1;
		}

		private void Button_Click_18(object sender, RoutedEventArgs e)
		{
			TargetFlipView.SelectedIndex = TargetFlipView.SelectedIndex + 2;
		}

		private void Button_Click_19(object sender, RoutedEventArgs e)
		{
			TargetFlipView.SelectedIndex = TargetFlipView.SelectedIndex - 2;
		}
	}
}
