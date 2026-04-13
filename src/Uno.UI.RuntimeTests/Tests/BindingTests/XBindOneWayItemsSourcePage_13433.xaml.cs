using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests
{
	public sealed partial class XBindOneWayItemsSourcePage_13433 : Page
	{
		public ObservableCollection<string> Items { get; set; } = new ObservableCollection<string> { "a", "b", "c" };

		public XBindOneWayItemsSourcePage_13433()
		{
			this.InitializeComponent();
		}

		public ListView ItemsListView => SUT;
	}
}
