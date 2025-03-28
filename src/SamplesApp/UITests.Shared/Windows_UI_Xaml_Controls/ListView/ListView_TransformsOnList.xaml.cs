using System.Linq;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ListView
{
	[SampleControlInfo("ListView", "ListView_TransformsOnList", description: "Rotate the list and it should continue to scroll smoothly.")]
	public sealed partial class ListView_TransformsOnList : Page
	{
		public ListView_TransformsOnList()
		{
			InitializeComponent();

			lst.ItemsSource = Enumerable
				.Range(0, 60)
				.Reverse()
				.Select(i => $"Items #{i}");
		}
	}
}
