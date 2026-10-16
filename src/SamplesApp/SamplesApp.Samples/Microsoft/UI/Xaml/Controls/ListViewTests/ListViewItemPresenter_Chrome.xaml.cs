#nullable enable

using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView;

[Sample("ListView", Name = "ListViewItemPresenter_Chrome", Description = "ListViewItemPresenter chrome with the default ListViewItem style: selection, check and indicator modes, rounded vs non-rounded, disabled items, custom brushes.")]
public sealed partial class ListViewItemPresenter_Chrome : Page
{
	public ListViewItemPresenter_Chrome()
	{
		this.InitializeComponent();

		_ = new ItemPresenterChromeSampleController(this, ListHost, () => new Microsoft.UI.Xaml.Controls.ListView(), SelectionModeBox, CheckModeBox, IndicatorModeBox, RoundedBox, DisabledBox, BrushesBox);
	}
}
