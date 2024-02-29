using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Media.Midi;
using Windows.Devices.Midi;

namespace Uno.Devices.Midi.Internal
{
	internal class MidiDeviceOpenedListener : Java.Lang.Object, MidiManager.IOnDeviceOpenedListener
	{
		private readonly TaskCompletionSource<MidiDevice> _taskCompletionSource;

		public MidiDeviceOpenedListener(TaskCompletionSource<MidiDevice> taskCompletionSource)
		{
			_taskCompletionSource = taskCompletionSource;
		}

		public void OnDeviceOpened(MidiDevice? device)
		{
			if (device == null)
			{
				throw new ArgumentNullException(nameof(device));
			}

			_taskCompletionSource.SetResult(device);
		}
	}
}
