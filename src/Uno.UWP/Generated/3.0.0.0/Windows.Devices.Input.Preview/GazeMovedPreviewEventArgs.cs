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
				throw new global::System.NotImplementedException("The member bool GazeMovedPreviewEventArgs.Handled is not implemented in Uno.");
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
				throw new global::System.NotImplementedException("The member GazePointPreview GazeMovedPreviewEventArgs.CurrentPoint is not implemented in Uno.");
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
			throw new global::System.NotImplementedException("The member IList<GazePointPreview> GazeMovedPreviewEventArgs.GetIntermediatePoints() is not implemented in Uno.");
		}
		#endif
	}
}
