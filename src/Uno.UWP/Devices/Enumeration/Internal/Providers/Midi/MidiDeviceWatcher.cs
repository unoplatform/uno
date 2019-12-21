#if __ANDROID__
using System;
using System.Linq;
using System.Threading.Tasks;
using Android.Content;
using Android.Media.Midi;
using Android.Runtime;
using Uno.UI;
using Windows.Devices.Enumeration;
using Windows.Foundation;

namespace Uno.Devices.Enumeration.Internal.Providers.Midi
{
	internal class MidiInDeviceClassProvider : IDeviceClassProvider
	{
		private MidiManager _midiManager;

		public MidiInDeviceClassProvider()
		{
		}

		public event TypedEventHandler<DeviceWatcher, DeviceInformation> Added;
		public event TypedEventHandler<DeviceWatcher, DeviceInformation> EnumerationCompleted;
		public event TypedEventHandler<DeviceWatcher, DeviceInformationUpdate> Removed;
		public event TypedEventHandler<DeviceWatcher, object> Stopped;
		public event TypedEventHandler<DeviceWatcher, DeviceInformationUpdate> Updated;
		public event EventHandler<DeviceInformation> WatchAdded;
		public event EventHandler<DeviceInformation> WatchEnumerationCompleted;
		public event EventHandler<DeviceInformationUpdate> WatchRemoved;
		public event EventHandler<object> WatchStopped;
		public event EventHandler<DeviceInformationUpdate> WatchUpdated;

		public Task<DeviceInformation> FindAllAsync()
		{
			throw new NotImplementedException();
		}

		public void WatchStart()
		{
			_midiManager = _midiManager ?? ContextHelper.Current.GetSystemService(Context.MidiService).JavaCast<MidiManager>();
		}

		public void WatchStop()
		{
			_midiManager?.Dispose();
			WatchStopped?.Invoke(this, null);
		}

		private MidiDeviceInfo[] GetMidiDevices()
		{
			return _midiManager
				.GetDevices()
				.ToArray();
		}
	}
}
#endif
