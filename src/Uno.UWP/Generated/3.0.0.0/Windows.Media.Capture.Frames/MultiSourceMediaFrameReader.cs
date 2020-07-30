#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Capture.Frames
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MultiSourceMediaFrameReader : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Capture.Frames.MediaFrameReaderAcquisitionMode AcquisitionMode
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaFrameReaderAcquisitionMode MultiSourceMediaFrameReader.AcquisitionMode is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.Frames.MultiSourceMediaFrameReader", "MediaFrameReaderAcquisitionMode MultiSourceMediaFrameReader.AcquisitionMode");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Capture.Frames.MultiSourceMediaFrameReader.FrameArrived.add
		// Forced skipping of method Windows.Media.Capture.Frames.MultiSourceMediaFrameReader.FrameArrived.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Capture.Frames.MultiSourceMediaFrameReference TryAcquireLatestFrame()
		{
			throw new global::System.NotImplementedException("The member MultiSourceMediaFrameReference MultiSourceMediaFrameReader.TryAcquireLatestFrame() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Capture.Frames.MultiSourceMediaFrameReaderStartStatus> StartAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MultiSourceMediaFrameReaderStartStatus> MultiSourceMediaFrameReader.StartAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction StopAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction MultiSourceMediaFrameReader.StopAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.Frames.MultiSourceMediaFrameReader", "void MultiSourceMediaFrameReader.Dispose()");
		}
		#endif
		// Forced skipping of method Windows.Media.Capture.Frames.MultiSourceMediaFrameReader.AcquisitionMode.set
		// Forced skipping of method Windows.Media.Capture.Frames.MultiSourceMediaFrameReader.AcquisitionMode.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Capture.Frames.MultiSourceMediaFrameReader, global::Windows.Media.Capture.Frames.MultiSourceMediaFrameArrivedEventArgs> FrameArrived
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.Frames.MultiSourceMediaFrameReader", "event TypedEventHandler<MultiSourceMediaFrameReader, MultiSourceMediaFrameArrivedEventArgs> MultiSourceMediaFrameReader.FrameArrived");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.Frames.MultiSourceMediaFrameReader", "event TypedEventHandler<MultiSourceMediaFrameReader, MultiSourceMediaFrameArrivedEventArgs> MultiSourceMediaFrameReader.FrameArrived");
			}
		}
		#endif
		// Processing: System.IDisposable
	}
}
