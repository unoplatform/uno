#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Display
{
	#if false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class DisplayInformation 
	{
		// Skipping already declared property CurrentOrientation
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  float LogicalDpi
		{
			get
			{
				throw new global::System.NotImplementedException("The member float DisplayInformation.LogicalDpi is not implemented in Uno.");
			}
		}
		#endif
		// Skipping already declared property NativeOrientation
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  float RawDpiX
		{
			get
			{
				throw new global::System.NotImplementedException("The member float DisplayInformation.RawDpiX is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  float RawDpiY
		{
			get
			{
				throw new global::System.NotImplementedException("The member float DisplayInformation.RawDpiY is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Graphics.Display.ResolutionScale ResolutionScale
		{
			get
			{
				throw new global::System.NotImplementedException("The member ResolutionScale DisplayInformation.ResolutionScale is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool StereoEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool DisplayInformation.StereoEnabled is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  double RawPixelsPerViewPixel
		{
			get
			{
				throw new global::System.NotImplementedException("The member double DisplayInformation.RawPixelsPerViewPixel is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  double? DiagonalSizeInInches
		{
			get
			{
				throw new global::System.NotImplementedException("The member double? DisplayInformation.DiagonalSizeInInches is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  uint ScreenHeightInRawPixels
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint DisplayInformation.ScreenHeightInRawPixels is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  uint ScreenWidthInRawPixels
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint DisplayInformation.ScreenWidthInRawPixels is not implemented in Uno.");
			}
		}
		#endif
		// Skipping already declared property AutoRotationPreferences
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.CurrentOrientation.get
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.NativeOrientation.get
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.OrientationChanged.add
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.OrientationChanged.remove
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.ResolutionScale.get
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.LogicalDpi.get
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.RawDpiX.get
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.RawDpiY.get
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.DpiChanged.add
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.DpiChanged.remove
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.StereoEnabled.get
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.StereoEnabledChanged.add
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.StereoEnabledChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IRandomAccessStream> GetColorProfileAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IRandomAccessStream> DisplayInformation.GetColorProfileAsync() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.ColorProfileChanged.add
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.ColorProfileChanged.remove
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.RawPixelsPerViewPixel.get
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.DiagonalSizeInInches.get
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.ScreenWidthInRawPixels.get
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.ScreenHeightInRawPixels.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Graphics.Display.AdvancedColorInfo GetAdvancedColorInfo()
		{
			throw new global::System.NotImplementedException("The member AdvancedColorInfo DisplayInformation.GetAdvancedColorInfo() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.AdvancedColorInfoChanged.add
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.AdvancedColorInfoChanged.remove
		// Skipping already declared method Windows.Graphics.Display.DisplayInformation.GetForCurrentView()
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.AutoRotationPreferences.get
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.AutoRotationPreferences.set
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.DisplayContentsInvalidated.add
		// Forced skipping of method Windows.Graphics.Display.DisplayInformation.DisplayContentsInvalidated.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Graphics.Display.DisplayInformation, object> ColorProfileChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.DisplayInformation", "event TypedEventHandler<DisplayInformation, object> DisplayInformation.ColorProfileChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.DisplayInformation", "event TypedEventHandler<DisplayInformation, object> DisplayInformation.ColorProfileChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Graphics.Display.DisplayInformation, object> DpiChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.DisplayInformation", "event TypedEventHandler<DisplayInformation, object> DisplayInformation.DpiChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.DisplayInformation", "event TypedEventHandler<DisplayInformation, object> DisplayInformation.DpiChanged");
			}
		}
		#endif
		// Skipping already declared event Windows.Graphics.Display.DisplayInformation.OrientationChanged
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Graphics.Display.DisplayInformation, object> StereoEnabledChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.DisplayInformation", "event TypedEventHandler<DisplayInformation, object> DisplayInformation.StereoEnabledChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.DisplayInformation", "event TypedEventHandler<DisplayInformation, object> DisplayInformation.StereoEnabledChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Graphics.Display.DisplayInformation, object> AdvancedColorInfoChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.DisplayInformation", "event TypedEventHandler<DisplayInformation, object> DisplayInformation.AdvancedColorInfoChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.DisplayInformation", "event TypedEventHandler<DisplayInformation, object> DisplayInformation.AdvancedColorInfoChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static event global::Windows.Foundation.TypedEventHandler<global::Windows.Graphics.Display.DisplayInformation, object> DisplayContentsInvalidated
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.DisplayInformation", "event TypedEventHandler<DisplayInformation, object> DisplayInformation.DisplayContentsInvalidated");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Display.DisplayInformation", "event TypedEventHandler<DisplayInformation, object> DisplayInformation.DisplayContentsInvalidated");
			}
		}
		#endif
	}
}
