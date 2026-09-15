using System.Runtime.InteropServices.JavaScript;

namespace __Windows.Devices.Midi
{
	internal partial class MidiInPort
	{
		internal static partial class NativeMethods
		{
			private const string JsType = "globalThis.Windows.Devices.Midi.MidiInPort";

			[JSImport($"{JsType}.createPort")]
			internal static partial void CreatePort(string managedId, string id);

			[JSImport($"{JsType}.removePort")]
			internal static partial void RemovePort(string managedId);

			[JSImport($"{JsType}.startMessageListener")]
			internal static partial void StartMessageListener(string managedId);

			[JSImport($"{JsType}.stopMessageListener")]
			internal static partial void StopMessageListener(string managedId);
		}
	}
}
