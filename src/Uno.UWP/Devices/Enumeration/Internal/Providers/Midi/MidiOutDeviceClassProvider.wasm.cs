#if __WASM__

namespace Uno.Devices.Enumeration.Internal.Providers.Midi
{
	internal class MidiOutDeviceClassProvider : MidiDeviceClassProviderBase
	{
		public MidiOutDeviceClassProvider() : base(false)
		{
		}
	}
}
#endif
