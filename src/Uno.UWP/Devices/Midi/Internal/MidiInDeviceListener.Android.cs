#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Media.Midi;
using Windows.Devices.Midi;

namespace Uno.Devices.Midi.Internal
{
	internal class MidiInDeviceListener : Java.Lang.Object, MidiManager.IOnDeviceOpenedListener
	{
		AndroidMidiAccess parent;
		MidiDevice device;
		MidiPortDetails port_to_open;
		ManualResetEventSlim wait;

		public MidiInDeviceListener(MidiInPort parent, MidiDevice device, MidiDeviceInfo.PortInfo portToOpen)
		{
			if (parent == null)
				throw new ArgumentNullException(nameof(parent));
			if (portToOpen == null)
				throw new ArgumentNullException(nameof(portToOpen));
			this.parent = parent;
			this.device = device;
			port_to_open = portToOpen;
		}

		public Task<IMidiInput> OpenInputAsync(CancellationToken token)
		{
			// MidiInput takes Android.Media.Midi.MidiOutputPort because... Android.Media.Midi API sucks and MidiOutputPort represents a MIDI IN device(!!)
			return OpenAsync(token, dev => (IMidiInput)new MidiInput(port_to_open, dev.OpenOutputPort(port_to_open.Port.PortNumber)));
		}

		public Task<IMidiOutput> OpenOutputAsync(CancellationToken token)
		{
			// MidiOutput takes Android.Media.Midi.MidiInputPort because... Android.Media.Midi API sucks and MidiInputPort represents a MIDI OUT device(!!)
			return OpenAsync(token, dev => (IMidiOutput)new MidiOutput(port_to_open, dev.OpenInputPort(port_to_open.Port.PortNumber)));
		}

		Task<T> OpenAsync<T>(CancellationToken token, Func<MidiDevice, T> resultCreator)
		{
			return Task.Run(delegate {
				if (device == null)
				{
					wait = new ManualResetEventSlim();
					parent.midi_manager.OpenDevice(port_to_open.Device, this, null);
					wait.Wait(token);
					wait.Reset();
				}
				return resultCreator(device);
			});
		}

		public void OnDeviceOpened(MidiDevice device)
		{
			if (device == null)
				throw new ArgumentNullException(nameof(device));
			this.device = device;
			parent.open_devices.Add(device);
			wait.Set();
		}
	}
}
#endif
