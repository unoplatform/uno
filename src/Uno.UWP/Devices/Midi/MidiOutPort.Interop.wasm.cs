using System.Runtime.InteropServices.JavaScript;

namespace __Windows.Devices.Midi
{
	internal partial class MidiOutPort
	{
		internal static partial class NativeMethods
		{
			[JSImport("globalThis.Windows.Devices.Midi.MidiOutPort.sendBuffer")]
			internal static partial void SendBuffer(string id, double timestamp, byte[] data);
		}
	}
}
