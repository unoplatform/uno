#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.HumanInterfaceDevice
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class HidInputReport 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.HumanInterfaceDevice.HidBooleanControl> ActivatedBooleanControls
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<HidBooleanControl> HidInputReport.ActivatedBooleanControls is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer Data
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer HidInputReport.Data is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ushort Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member ushort HidInputReport.Id is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.HumanInterfaceDevice.HidBooleanControl> TransitionedBooleanControls
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<HidBooleanControl> HidInputReport.TransitionedBooleanControls is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.HumanInterfaceDevice.HidInputReport.Id.get
		// Forced skipping of method Windows.Devices.HumanInterfaceDevice.HidInputReport.Data.get
		// Forced skipping of method Windows.Devices.HumanInterfaceDevice.HidInputReport.ActivatedBooleanControls.get
		// Forced skipping of method Windows.Devices.HumanInterfaceDevice.HidInputReport.TransitionedBooleanControls.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.HumanInterfaceDevice.HidBooleanControl GetBooleanControl( ushort usagePage,  ushort usageId)
		{
			throw new global::System.NotImplementedException("The member HidBooleanControl HidInputReport.GetBooleanControl(ushort usagePage, ushort usageId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.HumanInterfaceDevice.HidBooleanControl GetBooleanControlByDescription( global::Windows.Devices.HumanInterfaceDevice.HidBooleanControlDescription controlDescription)
		{
			throw new global::System.NotImplementedException("The member HidBooleanControl HidInputReport.GetBooleanControlByDescription(HidBooleanControlDescription controlDescription) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.HumanInterfaceDevice.HidNumericControl GetNumericControl( ushort usagePage,  ushort usageId)
		{
			throw new global::System.NotImplementedException("The member HidNumericControl HidInputReport.GetNumericControl(ushort usagePage, ushort usageId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.HumanInterfaceDevice.HidNumericControl GetNumericControlByDescription( global::Windows.Devices.HumanInterfaceDevice.HidNumericControlDescription controlDescription)
		{
			throw new global::System.NotImplementedException("The member HidNumericControl HidInputReport.GetNumericControlByDescription(HidNumericControlDescription controlDescription) is not implemented in Uno.");
		}
		#endif
	}
}
