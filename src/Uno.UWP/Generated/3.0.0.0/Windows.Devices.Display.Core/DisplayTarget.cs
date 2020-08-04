#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Display.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DisplayTarget 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Display.Core.DisplayAdapter Adapter
		{
			get
			{
				throw new global::System.NotImplementedException("The member DisplayAdapter DisplayTarget.Adapter is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint AdapterRelativeId
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint DisplayTarget.AdapterRelativeId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DeviceInterfacePath
		{
			get
			{
				throw new global::System.NotImplementedException("The member string DisplayTarget.DeviceInterfacePath is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsConnected
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool DisplayTarget.IsConnected is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsStale
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool DisplayTarget.IsStale is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsVirtualModeEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool DisplayTarget.IsVirtualModeEnabled is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsVirtualTopologyEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool DisplayTarget.IsVirtualTopologyEnabled is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Display.Core.DisplayTargetPersistence MonitorPersistence
		{
			get
			{
				throw new global::System.NotImplementedException("The member DisplayTargetPersistence DisplayTarget.MonitorPersistence is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyDictionary<global::System.Guid, object> Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyDictionary<Guid, object> DisplayTarget.Properties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string StableMonitorId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string DisplayTarget.StableMonitorId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Display.DisplayMonitorUsageKind UsageKind
		{
			get
			{
				throw new global::System.NotImplementedException("The member DisplayMonitorUsageKind DisplayTarget.UsageKind is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Display.Core.DisplayTarget.Adapter.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayTarget.DeviceInterfacePath.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayTarget.AdapterRelativeId.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayTarget.IsConnected.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayTarget.IsVirtualModeEnabled.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayTarget.IsVirtualTopologyEnabled.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayTarget.UsageKind.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayTarget.MonitorPersistence.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayTarget.StableMonitorId.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Display.DisplayMonitor TryGetMonitor()
		{
			throw new global::System.NotImplementedException("The member DisplayMonitor DisplayTarget.TryGetMonitor() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Devices.Display.Core.DisplayTarget.Properties.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayTarget.IsStale.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsSame( global::Windows.Devices.Display.Core.DisplayTarget otherTarget)
		{
			throw new global::System.NotImplementedException("The member bool DisplayTarget.IsSame(DisplayTarget otherTarget) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsEqual( global::Windows.Devices.Display.Core.DisplayTarget otherTarget)
		{
			throw new global::System.NotImplementedException("The member bool DisplayTarget.IsEqual(DisplayTarget otherTarget) is not implemented in Uno.");
		}
		#endif
	}
}
