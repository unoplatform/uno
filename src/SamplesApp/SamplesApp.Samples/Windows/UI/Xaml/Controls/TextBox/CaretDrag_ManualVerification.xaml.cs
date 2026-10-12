using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace Uno.UI.Samples.Content.UITests.TextBoxControl;

[Sample("TextBox", Name = "CaretDrag_ManualVerification", IsManualTest = true,
	Description = "Device-only checklist for the iOS space-bar trackpad caret drag - see the on-screen notes for why it can't be automated.")]
public sealed partial class CaretDrag_ManualVerification : UserControl
{
	private int _selectionChangedCount;

	public CaretDrag_ManualVerification()
	{
		this.InitializeComponent();
	}

	private void OnSelectionChanged(object sender, RoutedEventArgs e)
	{
		_selectionChangedCount++;
		SelectionChangedCountText.Text = $"SelectionChanged count: {_selectionChangedCount}";
	}
}
