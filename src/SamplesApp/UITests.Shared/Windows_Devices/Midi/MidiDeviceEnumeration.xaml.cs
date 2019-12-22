using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Android.Database;
using Uno.Disposables;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Devices.Enumeration;
using Windows.Devices.Midi;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_Devices.Midi
{
	[SampleControlInfo("Windows.Devices", "Midi_DeviceEnumeration", description: "Sample for enumeration of MIDI devices", viewModelType: typeof(MidiDeviceEnumerationTestsViewModel))]
	public sealed partial class MidiDeviceEnumerationTests : UserControl
	{
		public MidiDeviceEnumerationTests()
		{
			this.InitializeComponent();
		}
	}

	public class MidiDeviceEnumerationTestsViewModel : ViewModelBase
	{
		private readonly MidiDeviceWatcher _midiInDeviceWatcher;		
		private readonly MidiDeviceWatcher _midiOutDeviceWatcher;
		private int _selectedInputDeviceIndex = 0;

		public MidiDeviceEnumerationTestsViewModel(CoreDispatcher dispatcher) : base(dispatcher)
		{
			_midiInDeviceWatcher = new MidiDeviceWatcher(MidiInPort.GetDeviceSelector(), dispatcher, MidiInDevices);
			_midiOutDeviceWatcher = new MidiDeviceWatcher(MidiOutPort.GetDeviceSelector(), dispatcher, MidiOutDevices);

			_midiInDeviceWatcher.Start();
			_midiOutDeviceWatcher.Start();

			Disposables.Add(Disposable.Create(() =>
			{
				_midiInDeviceWatcher.Stop();
				_midiOutDeviceWatcher.Stop();
			}));
		}

		public ObservableCollection<string> MidiInDevices { get; } = new ObservableCollection<string>();

		public ObservableCollection<string> MidiOutDevices { get; } = new ObservableCollection<string>();

		public int SelectedInputDeviceIndex
		{
			get => _selectedInputDeviceIndex;
			set
			{
				_selectedInputDeviceIndex = value;
				RaisePropertyChanged();
			}
		}

		public ICommand EnumerateInDevicesCommand => GetOrCreateCommand(EnumerateInDevices);

		public ICommand EnumerateOutDevicesCommand => GetOrCreateCommand(EnumerateOutDevices);

		private async void EnumerateInDevices()
		{
			MidiInDevices.Clear();
			var midiInputQueryString = MidiInPort.GetDeviceSelector();
			var midiInputDevices = await DeviceInformation.FindAllAsync(midiInputQueryString);

			// Return if no external devices are connected
			if (midiInputDevices.Count == 0)
			{
				MidiInDevices.Add("No MIDI input devices found!");
				return;
			}

			// Else, add each connected input device to the list
			foreach (var deviceInfo in midiInputDevices)
			{
				MidiInDevices.Add(deviceInfo.Name);
			}
		}

		private async void EnumerateOutDevices()
		{
			MidiOutDevices.Clear();
			var midiOutputQueryString = MidiOutPort.GetDeviceSelector();
			var midiOutputDevices = await DeviceInformation.FindAllAsync(midiOutputQueryString);

			// Return if no external devices are connected
			if (midiOutputDevices.Count == 0)
			{
				MidiOutDevices.Add("No MIDI output devices found!");
				return;
			}

			// Else, add each connected input device to the list
			foreach (var deviceInfo in midiOutputDevices)
			{
				MidiOutDevices.Add(deviceInfo.Name);
			}
		}

	}
}
