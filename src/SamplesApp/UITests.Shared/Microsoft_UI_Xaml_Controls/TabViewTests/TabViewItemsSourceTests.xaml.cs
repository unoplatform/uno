using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Core;
using Uno.UI.Samples.Controls;

namespace UITests.Microsoft_UI_Xaml_Controls.TabViewTests
{
	[Sample("TabView", "WinUI")]
    public sealed partial class TabViewItemsSourceTests : Page
    {
		public DemoViewModel DefaultViewModel
		{
			get { return (DemoViewModel)GetValue(DefaultViewModelProperty); }
			set { SetValue(DefaultViewModelProperty, value); }
		}

		public static readonly DependencyProperty DefaultViewModelProperty = DependencyProperty.Register(
			nameof(DefaultViewModel),
			typeof(DemoViewModel),
			typeof(TabViewItemsSourceTests),
			new PropertyMetadata(null));



		public TabViewItemsSourceTests()
		{
			DemoViewModel defaultViewModel = new DemoViewModel(Dispatcher);
			// add Tabs as source for binding of TabView
			defaultViewModel.TabItems.Add(new TabItemViewModel(Dispatcher) { Header = "Tab 1" });
			defaultViewModel.TabItems.Add(new TabItemViewModel(Dispatcher) { Header = "Tab 2" });
			defaultViewModel.TabItems.Add(new TabItemViewModel(Dispatcher) { Header = "Tab 3" });
			defaultViewModel.TabItems.Add(new TabItemViewModel(Dispatcher) { Header = "Tab 4" });

			defaultViewModel.RibbonTabs.Add(new TabItemViewModel(Dispatcher) { Header = "File" });
			defaultViewModel.RibbonTabs.Add(new TabItemViewModel(Dispatcher) { Header = "View" });

			this.DefaultViewModel = defaultViewModel;

			this.InitializeComponent();
		}

		private void OnAddTabViewItemButtonClick(object sender, RoutedEventArgs e)
		{
			this.DefaultViewModel.TabItems.Add(new TabItemViewModel(Dispatcher) { Header = $"Tab {this.DefaultViewModel.TabItems.Count + 1}" });
		}
	}

	[System.ComponentModel.Bindable(BindableSupport.Yes)]
	public class DemoViewModel : ViewModelBase
	{
		public DemoViewModel(CoreDispatcher dispatcher) : base(dispatcher)
		{
		}

		public ObservableCollection<TabItemViewModel> TabItems { get; } = new ObservableCollection<TabItemViewModel>();
		public ObservableCollection<TabItemViewModel> RibbonTabs { get; } = new ObservableCollection<TabItemViewModel>();
	}

	[System.ComponentModel.Bindable(BindableSupport.Yes)]
	public class TabItemViewModel : ViewModelBase
	{
		private string header;

		public TabItemViewModel(CoreDispatcher dispatcher) : base(dispatcher)
		{
		}

		public string Header
		{
			get
			{
				return this.header;
			}
			set
			{
				if (this.header != value)
				{
					this.header = value;
					RaisePropertyChanged(nameof(Header));
				}
			}
		}
	}
}
