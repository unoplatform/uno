#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.BackgroundTransfer
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class BackgroundTransferRangesDownloadedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.Networking.BackgroundTransfer.BackgroundTransferFileRange> AddedRanges
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<BackgroundTransferFileRange> BackgroundTransferRangesDownloadedEventArgs.AddedRanges is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IList%3CBackgroundTransferFileRange%3E%20BackgroundTransferRangesDownloadedEventArgs.AddedRanges");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool WasDownloadRestarted
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool BackgroundTransferRangesDownloadedEventArgs.WasDownloadRestarted is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20BackgroundTransferRangesDownloadedEventArgs.WasDownloadRestarted");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.BackgroundTransfer.BackgroundTransferRangesDownloadedEventArgs.WasDownloadRestarted.get
		// Forced skipping of method Windows.Networking.BackgroundTransfer.BackgroundTransferRangesDownloadedEventArgs.AddedRanges.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Deferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member Deferral BackgroundTransferRangesDownloadedEventArgs.GetDeferral() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Deferral%20BackgroundTransferRangesDownloadedEventArgs.GetDeferral%28%29");
		}
		#endif
	}
}
