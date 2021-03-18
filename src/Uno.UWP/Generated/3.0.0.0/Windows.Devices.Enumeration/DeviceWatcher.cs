#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Enumeration
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class DeviceWatcher 
	{
		// Skipping already declared property Status
		// Forced skipping of method Windows.Devices.Enumeration.DeviceWatcher.Added.add
		// Forced skipping of method Windows.Devices.Enumeration.DeviceWatcher.Added.remove
		// Forced skipping of method Windows.Devices.Enumeration.DeviceWatcher.Updated.add
		// Forced skipping of method Windows.Devices.Enumeration.DeviceWatcher.Updated.remove
		// Forced skipping of method Windows.Devices.Enumeration.DeviceWatcher.Removed.add
		// Forced skipping of method Windows.Devices.Enumeration.DeviceWatcher.Removed.remove
		// Forced skipping of method Windows.Devices.Enumeration.DeviceWatcher.EnumerationCompleted.add
		// Forced skipping of method Windows.Devices.Enumeration.DeviceWatcher.EnumerationCompleted.remove
		// Forced skipping of method Windows.Devices.Enumeration.DeviceWatcher.Stopped.add
		// Forced skipping of method Windows.Devices.Enumeration.DeviceWatcher.Stopped.remove
		// Forced skipping of method Windows.Devices.Enumeration.DeviceWatcher.Status.get
		// Skipping already declared method Windows.Devices.Enumeration.DeviceWatcher.Start()
		// Skipping already declared method Windows.Devices.Enumeration.DeviceWatcher.Stop()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Background.DeviceWatcherTrigger GetBackgroundTrigger( global::System.Collections.Generic.IEnumerable<global::Windows.Devices.Enumeration.DeviceWatcherEventKind> requestedEventKinds)
		{
			throw new global::System.NotImplementedException("The member DeviceWatcherTrigger DeviceWatcher.GetBackgroundTrigger(IEnumerable<DeviceWatcherEventKind> requestedEventKinds) is not implemented in Uno.");
		}
		#endif
		// Skipping already declared event Windows.Devices.Enumeration.DeviceWatcher.Added
		// Skipping already declared event Windows.Devices.Enumeration.DeviceWatcher.EnumerationCompleted
		// Skipping already declared event Windows.Devices.Enumeration.DeviceWatcher.Removed
		// Skipping already declared event Windows.Devices.Enumeration.DeviceWatcher.Stopped
		// Skipping already declared event Windows.Devices.Enumeration.DeviceWatcher.Updated
	}
}
