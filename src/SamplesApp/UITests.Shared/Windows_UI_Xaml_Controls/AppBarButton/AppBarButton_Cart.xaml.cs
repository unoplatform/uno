using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace UITests.Shared.Windows_UI_Xaml_Controls.AppBarButton
{
	[SampleControlInfo("AppBar", nameof(AppBarButton_Cart))]
	public sealed partial class AppBarButton_Cart : UserControl
	{
		private ObservableCollection<string> _items = new ObservableCollection<string>();

		public AppBarButton_Cart()
		{
			this.InitializeComponent();

			_items.CollectionChanged += OnItemsChanged;
			UpdateBadge();
		}

		private void OnItemsChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			UpdateBadge();
		}

		private void AddToCart(object sender, RoutedEventArgs e)
		{
			_items.Add(DateTimeOffset.Now.ToString());
		}

		private void ClearCart(object sender, RoutedEventArgs e)
		{
			_items.Clear();
		}

		private void UpdateBadge()
		{
			Badge.Visibility = _items.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
			Count.Text = _items.Count.ToString();
		}
	}
}
