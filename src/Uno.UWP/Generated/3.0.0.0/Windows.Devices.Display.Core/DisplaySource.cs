#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Display.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DisplaySource 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.DisplayAdapterId AdapterId
		{
			get
			{
				throw new global::System.NotImplementedException("The member DisplayAdapterId DisplaySource.AdapterId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=DisplayAdapterId%20DisplaySource.AdapterId");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint SourceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint DisplaySource.SourceId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=uint%20DisplaySource.SourceId");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Display.Core.DisplaySourceStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member DisplaySourceStatus DisplaySource.Status is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=DisplaySourceStatus%20DisplaySource.Status");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Display.Core.DisplaySource.AdapterId.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplaySource.SourceId.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer GetMetadata( global::System.Guid Key)
		{
			throw new global::System.NotImplementedException("The member IBuffer DisplaySource.GetMetadata(Guid Key) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IBuffer%20DisplaySource.GetMetadata%28Guid%20Key%29");
		}
		#endif
		// Forced skipping of method Windows.Devices.Display.Core.DisplaySource.Status.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplaySource.StatusChanged.add
		// Forced skipping of method Windows.Devices.Display.Core.DisplaySource.StatusChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Display.Core.DisplaySource, object> StatusChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Display.Core.DisplaySource", "event TypedEventHandler<DisplaySource, object> DisplaySource.StatusChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Display.Core.DisplaySource", "event TypedEventHandler<DisplaySource, object> DisplaySource.StatusChanged");
			}
		}
		#endif
	}
}
