#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Input.Preview
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GazeMovedPreviewEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Handled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool GazeMovedPreviewEventArgs.Handled is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20GazeMovedPreviewEventArgs.Handled");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Input.Preview.GazeMovedPreviewEventArgs", "bool GazeMovedPreviewEventArgs.Handled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Input.Preview.GazePointPreview CurrentPoint
		{
			get
			{
				throw new global::System.NotImplementedException("The member GazePointPreview GazeMovedPreviewEventArgs.CurrentPoint is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=GazePointPreview%20GazeMovedPreviewEventArgs.CurrentPoint");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Input.Preview.GazeMovedPreviewEventArgs.Handled.get
		// Forced skipping of method Windows.Devices.Input.Preview.GazeMovedPreviewEventArgs.Handled.set
		// Forced skipping of method Windows.Devices.Input.Preview.GazeMovedPreviewEventArgs.CurrentPoint.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.Devices.Input.Preview.GazePointPreview> GetIntermediatePoints()
		{
			throw new global::System.NotImplementedException("The member IList<GazePointPreview> GazeMovedPreviewEventArgs.GetIntermediatePoints() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IList%3CGazePointPreview%3E%20GazeMovedPreviewEventArgs.GetIntermediatePoints%28%29");
		}
		#endif
	}
}
