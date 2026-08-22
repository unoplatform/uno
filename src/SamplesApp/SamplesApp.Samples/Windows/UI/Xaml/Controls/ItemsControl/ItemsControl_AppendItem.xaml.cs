using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

namespace UITests.Windows_UI_Xaml_Controls.ItemsControl
{
	[Sample("ItemsControl")]
	public sealed partial class ItemsControl_AppendItem : UserControl
	{
		private ObservableCollection<string> _collection;

		public ItemsControl_AppendItem()
		{
			this.InitializeComponent();

			_collection = new ObservableCollection<string>();

			_collection.Add("AppendItem01");

			theItemsControl.ItemsSource = _collection;
		}

		private void OnAppendContent01()
		{
			_collection.Add("AppendItem02");
		}
	}
}
