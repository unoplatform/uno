#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Printers
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class IppResolution 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int Height
		{
			get
			{
				throw new global::System.NotImplementedException("The member int IppResolution.Height is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=int%20IppResolution.Height");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Printers.IppResolutionUnit Unit
		{
			get
			{
				throw new global::System.NotImplementedException("The member IppResolutionUnit IppResolution.Unit is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IppResolutionUnit%20IppResolution.Unit");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int Width
		{
			get
			{
				throw new global::System.NotImplementedException("The member int IppResolution.Width is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=int%20IppResolution.Width");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public IppResolution( int width,  int height,  global::Windows.Devices.Printers.IppResolutionUnit unit) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Printers.IppResolution", "IppResolution.IppResolution(int width, int height, IppResolutionUnit unit)");
		}
		#endif
		// Forced skipping of method Windows.Devices.Printers.IppResolution.IppResolution(int, int, Windows.Devices.Printers.IppResolutionUnit)
		// Forced skipping of method Windows.Devices.Printers.IppResolution.Width.get
		// Forced skipping of method Windows.Devices.Printers.IppResolution.Height.get
		// Forced skipping of method Windows.Devices.Printers.IppResolution.Unit.get
	}
}
