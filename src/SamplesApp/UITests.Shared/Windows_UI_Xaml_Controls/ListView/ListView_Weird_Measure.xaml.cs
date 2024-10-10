using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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

namespace UITests.Shared.Windows_UI_Xaml_Controls.ListView
{
	[SampleControlInfo(description: "ListView gets measured with an anomalously-small width during arrange, which it must handle with grace and panache")]
	public sealed partial class ListView_Weird_Measure : UserControl
	{
		public ListView_Weird_Measure()
		{
			this.InitializeComponent();
		}

		private async void OnMakingProblems()
		{
			TargetListView.ItemsSource = null;
			TargetListView.ItemsSource = "ABC".ToArray();

			await Task.Delay(50);
			StatusTextBlock.Text = "Finished";
			HeightTextBlock.Text = TargetListView.ActualHeight.ToString();
		}
	}
}
