using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Automation;

[Sample("Automation", Name = "UiaDuplicateItems_ListView", Description = "A ListView repeating one item instance: each row must be a distinct accessibility element with its own label and frame.")]
public sealed partial class UiaDuplicateItems_ListView : Page
{
	public UiaDuplicateItems_ListView()
	{
		this.InitializeComponent();

		// One instance, repeated: the rows share a single item automation peer.
		var duplicate = "Duplicate item";
		DuplicateList.ItemsSource = new[] { duplicate, duplicate, "Unique item", duplicate };
		DuplicateList.SelectionChanged += (_, _) =>
			SelectionResult.Text = $"Selected index: {DuplicateList.SelectedIndex}";
	}
}
