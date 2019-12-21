#if __ANDROID__ || __IOS__
using Uno.Devices.Enumeration.Internal;

namespace Windows.Devices.Midi
{
	public partial class MidiInPort : global::System.IDisposable
	{
		private const string MidiInAqsFilter =
			"System.Devices.InterfaceClassGuid:=\"{" + DeviceClassGuids.MidiIn + "}\" AND System.Devices.InterfaceEnabled:=System.StructuredQueryType.Boolean#True";

		public static string GetDeviceSelector() => MidiInAqsFilter;
	}
}
#endif
