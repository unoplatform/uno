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

namespace UITests.Shared.Windows_Devices.Midi
{
	// Based on https://github.com/microsoft/Windows-universal-samples/blob/master/Samples/MIDI/cs/Scenario1_MIDIDeviceEnumeration.xaml.cs
	[SampleControlInfo("Windows.Devices", "Midi_DeviceEnumeration", description: "Sample for enumeration of MIDI devices")]
	public sealed partial class MidiDeviceEnumerationTests : UserControl
	{
		/// <summary>
		/// Device watchers for MIDI in and out ports
		/// </summary>
		MidiDeviceWatcher midiInDeviceWatcher;
		MidiDeviceWatcher midiOutDeviceWatcher;

		/// <summary>
		/// Constructor: Empty device lists, start the device watchers and
		/// set initial states for buttons
		/// </summary>
		public MidiDeviceEnumerationTests()
		{
			this.InitializeComponent();

			this.rootGrid.DataContext = this;

			// Start with a clean slate
			ClearAllDeviceValues();

			// Ensure Auto-detect devices toggle is on
			this.deviceAutoDetectToggle.IsOn = true;

			// Set up the MIDI input and output device watchers
			this.midiInDeviceWatcher = new MidiDeviceWatcher(MidiInPort.GetDeviceSelector(), Dispatcher, this.inputDevices);
			this.midiOutDeviceWatcher = new MidiDeviceWatcher(MidiOutPort.GetDeviceSelector(), Dispatcher, this.outputDevices);

			// Start watching for devices
			this.midiInDeviceWatcher.Start();
			this.midiOutDeviceWatcher.Start();

			// Disable manual enumeration buttons
			this.listInputDevicesButton.IsEnabled = false;
			this.listOutputDevicesButton.IsEnabled = false;

			this.Unloaded += MidiDeviceEnumerationTests_Unloaded;
		}

		public ObservableCollection<string> InputDevices { get; } = new ObservableCollection<string>();

		public ObservableCollection<string> OutputDevices { get; } = new ObservableCollection<string>();

		private void MidiDeviceEnumerationTests_Unloaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			// Stop the input and output device watchers
			this.midiInDeviceWatcher.Stop();
			this.midiOutDeviceWatcher.Stop();
		}

		/// <summary>
		/// Clear all input and output MIDI device lists and properties
		/// </summary>
		private void ClearAllDeviceValues()
		{
			// Clear input devices
			this.inputDevices.Items.Clear();
			this.inputDevices.Items.Add("Click button to list input MIDI devices");
			this.inputDevices.IsEnabled = false;

			// Clear output devices
			this.outputDevices.Items.Clear();
			this.outputDevices.Items.Add("Click button to list output MIDI devices");
			this.outputDevices.IsEnabled = false;

			// Clear input device properties
			this.inputDeviceProperties.Items.Clear();
			this.inputDeviceProperties.Items.Add("Select a MIDI input device to view its properties");
			this.inputDeviceProperties.IsEnabled = false;

			// Clear output device properties
			this.outputDeviceProperties.Items.Clear();
			this.outputDeviceProperties.Items.Add("Select a MIDI output device to view its properties");
			this.outputDeviceProperties.IsEnabled = false;
		}

