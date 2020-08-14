#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Perception
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PerceptionDepthFrameReader : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsPaused
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PerceptionDepthFrameReader.IsPaused is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Perception.PerceptionDepthFrameReader", "bool PerceptionDepthFrameReader.IsPaused");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Perception.PerceptionDepthFrameSource Source
		{
			get
			{
				throw new global::System.NotImplementedException("The member PerceptionDepthFrameSource PerceptionDepthFrameReader.Source is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Perception.PerceptionDepthFrameReader.FrameArrived.add
		// Forced skipping of method Windows.Devices.Perception.PerceptionDepthFrameReader.FrameArrived.remove
		// Forced skipping of method Windows.Devices.Perception.PerceptionDepthFrameReader.Source.get
		// Forced skipping of method Windows.Devices.Perception.PerceptionDepthFrameReader.IsPaused.get
		// Forced skipping of method Windows.Devices.Perception.PerceptionDepthFrameReader.IsPaused.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Perception.PerceptionDepthFrame TryReadLatestFrame()
		{
			throw new global::System.NotImplementedException("The member PerceptionDepthFrame PerceptionDepthFrameReader.TryReadLatestFrame() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Perception.PerceptionDepthFrameReader", "void PerceptionDepthFrameReader.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Perception.PerceptionDepthFrameReader, global::Windows.Devices.Perception.PerceptionDepthFrameArrivedEventArgs> FrameArrived
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Perception.PerceptionDepthFrameReader", "event TypedEventHandler<PerceptionDepthFrameReader, PerceptionDepthFrameArrivedEventArgs> PerceptionDepthFrameReader.FrameArrived");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Perception.PerceptionDepthFrameReader", "event TypedEventHandler<PerceptionDepthFrameReader, PerceptionDepthFrameArrivedEventArgs> PerceptionDepthFrameReader.FrameArrived");
			}
		}
		#endif
		// Processing: System.IDisposable
	}
}
