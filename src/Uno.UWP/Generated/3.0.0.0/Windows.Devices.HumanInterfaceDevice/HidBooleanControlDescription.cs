#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.HumanInterfaceDevice
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class HidBooleanControlDescription 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  uint Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint HidBooleanControlDescription.Id is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.HumanInterfaceDevice.HidCollection> ParentCollections
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<HidCollection> HidBooleanControlDescription.ParentCollections is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  ushort ReportId
		{
			get
			{
				throw new global::System.NotImplementedException("The member ushort HidBooleanControlDescription.ReportId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.HumanInterfaceDevice.HidReportType ReportType
		{
			get
			{
				throw new global::System.NotImplementedException("The member HidReportType HidBooleanControlDescription.ReportType is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  ushort UsageId
		{
			get
			{
				throw new global::System.NotImplementedException("The member ushort HidBooleanControlDescription.UsageId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  ushort UsagePage
		{
			get
			{
				throw new global::System.NotImplementedException("The member ushort HidBooleanControlDescription.UsagePage is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsAbsolute
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool HidBooleanControlDescription.IsAbsolute is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.HumanInterfaceDevice.HidBooleanControlDescription.Id.get
		// Forced skipping of method Windows.Devices.HumanInterfaceDevice.HidBooleanControlDescription.ReportId.get
		// Forced skipping of method Windows.Devices.HumanInterfaceDevice.HidBooleanControlDescription.ReportType.get
		// Forced skipping of method Windows.Devices.HumanInterfaceDevice.HidBooleanControlDescription.UsagePage.get
		// Forced skipping of method Windows.Devices.HumanInterfaceDevice.HidBooleanControlDescription.UsageId.get
		// Forced skipping of method Windows.Devices.HumanInterfaceDevice.HidBooleanControlDescription.ParentCollections.get
		// Forced skipping of method Windows.Devices.HumanInterfaceDevice.HidBooleanControlDescription.IsAbsolute.get
	}
}
