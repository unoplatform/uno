#if __ANDROID__
using Android.Media.Midi;

namespace Uno.Devices.Enumeration.Internal.Providers.Midi
{
	internal class MidiInDeviceClassProvider : MidiDeviceClassProviderBase
	{
		public MidiInDeviceClassProvider() : base(MidiPortType.Input)
		{
		}
	}
}
#endif
