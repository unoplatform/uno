using System.Collections.ObjectModel;
using Windows.UI.Xaml.Controls;
using SamplesApp.Windows_UI_Xaml_Controls.ToggleSwitchControl.Models;
using Uno.UI.Samples.Controls;

namespace Uno.UI.Samples.Content.UITests.ButtonTestsControl;

[Sample(
	"Buttons",
	IgnoreInSnapshotTests = true,
	IsManualTest = true,
	UsesFrame = false,
	Description =
		"This sample is specifically for macOS laptops. Go to system Settings -> Trackpad and enable Tap to Click feature. " +
		"Then move mouse over the button and light-tap the trackpad repeatedly. The button should get toggled on each tap reliably.")]
public sealed partial class ToggleButton_TapToClick : Page
{
	public ToggleButton_TapToClick()
	{
		this.InitializeComponent();
		MyToggle.Click += (s, e) => EventLog.Add("Click");
		MyToggle.Tapped += (s, e) => EventLog.Add("Tapped");
		MyToggle.PointerPressed += (s, e) => EventLog.Add("PointerPressed");
		MyToggle.PointerReleased += (s, e) => EventLog.Add("PointerReleased");
		MyToggle.PointerEntered += (s, e) => EventLog.Add("PointerEntered");
		MyToggle.PointerExited += (s, e) => EventLog.Add("PointerExited");
	}

	public ObservableCollection<string> EventLog { get; } = new();
}
