using System.Linq;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.Repeater;

[Sample("ItemsRepeater", IsManualTest = true, Description = "Very rapid scrolling shouldn't cause flickering")]
public sealed partial class ItemsRepeaterManyItems : Page
{
	public ItemsRepeaterManyItems()
	{
		this.InitializeComponent();
		itemsRepeater.ItemsSource = Enumerable.Range(0, 20000000);
	}
}
