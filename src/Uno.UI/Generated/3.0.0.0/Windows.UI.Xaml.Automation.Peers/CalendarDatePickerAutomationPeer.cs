#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation.Peers
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CalendarDatePickerAutomationPeer : global::Windows.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer,global::Windows.UI.Xaml.Automation.Provider.IInvokeProvider,global::Windows.UI.Xaml.Automation.Provider.IValueProvider
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsReadOnly
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CalendarDatePickerAutomationPeer.IsReadOnly is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Value
		{
			get
			{
				throw new global::System.NotImplementedException("The member string CalendarDatePickerAutomationPeer.Value is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public CalendarDatePickerAutomationPeer( global::Windows.UI.Xaml.Controls.CalendarDatePicker owner) : base(owner)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.CalendarDatePickerAutomationPeer", "CalendarDatePickerAutomationPeer.CalendarDatePickerAutomationPeer(CalendarDatePicker owner)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Automation.Peers.CalendarDatePickerAutomationPeer.CalendarDatePickerAutomationPeer(Windows.UI.Xaml.Controls.CalendarDatePicker)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Invoke()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.CalendarDatePickerAutomationPeer", "void CalendarDatePickerAutomationPeer.Invoke()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Automation.Peers.CalendarDatePickerAutomationPeer.IsReadOnly.get
		// Forced skipping of method Windows.UI.Xaml.Automation.Peers.CalendarDatePickerAutomationPeer.Value.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetValue( string value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Automation.Peers.CalendarDatePickerAutomationPeer", "void CalendarDatePickerAutomationPeer.SetValue(string value)");
		}
		#endif
		// Processing: Windows.UI.Xaml.Automation.Provider.IInvokeProvider
		// Processing: Windows.UI.Xaml.Automation.Provider.IValueProvider
	}
}
