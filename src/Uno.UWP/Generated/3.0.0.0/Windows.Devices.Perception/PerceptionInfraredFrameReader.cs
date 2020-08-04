#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Perception
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PerceptionInfraredFrameReader : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsPaused
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PerceptionInfraredFrameReader.IsPaused is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Perception.PerceptionInfraredFrameReader", "bool PerceptionInfraredFrameReader.IsPaused");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Perception.PerceptionInfraredFrameSource Source
		{
			get
			{
				throw new global::System.NotImplementedException("The member PerceptionInfraredFrameSource PerceptionInfraredFrameReader.Source is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Perception.PerceptionInfraredFrameReader.FrameArrived.add
		// Forced skipping of method Windows.Devices.Perception.PerceptionInfraredFrameReader.FrameArrived.remove
		// Forced skipping of method Windows.Devices.Perception.PerceptionInfraredFrameReader.Source.get
		// Forced skipping of method Windows.Devices.Perception.PerceptionInfraredFrameReader.IsPaused.get
		// Forced skipping of method Windows.Devices.Perception.PerceptionInfraredFrameReader.IsPaused.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Perception.PerceptionInfraredFrame TryReadLatestFrame()
		{
			throw new global::System.NotImplementedException("The member PerceptionInfraredFrame PerceptionInfraredFrameReader.TryReadLatestFrame() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Perception.PerceptionInfraredFrameReader", "void PerceptionInfraredFrameReader.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Perception.PerceptionInfraredFrameReader, global::Windows.Devices.Perception.PerceptionInfraredFrameArrivedEventArgs> FrameArrived
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Perception.PerceptionInfraredFrameReader", "event TypedEventHandler<PerceptionInfraredFrameReader, PerceptionInfraredFrameArrivedEventArgs> PerceptionInfraredFrameReader.FrameArrived");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Perception.PerceptionInfraredFrameReader", "event TypedEventHandler<PerceptionInfraredFrameReader, PerceptionInfraredFrameArrivedEventArgs> PerceptionInfraredFrameReader.FrameArrived");
			}
		}
		#endif
		// Processing: System.IDisposable
	}
}
