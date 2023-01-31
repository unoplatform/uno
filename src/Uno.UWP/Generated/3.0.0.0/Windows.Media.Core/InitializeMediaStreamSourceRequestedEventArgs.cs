#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class InitializeMediaStreamSourceRequestedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IRandomAccessStream RandomAccessStream
		{
			get
			{
				throw new global::System.NotImplementedException("The member IRandomAccessStream InitializeMediaStreamSourceRequestedEventArgs.RandomAccessStream is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IRandomAccessStream%20InitializeMediaStreamSourceRequestedEventArgs.RandomAccessStream");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Core.MediaStreamSource Source
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaStreamSource InitializeMediaStreamSourceRequestedEventArgs.Source is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=MediaStreamSource%20InitializeMediaStreamSourceRequestedEventArgs.Source");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Core.InitializeMediaStreamSourceRequestedEventArgs.Source.get
		// Forced skipping of method Windows.Media.Core.InitializeMediaStreamSourceRequestedEventArgs.RandomAccessStream.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Deferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member Deferral InitializeMediaStreamSourceRequestedEventArgs.GetDeferral() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Deferral%20InitializeMediaStreamSourceRequestedEventArgs.GetDeferral%28%29");
		}
		#endif
	}
}