		/// <summary>
		/// Input button click handler
		/// </summary>
		/// <param name="sender">Element that fired the event</param>
		/// <param name="e">Event arguments</param>
		private async void listInputDevicesButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			// Enumerate input devices
			await EnumerateMidiInputDevices();
		}

		/// <summary>
		/// Query DeviceInformation class for Midi Input devices
		/// </summary>
		private async Task EnumerateMidiInputDevices()
		{
			// Clear input devices
			this.inputDevices.Items.Clear();
			this.inputDeviceProperties.Items.Clear();
			this.inputDeviceProperties.IsEnabled = false;

			// Find all input MIDI devices
			string midiInputQueryString = MidiInPort.GetDeviceSelector();
			DeviceInformationCollection midiInputDevices = await DeviceInformation.FindAllAsync(midiInputQueryString);

			// Return if no external devices are connected
			if (midiInputDevices.Count == 0)
			{
				this.inputDevices.Items.Add("No MIDI input devices found!");
				this.inputDevices.IsEnabled = false;

				NotifyUser("Please connect at least one external MIDI device for this demo to work correctly");
				return;
			}

			// Else, add each connected input device to the list
			foreach (DeviceInformation deviceInfo in midiInputDevices)
			{
				this.inputDevices.Items.Add(deviceInfo.Name);
				this.inputDevices.IsEnabled = true;
			}

			NotifyUser("MIDI Input devices found!");
		}

		/// <summary>
		/// Output button click handler
		/// </summary>
		/// <param name="sender">Element that fired the event</param>
		/// <param name="e">Event arguments</param>
		private async void listOutputDevicesButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			// Enumerate output devices
			await EnumerateMidiOutputDevices();
		}

		/// <summary>
		/// Query DeviceInformation class for Midi Output devices
		/// </summary>
		private async Task EnumerateMidiOutputDevices()
		{
			// Clear output devices
			this.outputDevices.Items.Clear();
			this.outputDeviceProperties.Items.Clear();
			this.outputDeviceProperties.IsEnabled = false;

			// Find all output MIDI devices
			string midiOutputQueryString = MidiOutPort.GetDeviceSelector();
			DeviceInformationCollection midiOutputDevices = await DeviceInformation.FindAllAsync(midiOutputQueryString);

			// Return if no external devices are connected, and GS synth is not detected
			if (midiOutputDevices.Count == 0)
			{
				this.outputDevices.Items.Add("No MIDI output devices found!");
				this.outputDevices.IsEnabled = false;

				NotifyUser("Please connect at least one external MIDI device for this demo to work correctly");
				return;
			}

			// List specific device information for each output device
			foreach (DeviceInformation deviceInfo in midiOutputDevices)
			{
				this.outputDevices.Items.Add(deviceInfo.Name);
				this.outputDevices.IsEnabled = true;
			}

			NotifyUser("MIDI Output devices found!");
		}

		/// <summary>
		/// Detect the toggle state of the Devicewatcher button.
		/// If auto-detect is on, disable manual enumeration buttons and start device watchers
		/// If auto-detect is off, enable manual enumeration buttons and stop device watchers
		/// </summary>
		/// <param name="sender">Element that fired the event</param>
		/// <param name="e">Event arguments</param>
		private void DeviceAutoDetectToggle_Toggled(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			if (this.deviceAutoDetectToggle.IsOn)
			{
				this.listInputDevicesButton.IsEnabled = false;
				this.listOutputDevicesButton.IsEnabled = false;

				if (this.midiInDeviceWatcher != null)
				{
					this.midiInDeviceWatcher.Start();
				}
				if (this.midiOutDeviceWatcher != null)
				{
					this.midiOutDeviceWatcher.Start();
				}
			}
			else
			{
				this.listInputDevicesButton.IsEnabled = true;
				this.listOutputDevicesButton.IsEnabled = true;

				if (this.midiInDeviceWatcher != null)
				{
					this.midiInDeviceWatcher.Stop();
				}
				if (this.midiOutDeviceWatcher != null)
				{
					this.midiOutDeviceWatcher.Stop();
				}
			}
		}

		/// <summary>
		/// Change the active input MIDI device
		/// </summary>
		/// <param name="sender">Element that fired the event</param>
		/// <param name="e">Event arguments</param>
		private void inputDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Get the selected input MIDI device
			int selectedInputDeviceIndex = this.inputDevices.SelectedIndex;

			// Try to display the appropriate device properties
			if (selectedInputDeviceIndex < 0)
			{
				// Clear input device properties
				this.inputDeviceProperties.Items.Clear();
				this.inputDeviceProperties.Items.Add("Select a MIDI input device to view its properties");
				this.inputDeviceProperties.IsEnabled = false;
				NotifyUser("Select a MIDI input device to view its properties");
				return;
			}

			DeviceInformationCollection devInfoCollection = this.midiInDeviceWatcher.GetDeviceInformationCollection();
			if (devInfoCollection == null)
			{
				this.inputDeviceProperties.Items.Clear();
				this.inputDeviceProperties.Items.Add("Device not found!");
				this.inputDeviceProperties.IsEnabled = false;
				NotifyUser("Device not found!");
				return;
			}

			DeviceInformation devInfo = devInfoCollection[selectedInputDeviceIndex];
			if (devInfo == null)
			{
				this.inputDeviceProperties.Items.Clear();
				this.inputDeviceProperties.Items.Add("Device not found!");
				this.inputDeviceProperties.IsEnabled = false;
				NotifyUser("Device not found!");
				return;
			}

			// Display the found properties
			DisplayDeviceProperties(devInfo, this.inputDeviceProperties);
			NotifyUser("Device information found!");
		}

		/// <summary>
		/// Change the active output MIDI device
		/// </summary>
		/// <param name="sender">Element that fired the event</param>
		/// <param name="e">Event arguments</param>
		private void outputDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Get the selected output MIDI device
			int selectedOutputDeviceIndex = this.outputDevices.SelectedIndex;

			// Try to display the appropriate device properties
			if (selectedOutputDeviceIndex < 0)
			{
				// Clear output device properties
				this.outputDeviceProperties.Items.Clear();
				this.outputDeviceProperties.Items.Add("Select a MIDI output device to view its properties");
				this.outputDeviceProperties.IsEnabled = false;
				NotifyUser("Select a MIDI output device to view its properties");
				return;
			}

			DeviceInformationCollection devInfoCollection = this.midiOutDeviceWatcher.GetDeviceInformationCollection();
			if (devInfoCollection == null)
			{
				this.outputDeviceProperties.Items.Clear();
				this.outputDeviceProperties.Items.Add("Device not found!");
				this.outputDeviceProperties.IsEnabled = false;
				NotifyUser("Device not found!");
				return;
			}

			DeviceInformation devInfo = devInfoCollection[selectedOutputDeviceIndex];
			if (devInfo == null)
			{
				this.outputDeviceProperties.Items.Clear();
				this.outputDeviceProperties.Items.Add("Device not found!");
				this.outputDeviceProperties.IsEnabled = false;
				NotifyUser("Device not found!");
				return;
			}

			// Display the found properties
			DisplayDeviceProperties(devInfo, this.outputDeviceProperties);
			NotifyUser("Device information found!");
		}

		/// <summary>
		/// Display the properties of the MIDI device to the user
		/// </summary>
		/// <param name="devInfo"></param>
		/// <param name="propertiesList"></param>
		private void DisplayDeviceProperties(DeviceInformation devInfo, ListView propertiesList)
		{
			propertiesList.Items.Clear();
			propertiesList.Items.Add("Id: " + devInfo.Id);
			propertiesList.Items.Add("Name: " + devInfo.Name);
			propertiesList.Items.Add("IsDefault: " + devInfo.IsDefault);
			propertiesList.Items.Add("IsEnabled: " + devInfo.IsEnabled);
			propertiesList.Items.Add("EnclosureLocation: " + devInfo.EnclosureLocation);

			// Add device interface information
			propertiesList.Items.Add("----Device Interface----");
			foreach (var deviceProperty in devInfo.Properties)
			{
				propertiesList.Items.Add(deviceProperty.Key + ": " + deviceProperty.Value);
			}

			propertiesList.IsEnabled = true;
		}

		private void NotifyUser(string message)
		{
			statusBlock.Text = message;
		}

		//private byte ENote = 64;
		//private byte DNote = 62;
		//private byte CNote = 60;
		//private byte FNote = 65;
		//private byte GNote = 67;
		//private const int Skip = 400;

		//private async void Play()
		//{
		//	if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
		//	{
		//		this.Log().Error("Play");
		//	}
		//	if (SelectedOutputDeviceIndex < 0)
		//	{
		//		SelectedOutputDeviceIndex = 0;
		//	}
		//	if (SelectedOutputDeviceIndex >= 0)
		//	{
		//		var midiOutputQueryString = MidiOutPort.GetDeviceSelector();
		//		var midiOutputDevices = await DeviceInformation.FindAllAsync(midiOutputQueryString);
		//		var device = midiOutputDevices[SelectedOutputDeviceIndex];

		//		_port = await MidiOutPort.FromIdAsync(device.Id);
		//		JingleBells();
		//	}
		//}

		//private async void JingleBells()
		//{
		//	await PlayNoteAsync(ENote);
		//	await PlayNoteAsync(ENote);
		//	await PlayNoteAsync(ENote, Skip * 2, 127);
		//	await PlayNoteAsync(ENote);
		//	await PlayNoteAsync(ENote);
		//	await PlayNoteAsync(ENote, Skip * 2, 127);
		//	await PlayNoteAsync(ENote);
		//	await PlayNoteAsync(GNote);
		//	await PlayNoteAsync(CNote);
		//	await PlayNoteAsync(DNote);
		//	await PlayNoteAsync(ENote, Skip * 4, 127);
		//	await PlayNoteAsync(FNote);
		//	await PlayNoteAsync(FNote);
		//	await PlayNoteAsync(FNote);
		//	await PlayNoteAsync(FNote);
		//	await PlayNoteAsync(FNote);
		//	await PlayNoteAsync(ENote);
		//	await PlayNoteAsync(ENote);
		//	await PlayNoteAsync(ENote, Skip / 2, 127);
		//	await PlayNoteAsync(ENote, Skip / 2, 127);
		//	await PlayNoteAsync(ENote);
		//	await PlayNoteAsync(DNote);
		//	await PlayNoteAsync(DNote);
		//	await PlayNoteAsync(ENote);
		//	await PlayNoteAsync(DNote, Skip * 2, 127);
		//	await PlayNoteAsync(GNote, Skip * 2, 127);
		//	await PlayNoteAsync(ENote);
		//	await PlayNoteAsync(ENote);
		//	await PlayNoteAsync(ENote, Skip * 2, 127);
		//	await PlayNoteAsync(ENote);
		//	await PlayNoteAsync(ENote);
		//	await PlayNoteAsync(ENote, Skip * 2, 127);
		//	await PlayNoteAsync(ENote);
		//	await PlayNoteAsync(GNote);
		//	await PlayNoteAsync(CNote);
		//	await PlayNoteAsync(DNote);
		//	await PlayNoteAsync(ENote, Skip * 4, 127);
		//	await PlayNoteAsync(FNote);
		//	await PlayNoteAsync(FNote);
		//	await PlayNoteAsync(FNote);
		//	await PlayNoteAsync(FNote);
		//	await PlayNoteAsync(FNote);
		//	await PlayNoteAsync(ENote);
		//	await PlayNoteAsync(ENote);
		//	await PlayNoteAsync(ENote, Skip / 2, 127);
		//	await PlayNoteAsync(ENote, Skip / 2, 127);
		//	await PlayNoteAsync(GNote);
		//	await PlayNoteAsync(GNote);
		//	await PlayNoteAsync(FNote);
		//	await PlayNoteAsync(DNote);
		//	await PlayNoteAsync(CNote, Skip * 4, 127);





		//}

		//private async Task PlayNoteAsync(byte noteNumber, int duration = Skip, byte velocity = 127)
		//{
		//	_port.SendMessage(new MidiNoteOnMessage(0, noteNumber, velocity));
		//	await Task.Delay(duration);
		//	_port.SendMessage(new MidiNoteOffMessage(0, noteNumber, velocity));
		//}
	}
}
