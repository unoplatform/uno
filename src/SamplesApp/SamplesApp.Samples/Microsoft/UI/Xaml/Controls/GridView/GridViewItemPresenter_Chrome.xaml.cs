#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SamplesApp.Windows_UI_Xaml_Controls.ListView;
using Uno.UI.Samples.Controls;

namespace Uno.UI.Samples.Content.UITests.GridView;

[Sample("GridView", Name = "GridViewItemPresenter_Chrome", Description = "GridViewItem chrome with the default GridViewItem style (ListViewItemPresenter) or the legacy GridViewItemPresenter: selection, check and indicator modes, rounded vs non-rounded, disabled items, custom brushes.")]
public sealed partial class GridViewItemPresenter_Chrome : Page
{
	private readonly ItemPresenterChromeSampleController _controller;

	public GridViewItemPresenter_Chrome()
	{
		this.InitializeComponent();

		_controller = new ItemPresenterChromeSampleController(this, ListHost, CreateList, SelectionModeBox, CheckModeBox, IndicatorModeBox, RoundedBox, DisabledBox, BrushesBox);
	}

	private Microsoft.UI.Xaml.Controls.GridView CreateList() => new()
	{
		ItemContainerStyle = (Style)Resources[LegacyPresenterBox.IsChecked == true ? "LegacyPresenterItemStyle" : "PresenterItemStyle"],
	};

	private void OnLegacyPresenterChanged(object sender, RoutedEventArgs e) => _controller.RebuildList();
}
