using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace UITests.Windows_UI_ViewManagement;

[Sample("Windows.UI.ViewManagement",
	Name = "SystemAccentColor",
	Description = "Shows that SystemAccentColor and the accent brushes follow the OS accent color. "
		+ "On macOS, open System Settings → Appearance and change the accent color while this "
		+ "sample is visible: the swatches and the controls below update live. The override buttons "
		+ "force a specific accent color on any platform.",
	IsManualTest = true,
	IgnoreInSnapshotTests = true)]
public sealed partial class SystemAccentColorSample : Page
{
	private readonly UISettings _uiSettings = new();

#if HAS_UNO
	private readonly Color? _previousOverride = Uno.UI.FeatureConfiguration.AccentColor.OverrideAccentColor;
#endif

	public SystemAccentColorSample()
	{
		this.InitializeComponent();

		_uiSettings.ColorValuesChanged += OnColorValuesChanged;
		Unloaded += OnUnloaded;

		UpdateColors();
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		_uiSettings.ColorValuesChanged -= OnColorValuesChanged;
#if HAS_UNO
		// Restore whatever override was active before this sample ran.
		Uno.UI.FeatureConfiguration.AccentColor.OverrideAccentColor = _previousOverride;
#endif
	}

	private void OnRefreshClick(object sender, RoutedEventArgs e) => UpdateColors();

	private void OnOverrideRedClick(object sender, RoutedEventArgs e) => SetOverride(Color.FromArgb(0xFF, 0xD0, 0x21, 0x2B));

	private void OnOverrideGreenClick(object sender, RoutedEventArgs e) => SetOverride(Color.FromArgb(0xFF, 0x10, 0x7C, 0x10));

	private void OnOverridePurpleClick(object sender, RoutedEventArgs e) => SetOverride(Color.FromArgb(0xFF, 0x88, 0x17, 0x98));

	private void OnClearOverrideClick(object sender, RoutedEventArgs e) => SetOverride(null);

	private void SetOverride(Color? color)
	{
#if HAS_UNO
		Uno.UI.FeatureConfiguration.AccentColor.OverrideAccentColor = color;
#endif
	}

	private void OnColorValuesChanged(UISettings sender, object args)
		=> this.DispatcherQueue.TryEnqueue(UpdateColors);

	private void UpdateColors()
	{
		SetSwatch(AccentSwatch, AccentHex, UIColorType.Accent);
		SetSwatch(Light1Swatch, Light1Hex, UIColorType.AccentLight1);
		SetSwatch(Light2Swatch, Light2Hex, UIColorType.AccentLight2);
		SetSwatch(Light3Swatch, Light3Hex, UIColorType.AccentLight3);
		SetSwatch(Dark1Swatch, Dark1Hex, UIColorType.AccentDark1);
		SetSwatch(Dark2Swatch, Dark2Hex, UIColorType.AccentDark2);
		SetSwatch(Dark3Swatch, Dark3Hex, UIColorType.AccentDark3);
	}

	private void SetSwatch(Border swatch, TextBlock hex, UIColorType type)
	{
		var color = _uiSettings.GetColorValue(type);
		swatch.Background = new SolidColorBrush(color);
		hex.Text = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
	}
}
