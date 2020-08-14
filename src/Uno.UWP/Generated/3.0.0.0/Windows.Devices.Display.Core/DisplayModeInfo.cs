#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Display.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DisplayModeInfo 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsInterlaced
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool DisplayModeInfo.IsInterlaced is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsStereo
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool DisplayModeInfo.IsStereo is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Display.Core.DisplayPresentationRate PresentationRate
		{
			get
			{
				throw new global::System.NotImplementedException("The member DisplayPresentationRate DisplayModeInfo.PresentationRate is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyDictionary<global::System.Guid, object> Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyDictionary<Guid, object> DisplayModeInfo.Properties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.DirectX.DirectXPixelFormat SourcePixelFormat
		{
			get
			{
				throw new global::System.NotImplementedException("The member DirectXPixelFormat DisplayModeInfo.SourcePixelFormat is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.SizeInt32 SourceResolution
		{
			get
			{
				throw new global::System.NotImplementedException("The member SizeInt32 DisplayModeInfo.SourceResolution is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.SizeInt32 TargetResolution
		{
			get
			{
				throw new global::System.NotImplementedException("The member SizeInt32 DisplayModeInfo.TargetResolution is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Display.Core.DisplayModeInfo.SourceResolution.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayModeInfo.IsStereo.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayModeInfo.SourcePixelFormat.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayModeInfo.TargetResolution.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayModeInfo.PresentationRate.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayModeInfo.IsInterlaced.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Display.Core.DisplayBitsPerChannel GetWireFormatSupportedBitsPerChannel( global::Windows.Devices.Display.Core.DisplayWireFormatPixelEncoding encoding)
		{
			throw new global::System.NotImplementedException("The member DisplayBitsPerChannel DisplayModeInfo.GetWireFormatSupportedBitsPerChannel(DisplayWireFormatPixelEncoding encoding) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsWireFormatSupported( global::Windows.Devices.Display.Core.DisplayWireFormat wireFormat)
		{
			throw new global::System.NotImplementedException("The member bool DisplayModeInfo.IsWireFormatSupported(DisplayWireFormat wireFormat) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Devices.Display.Core.DisplayModeInfo.Properties.get
	}
}
