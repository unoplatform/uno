#if __WASM__

namespace Uno.Devices.Enumeration.Internal.Providers.Midi
{
	internal class MidiInDeviceClassProvider : MidiDeviceClassProviderBase
	{
		public MidiInDeviceClassProvider() : base(true)
		{
		}
	}
}
#endif
