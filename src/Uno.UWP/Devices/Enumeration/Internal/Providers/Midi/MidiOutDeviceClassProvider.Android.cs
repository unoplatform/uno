#if __ANDROID__
using Android.Media.Midi;

namespace Uno.Devices.Enumeration.Internal.Providers.Midi
{
	internal class MidiOutDeviceClassProvider : MidiDeviceClassProviderBase
	{
		public MidiOutDeviceClassProvider() : base(MidiPortType.Output)
		{
		}
	}
}
#endif
