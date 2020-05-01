using System;
using Uno.Devices.Enumeration.Internal;

namespace Windows.Devices.Midi
{
	public partial class MidiOutPort : IDisposable
	{
		private const string MidiOutAqsFilter =
			"System.Devices.InterfaceClassGuid:=\"{" + DeviceClassGuids.MidiOut + "}\" AND System.Devices.InterfaceEnabled:=System.StructuredQueryType.Boolean#True";

		public static string GetDeviceSelector() => MidiOutAqsFilter;
	}
}
