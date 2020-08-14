#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.DataTransfer
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ClipboardContentOptions 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsRoamable
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ClipboardContentOptions.IsRoamable is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.ClipboardContentOptions", "bool ClipboardContentOptions.IsRoamable");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsAllowedInHistory
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ClipboardContentOptions.IsAllowedInHistory is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.ClipboardContentOptions", "bool ClipboardContentOptions.IsAllowedInHistory");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<string> HistoryFormats
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<string> ClipboardContentOptions.HistoryFormats is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<string> RoamingFormats
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<string> ClipboardContentOptions.RoamingFormats is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public ClipboardContentOptions() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.ClipboardContentOptions", "ClipboardContentOptions.ClipboardContentOptions()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.ClipboardContentOptions.ClipboardContentOptions()
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.ClipboardContentOptions.IsRoamable.get
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.ClipboardContentOptions.IsRoamable.set
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.ClipboardContentOptions.IsAllowedInHistory.get
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.ClipboardContentOptions.IsAllowedInHistory.set
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.ClipboardContentOptions.RoamingFormats.get
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.ClipboardContentOptions.HistoryFormats.get
	}
}
