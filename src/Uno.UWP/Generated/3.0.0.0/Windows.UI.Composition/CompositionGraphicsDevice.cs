#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CompositionGraphicsDevice : global::Windows.UI.Composition.CompositionObject
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Composition.CompositionDrawingSurface CreateDrawingSurface( global::Windows.Foundation.Size sizePixels,  global::Windows.Graphics.DirectX.DirectXPixelFormat pixelFormat,  global::Windows.Graphics.DirectX.DirectXAlphaMode alphaMode)
		{
			throw new global::System.NotImplementedException("The member CompositionDrawingSurface CompositionGraphicsDevice.CreateDrawingSurface(Size sizePixels, DirectXPixelFormat pixelFormat, DirectXAlphaMode alphaMode) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Composition.CompositionGraphicsDevice.RenderingDeviceReplaced.add
		// Forced skipping of method Windows.UI.Composition.CompositionGraphicsDevice.RenderingDeviceReplaced.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Composition.CompositionDrawingSurface CreateDrawingSurface2( global::Windows.Graphics.SizeInt32 sizePixels,  global::Windows.Graphics.DirectX.DirectXPixelFormat pixelFormat,  global::Windows.Graphics.DirectX.DirectXAlphaMode alphaMode)
		{
			throw new global::System.NotImplementedException("The member CompositionDrawingSurface CompositionGraphicsDevice.CreateDrawingSurface2(SizeInt32 sizePixels, DirectXPixelFormat pixelFormat, DirectXAlphaMode alphaMode) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Composition.CompositionVirtualDrawingSurface CreateVirtualDrawingSurface( global::Windows.Graphics.SizeInt32 sizePixels,  global::Windows.Graphics.DirectX.DirectXPixelFormat pixelFormat,  global::Windows.Graphics.DirectX.DirectXAlphaMode alphaMode)
		{
			throw new global::System.NotImplementedException("The member CompositionVirtualDrawingSurface CompositionGraphicsDevice.CreateVirtualDrawingSurface(SizeInt32 sizePixels, DirectXPixelFormat pixelFormat, DirectXAlphaMode alphaMode) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Composition.CompositionMipmapSurface CreateMipmapSurface( global::Windows.Graphics.SizeInt32 sizePixels,  global::Windows.Graphics.DirectX.DirectXPixelFormat pixelFormat,  global::Windows.Graphics.DirectX.DirectXAlphaMode alphaMode)
		{
			throw new global::System.NotImplementedException("The member CompositionMipmapSurface CompositionGraphicsDevice.CreateMipmapSurface(SizeInt32 sizePixels, DirectXPixelFormat pixelFormat, DirectXAlphaMode alphaMode) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Trim()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.CompositionGraphicsDevice", "void CompositionGraphicsDevice.Trim()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Composition.CompositionGraphicsDevice, global::Windows.UI.Composition.RenderingDeviceReplacedEventArgs> RenderingDeviceReplaced
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.CompositionGraphicsDevice", "event TypedEventHandler<CompositionGraphicsDevice, RenderingDeviceReplacedEventArgs> CompositionGraphicsDevice.RenderingDeviceReplaced");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.CompositionGraphicsDevice", "event TypedEventHandler<CompositionGraphicsDevice, RenderingDeviceReplacedEventArgs> CompositionGraphicsDevice.RenderingDeviceReplaced");
			}
		}
		#endif
	}
}
