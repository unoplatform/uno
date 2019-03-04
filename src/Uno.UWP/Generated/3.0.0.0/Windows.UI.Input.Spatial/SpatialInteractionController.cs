#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input.Spatial
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SpatialInteractionController 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool HasThumbstick
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool SpatialInteractionController.HasThumbstick is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool HasTouchpad
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool SpatialInteractionController.HasTouchpad is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  ushort ProductId
		{
			get
			{
				throw new global::System.NotImplementedException("The member ushort SpatialInteractionController.ProductId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.Haptics.SimpleHapticsController SimpleHapticsController
		{
			get
			{
				throw new global::System.NotImplementedException("The member SimpleHapticsController SpatialInteractionController.SimpleHapticsController is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  ushort VendorId
		{
			get
			{
				throw new global::System.NotImplementedException("The member ushort SpatialInteractionController.VendorId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  ushort Version
		{
			get
			{
				throw new global::System.NotImplementedException("The member ushort SpatialInteractionController.Version is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Input.Spatial.SpatialInteractionController.HasTouchpad.get
		// Forced skipping of method Windows.UI.Input.Spatial.SpatialInteractionController.HasThumbstick.get
		// Forced skipping of method Windows.UI.Input.Spatial.SpatialInteractionController.SimpleHapticsController.get
		// Forced skipping of method Windows.UI.Input.Spatial.SpatialInteractionController.VendorId.get
		// Forced skipping of method Windows.UI.Input.Spatial.SpatialInteractionController.ProductId.get
		// Forced skipping of method Windows.UI.Input.Spatial.SpatialInteractionController.Version.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IRandomAccessStreamWithContentType> TryGetRenderableModelAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IRandomAccessStreamWithContentType> SpatialInteractionController.TryGetRenderableModelAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.Power.BatteryReport TryGetBatteryReport()
		{
			throw new global::System.NotImplementedException("The member BatteryReport SpatialInteractionController.TryGetBatteryReport() is not implemented in Uno.");
		}
		#endif
	}
}
