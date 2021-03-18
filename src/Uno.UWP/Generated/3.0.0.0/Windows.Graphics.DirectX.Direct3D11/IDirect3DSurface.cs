#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.DirectX.Direct3D11
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IDirect3DSurface : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Graphics.DirectX.Direct3D11.Direct3DSurfaceDescription Description
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Graphics.DirectX.Direct3D11.IDirect3DSurface.Description.get
	}
}
