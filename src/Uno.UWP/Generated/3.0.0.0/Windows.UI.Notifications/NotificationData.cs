#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Notifications
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class NotificationData 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint SequenceNumber
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint NotificationData.SequenceNumber is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Notifications.NotificationData", "uint NotificationData.SequenceNumber");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IDictionary<string, string> Values
		{
			get
			{
				throw new global::System.NotImplementedException("The member IDictionary<string, string> NotificationData.Values is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public NotificationData( global::System.Collections.Generic.IEnumerable<global::System.Collections.Generic.KeyValuePair<string, string>> initialValues,  uint sequenceNumber) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Notifications.NotificationData", "NotificationData.NotificationData(IEnumerable<KeyValuePair<string, string>> initialValues, uint sequenceNumber)");
		}
		#endif
		// Forced skipping of method Windows.UI.Notifications.NotificationData.NotificationData(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>>, uint)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public NotificationData( global::System.Collections.Generic.IEnumerable<global::System.Collections.Generic.KeyValuePair<string, string>> initialValues) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Notifications.NotificationData", "NotificationData.NotificationData(IEnumerable<KeyValuePair<string, string>> initialValues)");
		}
		#endif
		// Forced skipping of method Windows.UI.Notifications.NotificationData.NotificationData(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>>)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public NotificationData() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Notifications.NotificationData", "NotificationData.NotificationData()");
		}
		#endif
		// Forced skipping of method Windows.UI.Notifications.NotificationData.NotificationData()
		// Forced skipping of method Windows.UI.Notifications.NotificationData.Values.get
		// Forced skipping of method Windows.UI.Notifications.NotificationData.SequenceNumber.get
		// Forced skipping of method Windows.UI.Notifications.NotificationData.SequenceNumber.set
	}
}
