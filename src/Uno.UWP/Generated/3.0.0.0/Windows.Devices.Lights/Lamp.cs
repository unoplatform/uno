#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Lights
{
	#if false || false || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class Lamp : global::System.IDisposable
	{
		#if false || false || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool Lamp.IsEnabled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Lights.Lamp", "bool Lamp.IsEnabled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Color Color
		{
			get
			{
				throw new global::System.NotImplementedException("The member Color Lamp.Color is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Lights.Lamp", "Color Lamp.Color");
			}
		}
		#endif
		#if false || false || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  float BrightnessLevel
		{
			get
			{
				throw new global::System.NotImplementedException("The member float Lamp.BrightnessLevel is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Lights.Lamp", "float Lamp.BrightnessLevel");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string DeviceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string Lamp.DeviceId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsColorSettable
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool Lamp.IsColorSettable is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Lights.Lamp.DeviceId.get
		// Forced skipping of method Windows.Devices.Lights.Lamp.IsEnabled.get
		// Forced skipping of method Windows.Devices.Lights.Lamp.IsEnabled.set
		// Forced skipping of method Windows.Devices.Lights.Lamp.BrightnessLevel.get
		// Forced skipping of method Windows.Devices.Lights.Lamp.BrightnessLevel.set
		// Forced skipping of method Windows.Devices.Lights.Lamp.IsColorSettable.get
		// Forced skipping of method Windows.Devices.Lights.Lamp.Color.get
		// Forced skipping of method Windows.Devices.Lights.Lamp.Color.set
		// Forced skipping of method Windows.Devices.Lights.Lamp.AvailabilityChanged.add
		// Forced skipping of method Windows.Devices.Lights.Lamp.AvailabilityChanged.remove
		#if false || false || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Lights.Lamp", "void Lamp.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static string GetDeviceSelector()
		{
			throw new global::System.NotImplementedException("The member string Lamp.GetDeviceSelector() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Lights.Lamp> FromIdAsync( string deviceId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<Lamp> Lamp.FromIdAsync(string deviceId) is not implemented in Uno.");
		}
		#endif
		#if false || false || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Lights.Lamp> GetDefaultAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<Lamp> Lamp.GetDefaultAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Lights.Lamp, global::Windows.Devices.Lights.LampAvailabilityChangedEventArgs> AvailabilityChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Lights.Lamp", "event TypedEventHandler<Lamp, LampAvailabilityChangedEventArgs> Lamp.AvailabilityChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Lights.Lamp", "event TypedEventHandler<Lamp, LampAvailabilityChangedEventArgs> Lamp.AvailabilityChanged");
			}
		}
		#endif
		// Processing: System.IDisposable
	}
}
