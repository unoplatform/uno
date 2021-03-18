#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Perception
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PerceptionColorFrameReader : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsPaused
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PerceptionColorFrameReader.IsPaused is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Perception.PerceptionColorFrameReader", "bool PerceptionColorFrameReader.IsPaused");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Perception.PerceptionColorFrameSource Source
		{
			get
			{
				throw new global::System.NotImplementedException("The member PerceptionColorFrameSource PerceptionColorFrameReader.Source is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Perception.PerceptionColorFrameReader.FrameArrived.add
		// Forced skipping of method Windows.Devices.Perception.PerceptionColorFrameReader.FrameArrived.remove
		// Forced skipping of method Windows.Devices.Perception.PerceptionColorFrameReader.Source.get
		// Forced skipping of method Windows.Devices.Perception.PerceptionColorFrameReader.IsPaused.get
		// Forced skipping of method Windows.Devices.Perception.PerceptionColorFrameReader.IsPaused.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Perception.PerceptionColorFrame TryReadLatestFrame()
		{
			throw new global::System.NotImplementedException("The member PerceptionColorFrame PerceptionColorFrameReader.TryReadLatestFrame() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Perception.PerceptionColorFrameReader", "void PerceptionColorFrameReader.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Perception.PerceptionColorFrameReader, global::Windows.Devices.Perception.PerceptionColorFrameArrivedEventArgs> FrameArrived
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Perception.PerceptionColorFrameReader", "event TypedEventHandler<PerceptionColorFrameReader, PerceptionColorFrameArrivedEventArgs> PerceptionColorFrameReader.FrameArrived");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Perception.PerceptionColorFrameReader", "event TypedEventHandler<PerceptionColorFrameReader, PerceptionColorFrameArrivedEventArgs> PerceptionColorFrameReader.FrameArrived");
			}
		}
		#endif
		// Processing: System.IDisposable
	}
}
