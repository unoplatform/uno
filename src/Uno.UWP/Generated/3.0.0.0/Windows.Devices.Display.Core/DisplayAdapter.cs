#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Display.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DisplayAdapter 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DeviceInterfacePath
		{
			get
			{
				throw new global::System.NotImplementedException("The member string DisplayAdapter.DeviceInterfacePath is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.DisplayAdapterId Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member DisplayAdapterId DisplayAdapter.Id is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint PciDeviceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint DisplayAdapter.PciDeviceId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint PciRevision
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint DisplayAdapter.PciRevision is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint PciSubSystemId
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint DisplayAdapter.PciSubSystemId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint PciVendorId
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint DisplayAdapter.PciVendorId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyDictionary<global::System.Guid, object> Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyDictionary<Guid, object> DisplayAdapter.Properties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint SourceCount
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint DisplayAdapter.SourceCount is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Display.Core.DisplayAdapter.Id.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayAdapter.DeviceInterfacePath.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayAdapter.SourceCount.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayAdapter.PciVendorId.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayAdapter.PciDeviceId.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayAdapter.PciSubSystemId.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayAdapter.PciRevision.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayAdapter.Properties.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Devices.Display.Core.DisplayAdapter FromId( global::Windows.Graphics.DisplayAdapterId id)
		{
			throw new global::System.NotImplementedException("The member DisplayAdapter DisplayAdapter.FromId(DisplayAdapterId id) is not implemented in Uno.");
		}
		#endif
	}
}
