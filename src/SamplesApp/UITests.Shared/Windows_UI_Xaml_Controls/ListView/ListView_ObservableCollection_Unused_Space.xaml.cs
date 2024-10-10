using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
	[SampleControlInfo(description: "ListView inside StackPanel that can have items added to its ItemsSource. Should remeasure correctly.")]
	public sealed partial class ListView_ObservableCollection_Unused_Space : UserControl
	{
		private MyViewModel _vm;

		public ListView_ObservableCollection_Unused_Space()
		{
			this.InitializeComponent();

			this.DataContext = _vm = new MyViewModel();
			_vm.MyItems.Add("Test 0");
			Loaded += MainPage_Loaded;
		}

		private async void MainPage_Loaded(object sender, RoutedEventArgs e)
		{
			await Task.Delay(20);
			HeightTextBlock.Text = TargetListView.ActualHeight.ToString();
			StatusTextBlock.Text = "Ready";
		}

		private async void AddItems(object sender, RoutedEventArgs args)
		{
			_vm.MyItems.Add("Test 1");
			await Task.Delay(20);
			_vm.MyItems.Add("Test 2");
			await Task.Delay(20);

			_ = Dispatcher.RunIdleAsync(_ =>
			{
				HeightTextBlock.Text = TargetListView.ActualHeight.ToString();
				StatusTextBlock.Text = "Finished";
			});
		}

		public class MyViewModel
		{
			public ObservableCollection<string> MyItems { get; set; }

			public MyViewModel()
			{
				MyItems = new ObservableCollection<string>();
			}
		}
	}
}
