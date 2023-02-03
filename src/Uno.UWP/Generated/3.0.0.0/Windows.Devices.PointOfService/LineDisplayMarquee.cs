#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.PointOfService
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class LineDisplayMarquee 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan ScrollWaitInterval
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan LineDisplayMarquee.ScrollWaitInterval is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=TimeSpan%20LineDisplayMarquee.ScrollWaitInterval");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.LineDisplayMarquee", "TimeSpan LineDisplayMarquee.ScrollWaitInterval");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan RepeatWaitInterval
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan LineDisplayMarquee.RepeatWaitInterval is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=TimeSpan%20LineDisplayMarquee.RepeatWaitInterval");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.LineDisplayMarquee", "TimeSpan LineDisplayMarquee.RepeatWaitInterval");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.PointOfService.LineDisplayMarqueeFormat Format
		{
			get
			{
				throw new global::System.NotImplementedException("The member LineDisplayMarqueeFormat LineDisplayMarquee.Format is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=LineDisplayMarqueeFormat%20LineDisplayMarquee.Format");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.LineDisplayMarquee", "LineDisplayMarqueeFormat LineDisplayMarquee.Format");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.PointOfService.LineDisplayMarquee.Format.get
		// Forced skipping of method Windows.Devices.PointOfService.LineDisplayMarquee.Format.set
		// Forced skipping of method Windows.Devices.PointOfService.LineDisplayMarquee.RepeatWaitInterval.get
		// Forced skipping of method Windows.Devices.PointOfService.LineDisplayMarquee.RepeatWaitInterval.set
		// Forced skipping of method Windows.Devices.PointOfService.LineDisplayMarquee.ScrollWaitInterval.get
		// Forced skipping of method Windows.Devices.PointOfService.LineDisplayMarquee.ScrollWaitInterval.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryStartScrollingAsync( global::Windows.Devices.PointOfService.LineDisplayScrollDirection direction)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> LineDisplayMarquee.TryStartScrollingAsync(LineDisplayScrollDirection direction) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3Cbool%3E%20LineDisplayMarquee.TryStartScrollingAsync%28LineDisplayScrollDirection%20direction%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryStopScrollingAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> LineDisplayMarquee.TryStopScrollingAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3Cbool%3E%20LineDisplayMarquee.TryStopScrollingAsync%28%29");
		}
		#endif
	}
}
