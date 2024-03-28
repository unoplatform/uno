using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Microsoft_UI_Xaml_Controls.ExpanderTests
{
	// We are going to test setting the events source of an expander to the customcontrol's
	public sealed partial class TestControl : ContentControl
	{
		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new TestControlAutomationPeer(this);
		}
	}

	public sealed partial class TestControlAutomationPeer : FrameworkElementAutomationPeer
	{
		public TestControlAutomationPeer(TestControl owner) : base(owner) { }
		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.Custom;
		}
	}

	[Sample("Expander", "MUX", Name = "WinUIExpanderPage")]
	public sealed partial class WinUIExpanderPage : UserControl
	{
		public WinUIExpanderPage()
		{
			this.InitializeComponent();
#if !WINAPPSDK
			var customControlPeer = FrameworkElementAutomationPeer.FromElement(CustomControl);
			var expanderPeer = FrameworkElementAutomationPeer.FromElement(ExpanderWithCustomEventsSource);

			// Commenting because of MuxTestInfra bug: 
			// https://github.com/microsoft/microsoft-ui-xaml/issues/3491
			//expanderPeer.EventsSource = customControlPeer;
#endif
		}
	}
}
