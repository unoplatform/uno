using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.UI.Samples.Controls;
using Windows.UI;

namespace UITests.Windows_UI_Xaml_Controls.MonochromaticOverlayPresenterTests;

[Sample(
	"Pickers",
	Name = "MonochromaticOverlayPresenter_Showcase",
	Description = "MonochromaticOverlayPresenter redraws the source element's content under it in a single ReplacementColor. "
		+ "Used by the DatePicker/TimePicker flyouts for their highlight band; the band must follow the value while a column scrolls.",
	IgnoreInSnapshotTests = true)]
public sealed partial class MonochromaticOverlayPresenter_Showcase : Page
{
	private Color? _themeReplacementColor;

	public MonochromaticOverlayPresenter_Showcase()
	{
		this.InitializeComponent();

		SourceItems.ItemsSource = Enumerable.Range(1, 50).Select(i => $"Item {i}").ToArray();
	}

	private void OnColorSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (LiveOverlay is null)
		{
			return;
		}

		_themeReplacementColor ??= LiveOverlay.ReplacementColor;
		LiveOverlay.ReplacementColor = ColorPicker.SelectedIndex switch
		{
			1 => Colors.Red,
			2 => Colors.Gold,
			3 => Colors.Black,
			4 => Colors.Transparent,
			_ => _themeReplacementColor.Value,
		};
	}

	private void OnWidthChanged(object sender, RangeBaseValueChangedEventArgs e)
	{
		if (LiveOverlay is not null)
		{
			LiveOverlay.Width = e.NewValue;
		}
	}
}
