#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Enumeration
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class DeviceInformation 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Enumeration.EnclosureLocation EnclosureLocation
		{
			get
			{
				throw new global::System.NotImplementedException("The member EnclosureLocation DeviceInformation.EnclosureLocation is not implemented in Uno.");
			}
		}
		#endif
		// Skipping already declared property Id
		// Skipping already declared property IsDefault
		// Skipping already declared property IsEnabled
		// Skipping already declared property Name
		// Skipping already declared property Properties
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Enumeration.DeviceInformationKind Kind
		{
			get
			{
				throw new global::System.NotImplementedException("The member DeviceInformationKind DeviceInformation.Kind is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Enumeration.DeviceInformationPairing Pairing
		{
			get
			{
				throw new global::System.NotImplementedException("The member DeviceInformationPairing DeviceInformation.Pairing is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Enumeration.DeviceInformation.Id.get
		// Forced skipping of method Windows.Devices.Enumeration.DeviceInformation.Name.get
		// Forced skipping of method Windows.Devices.Enumeration.DeviceInformation.IsEnabled.get
		// Forced skipping of method Windows.Devices.Enumeration.DeviceInformation.IsDefault.get
		// Forced skipping of method Windows.Devices.Enumeration.DeviceInformation.EnclosureLocation.get
		// Forced skipping of method Windows.Devices.Enumeration.DeviceInformation.Properties.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Update( global::Windows.Devices.Enumeration.DeviceInformationUpdate updateInfo)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Enumeration.DeviceInformation", "void DeviceInformation.Update(DeviceInformationUpdate updateInfo)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Enumeration.DeviceThumbnail> GetThumbnailAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<DeviceThumbnail> DeviceInformation.GetThumbnailAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Enumeration.DeviceThumbnail> GetGlyphThumbnailAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<DeviceThumbnail> DeviceInformation.GetGlyphThumbnailAsync() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Devices.Enumeration.DeviceInformation.Kind.get
		// Forced skipping of method Windows.Devices.Enumeration.DeviceInformation.Pairing.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string GetAqsFilterFromDeviceClass( global::Windows.Devices.Enumeration.DeviceClass deviceClass)
		{
			throw new global::System.NotImplementedException("The member string DeviceInformation.GetAqsFilterFromDeviceClass(DeviceClass deviceClass) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Enumeration.DeviceInformation> CreateFromIdAsync( string deviceId,  global::System.Collections.Generic.IEnumerable<string> additionalProperties,  global::Windows.Devices.Enumeration.DeviceInformationKind kind)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<DeviceInformation> DeviceInformation.CreateFromIdAsync(string deviceId, IEnumerable<string> additionalProperties, DeviceInformationKind kind) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Enumeration.DeviceInformationCollection> FindAllAsync( string aqsFilter,  global::System.Collections.Generic.IEnumerable<string> additionalProperties,  global::Windows.Devices.Enumeration.DeviceInformationKind kind)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<DeviceInformationCollection> DeviceInformation.FindAllAsync(string aqsFilter, IEnumerable<string> additionalProperties, DeviceInformationKind kind) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Devices.Enumeration.DeviceWatcher CreateWatcher( string aqsFilter,  global::System.Collections.Generic.IEnumerable<string> additionalProperties,  global::Windows.Devices.Enumeration.DeviceInformationKind kind)
		{
			throw new global::System.NotImplementedException("The member DeviceWatcher DeviceInformation.CreateWatcher(string aqsFilter, IEnumerable<string> additionalProperties, DeviceInformationKind kind) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Enumeration.DeviceInformation> CreateFromIdAsync( string deviceId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<DeviceInformation> DeviceInformation.CreateFromIdAsync(string deviceId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Enumeration.DeviceInformation> CreateFromIdAsync( string deviceId,  global::System.Collections.Generic.IEnumerable<string> additionalProperties)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<DeviceInformation> DeviceInformation.CreateFromIdAsync(string deviceId, IEnumerable<string> additionalProperties) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Enumeration.DeviceInformationCollection> FindAllAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<DeviceInformationCollection> DeviceInformation.FindAllAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Enumeration.DeviceInformationCollection> FindAllAsync( global::Windows.Devices.Enumeration.DeviceClass deviceClass)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<DeviceInformationCollection> DeviceInformation.FindAllAsync(DeviceClass deviceClass) is not implemented in Uno.");
		}
		#endif
		// Skipping already declared method Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(string)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Enumeration.DeviceInformationCollection> FindAllAsync( string aqsFilter,  global::System.Collections.Generic.IEnumerable<string> additionalProperties)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<DeviceInformationCollection> DeviceInformation.FindAllAsync(string aqsFilter, IEnumerable<string> additionalProperties) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Devices.Enumeration.DeviceWatcher CreateWatcher()
		{
			throw new global::System.NotImplementedException("The member DeviceWatcher DeviceInformation.CreateWatcher() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Devices.Enumeration.DeviceWatcher CreateWatcher( global::Windows.Devices.Enumeration.DeviceClass deviceClass)
		{
			throw new global::System.NotImplementedException("The member DeviceWatcher DeviceInformation.CreateWatcher(DeviceClass deviceClass) is not implemented in Uno.");
		}
		#endif
		// Skipping already declared method Windows.Devices.Enumeration.DeviceInformation.CreateWatcher(string)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Devices.Enumeration.DeviceWatcher CreateWatcher( string aqsFilter,  global::System.Collections.Generic.IEnumerable<string> additionalProperties)
		{
			throw new global::System.NotImplementedException("The member DeviceWatcher DeviceInformation.CreateWatcher(string aqsFilter, IEnumerable<string> additionalProperties) is not implemented in Uno.");
		}
		#endif
	}
}
