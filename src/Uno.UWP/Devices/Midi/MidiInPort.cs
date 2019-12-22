#if __ANDROID__ || __IOS__
using Uno.Devices.Enumeration.Internal;

namespace Windows.Devices.Midi
{
	public partial class MidiInPort : global::System.IDisposable
	{
		private const string MidiInAqsFilter =
			"System.Devices.InterfaceClassGuid:=\"{" + DeviceClassGuids.MidiIn + "}\" AND System.Devices.InterfaceEnabled:=System.StructuredQueryType.Boolean#True";

		public static string GetDeviceSelector() => MidiInAqsFilter;

		public void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Midi.MidiInPort", "void MidiInPort.Dispose()");
		}

		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Midi.MidiInPort> FromIdAsync(string deviceId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MidiInPort> MidiInPort.FromIdAsync(string deviceId) is not implemented in Uno.");
		}
	}
}
#endif
