#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation.Peers
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial class TimePickerAutomationPeer : global::Windows.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public TimePickerAutomationPeer( global::Windows.UI.Xaml.Controls.TimePicker owner) : base(owner)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.TimePickerAutomationPeer", "TimePickerAutomationPeer.TimePickerAutomationPeer(TimePicker owner)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Automation.Peers.TimePickerAutomationPeer.TimePickerAutomationPeer(Windows.UI.Xaml.Controls.TimePicker)
	}
}
