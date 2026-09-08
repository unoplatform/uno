using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.SemanticZoomTests;

[Sample(
	"SemanticZoom",
	Name = "SemanticZoom_Basic",
	IsManualTest = true,
	Description = "Switches between grouped detail and overview lists using SemanticZoom.")]
public sealed partial class SemanticZoom_Basic : UserControl
{
	public SemanticZoom_Basic()
	{
		this.InitializeComponent();
		GroupedItems.Source = CreateGroups();
		UpdateActiveViewText();
	}

	private void OnToggleView(object sender, RoutedEventArgs e) =>
		SemanticZoomControl.ToggleActiveView();

	private void OnViewChangeCompleted(object sender, SemanticZoomViewChangedEventArgs e) =>
		UpdateActiveViewText();

	private void UpdateActiveViewText() =>
		ActiveViewText.Text = SemanticZoomControl.IsZoomedInViewActive ? "Detailed view" : "Group overview";

	private static IReadOnlyList<SemanticZoomSampleGroup> CreateGroups() =>
	[
		new("A", ["Aster", "Atlas", "Aurora", "Azure"]),
		new("B", ["Basil", "Birch", "Bluebell", "Bramble"]),
		new("C", ["Cedar", "Clover", "Cosmos", "Cypress"]),
		new("D", ["Dahlia", "Daisy", "Daphne", "Dogwood"]),
		new("E", ["Elm", "Erica", "Eucalyptus", "Evening primrose"]),
		new("F", ["Fern", "Fir", "Flax", "Foxglove"]),
	];

	private sealed class SemanticZoomSampleGroup : List<string>
	{
		public SemanticZoomSampleGroup(string key, IEnumerable<string> items)
			: base(items)
		{
			Key = key;
		}

		public string Key { get; }
	}
}
