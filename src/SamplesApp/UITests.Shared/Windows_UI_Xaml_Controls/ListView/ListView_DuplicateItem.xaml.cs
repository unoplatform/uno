using System.Collections.ObjectModel;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace UITests.Windows_UI_Xaml_Controls.ListView
{
	[Sample("ListView")]
	public sealed partial class ListView_DuplicateItem : Page
	{
		public ListView_DuplicateItem()
		{
			this.InitializeComponent();
			var items = new ObservableCollection<string>(new[]
			{
				"String 1",
				"String 1",
				"String 1",
				"String 2",
				"String 2",
				"String 2",
				"String 3",
				"String 3",
				"String 3",
				"String 1",
				"String 1",
				"String 1",
				"String 2",
				"String 2",
				"String 2",
				"String 3",
				"String 3",
				"String 3",
			});
			listView.ItemsSource = items;

		}
	}
}
