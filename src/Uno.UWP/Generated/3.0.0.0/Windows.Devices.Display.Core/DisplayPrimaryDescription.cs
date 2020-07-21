#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Display.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DisplayPrimaryDescription 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.DirectX.DirectXColorSpace ColorSpace
		{
			get
			{
				throw new global::System.NotImplementedException("The member DirectXColorSpace DisplayPrimaryDescription.ColorSpace is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.DirectX.DirectXPixelFormat Format
		{
			get
			{
				throw new global::System.NotImplementedException("The member DirectXPixelFormat DisplayPrimaryDescription.Format is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Height
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint DisplayPrimaryDescription.Height is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsStereo
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool DisplayPrimaryDescription.IsStereo is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.DirectX.Direct3D11.Direct3DMultisampleDescription MultisampleDescription
		{
			get
			{
				throw new global::System.NotImplementedException("The member Direct3DMultisampleDescription DisplayPrimaryDescription.MultisampleDescription is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyDictionary<global::System.Guid, object> Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyDictionary<Guid, object> DisplayPrimaryDescription.Properties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Width
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint DisplayPrimaryDescription.Width is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public DisplayPrimaryDescription( uint width,  uint height,  global::Windows.Graphics.DirectX.DirectXPixelFormat pixelFormat,  global::Windows.Graphics.DirectX.DirectXColorSpace colorSpace,  bool isStereo,  global::Windows.Graphics.DirectX.Direct3D11.Direct3DMultisampleDescription multisampleDescription) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Display.Core.DisplayPrimaryDescription", "DisplayPrimaryDescription.DisplayPrimaryDescription(uint width, uint height, DirectXPixelFormat pixelFormat, DirectXColorSpace colorSpace, bool isStereo, Direct3DMultisampleDescription multisampleDescription)");
		}
		#endif
		// Forced skipping of method Windows.Devices.Display.Core.DisplayPrimaryDescription.DisplayPrimaryDescription(uint, uint, Windows.Graphics.DirectX.DirectXPixelFormat, Windows.Graphics.DirectX.DirectXColorSpace, bool, Windows.Graphics.DirectX.Direct3D11.Direct3DMultisampleDescription)
		// Forced skipping of method Windows.Devices.Display.Core.DisplayPrimaryDescription.Width.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayPrimaryDescription.Height.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayPrimaryDescription.Format.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayPrimaryDescription.ColorSpace.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayPrimaryDescription.IsStereo.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayPrimaryDescription.MultisampleDescription.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayPrimaryDescription.Properties.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Devices.Display.Core.DisplayPrimaryDescription CreateWithProperties( global::System.Collections.Generic.IEnumerable<global::System.Collections.Generic.KeyValuePair<global::System.Guid, object>> extraProperties,  uint width,  uint height,  global::Windows.Graphics.DirectX.DirectXPixelFormat pixelFormat,  global::Windows.Graphics.DirectX.DirectXColorSpace colorSpace,  bool isStereo,  global::Windows.Graphics.DirectX.Direct3D11.Direct3DMultisampleDescription multisampleDescription)
		{
			throw new global::System.NotImplementedException("The member DisplayPrimaryDescription DisplayPrimaryDescription.CreateWithProperties(IEnumerable<KeyValuePair<Guid, object>> extraProperties, uint width, uint height, DirectXPixelFormat pixelFormat, DirectXColorSpace colorSpace, bool isStereo, Direct3DMultisampleDescription multisampleDescription) is not implemented in Uno.");
		}
		#endif
	}
}
