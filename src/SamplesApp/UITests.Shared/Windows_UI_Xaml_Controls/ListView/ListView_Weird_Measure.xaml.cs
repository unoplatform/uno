using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.ListView
{
	[Sample(description: "ListView gets measured with an anomalously-small width during arrange, which it must handle with grace and panache")]
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
