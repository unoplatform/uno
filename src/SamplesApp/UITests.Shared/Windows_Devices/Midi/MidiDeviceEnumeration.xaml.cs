using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Logging;
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
		private int _selectedOutputDeviceIndex = 0;

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

		public int SelectedOutputDeviceIndex
		{
			get => _selectedOutputDeviceIndex;
			set
			{
				_selectedOutputDeviceIndex = value;
				RaisePropertyChanged();
			}
		}

		public ICommand EnumerateInDevicesCommand => GetOrCreateCommand(EnumerateInDevices);

		public ICommand EnumerateOutDevicesCommand => GetOrCreateCommand(EnumerateOutDevices);

		public ICommand PlayCommand => GetOrCreateCommand(Play);

		private IMidiOutPort _port;
		private byte ENote = 64;
		private byte DNote = 62;
		private byte CNote = 60;
		private byte FNote = 65;
		private byte GNote = 67;
		private const int Skip = 400;

		private async void Play()
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
			{
				this.Log().Error("Play");
			}
			if (SelectedOutputDeviceIndex >= 0)
			{
				var midiOutputQueryString = MidiOutPort.GetDeviceSelector();
				var midiOutputDevices = await DeviceInformation.FindAllAsync(midiOutputQueryString);
				var device = midiOutputDevices[SelectedOutputDeviceIndex];

				_port = await MidiOutPort.FromIdAsync(device.Id);
				JingleBells();
			}
		}

		private async void JingleBells()
		{
			await PlayNoteAsync(ENote);
			await PlayNoteAsync(ENote);
			await PlayNoteAsync(ENote, Skip * 2, 127);
			await PlayNoteAsync(ENote);
			await PlayNoteAsync(ENote);
			await PlayNoteAsync(ENote, Skip * 2, 127);
			await PlayNoteAsync(ENote);
			await PlayNoteAsync(GNote);
			await PlayNoteAsync(CNote);
			await PlayNoteAsync(DNote);
			await PlayNoteAsync(ENote, Skip * 4, 127);
			await PlayNoteAsync(FNote);
			await PlayNoteAsync(FNote);
			await PlayNoteAsync(FNote);
			await PlayNoteAsync(FNote);
			await PlayNoteAsync(FNote);
			await PlayNoteAsync(ENote);
			await PlayNoteAsync(ENote);
			await PlayNoteAsync(ENote, Skip / 2, 127);
			await PlayNoteAsync(ENote, Skip / 2, 127);
			await PlayNoteAsync(ENote);
			await PlayNoteAsync(DNote);
			await PlayNoteAsync(DNote);
			await PlayNoteAsync(ENote);
			await PlayNoteAsync(DNote, Skip * 2, 127);
			await PlayNoteAsync(GNote, Skip * 2, 127);
			await PlayNoteAsync(ENote);
			await PlayNoteAsync(ENote);
			await PlayNoteAsync(ENote, Skip * 2, 127);
			await PlayNoteAsync(ENote);
			await PlayNoteAsync(ENote);
			await PlayNoteAsync(ENote, Skip * 2, 127);
			await PlayNoteAsync(ENote);
			await PlayNoteAsync(GNote);
			await PlayNoteAsync(CNote);
			await PlayNoteAsync(DNote);
			await PlayNoteAsync(ENote, Skip * 4, 127);
			await PlayNoteAsync(FNote);
			await PlayNoteAsync(FNote);
			await PlayNoteAsync(FNote);
			await PlayNoteAsync(FNote);
			await PlayNoteAsync(FNote);
			await PlayNoteAsync(ENote);
			await PlayNoteAsync(ENote);
			await PlayNoteAsync(ENote, Skip / 2, 127);
			await PlayNoteAsync(ENote, Skip / 2, 127);
			await PlayNoteAsync(GNote);
			await PlayNoteAsync(GNote);
			await PlayNoteAsync(FNote);
			await PlayNoteAsync(DNote);
			await PlayNoteAsync(CNote, Skip * 4, 127);





		}

		private async Task PlayNoteAsync(byte noteNumber, int duration = Skip, byte velocity = 127)
		{
			_port.SendMessage(new MidiNoteOnMessage(0, noteNumber, velocity));
			await Task.Delay(duration);
			_port.SendMessage(new MidiNoteOffMessage(0, noteNumber, velocity));
		}

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
