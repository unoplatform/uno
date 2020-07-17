#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || NET461 || false || __SKIA__ || __NETSTD_REFERENCE__ || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class TimePickerFlyout 
	{
		// Skipping already declared property Time
		// Skipping already declared property MinuteIncrement
		// Skipping already declared property ClockIdentifier
		// Skipping already declared property ClockIdentifierProperty
		// Skipping already declared property MinuteIncrementProperty
		// Skipping already declared property TimeProperty
		// Skipping already declared method Windows.UI.Xaml.Controls.TimePickerFlyout.TimePickerFlyout()
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePickerFlyout.TimePickerFlyout()
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePickerFlyout.ClockIdentifier.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePickerFlyout.ClockIdentifier.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePickerFlyout.Time.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePickerFlyout.Time.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePickerFlyout.MinuteIncrement.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePickerFlyout.MinuteIncrement.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePickerFlyout.TimePicked.add
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePickerFlyout.TimePicked.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.TimeSpan?> ShowAtAsync( global::Windows.UI.Xaml.FrameworkElement target)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<TimeSpan?> TimePickerFlyout.ShowAtAsync(FrameworkElement target) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePickerFlyout.ClockIdentifierProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePickerFlyout.TimeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TimePickerFlyout.MinuteIncrementProperty.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.TimePickerFlyout, global::Windows.UI.Xaml.Controls.TimePickedEventArgs> TimePicked
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TimePickerFlyout", "event TypedEventHandler<TimePickerFlyout, TimePickedEventArgs> TimePickerFlyout.TimePicked");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TimePickerFlyout", "event TypedEventHandler<TimePickerFlyout, TimePickedEventArgs> TimePickerFlyout.TimePicked");
			}
		}
		#endif
	}
}
