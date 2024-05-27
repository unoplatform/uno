using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Controls;
using SamplesApp.Windows_UI_Xaml_Controls.ToggleSwitchControl.Models;
using Uno.UI.Samples.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.ButtonTests
{
	[Sample(
		"Buttons",
		IsManualTest = true,
		Description = 
			"This sample is specifically for macOS laptops. Go to system Settings -> Trackpad and enable Tap to Click feature. " + 
			"Then move mouse over the button and light-tap the trackpad repeatedly. The button should get toggled on each tap reliably.")]
	public sealed partial class ToggleButton_TapToClick : UserControl
	{
		public ToggleButton_TapToClick()
		{
			this.InitializeComponent();
			TestSwitch.Click += (s, e) => EventLog.Add("Click");
			TestSwitch.Tapped += (s, e) => EventLog.Add("Tapped");
			TestSwitch.PointerPressed += (s, e) => EventLog.Add("PointerPressed");
			TestSwitch.PointerReleased += (s, e) => EventLog.Add("PointerReleased");
			TestSwitch.PointerEntered += (s, e) => EventLog.Add("PointerEntered");
			TestSwitch.PointerExited += (s, e) => EventLog.Add("PointerExited");
		}

		public ObservableCollection<string> EventLog { get; } = new();
	}
}
