using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.DragAndDrop;

[Sample("DragAndDrop", "ListView",
	Description = "This automated tests validate that reordering selected items in ListView keeps valid selection.",
	IgnoreInSnapshotTests = true)]
public sealed partial class DragDrop_ListView_Selection : Page
{
	public DragDrop_ListView_Selection()
	{
		this.InitializeComponent();
	}

	public ObservableCollection<string> Items { get; } = new(Enumerable.Range(1, 32).Select(i => $"Item #{i}"));

	private void SUT_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		SelectedOuput.Text = e.AddedItems?.FirstOrDefault()?.ToString() ?? "--none--";
	}
}
