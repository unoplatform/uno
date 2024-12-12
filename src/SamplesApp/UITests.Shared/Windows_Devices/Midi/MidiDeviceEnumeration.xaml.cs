using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Devices.Enumeration;
using Windows.Devices.Midi;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Private.Infrastructure;

namespace UITests.Shared.Windows_Devices.Midi
{
	// Based on https://github.com/microsoft/Windows-universal-samples/blob/master/Samples/MIDI/cs/Scenario1_MIDIDeviceEnumeration.xaml.cs
	[SampleControlInfo("Windows.Devices", "Midi_DeviceEnumeration", description: "Sample for enumeration of MIDI devices")]
	public sealed partial class MidiDeviceEnumerationTests : UserControl
	{
		/// <summary>
		/// Device watchers for MIDI in and out ports
		/// </summary>
		private readonly MidiDeviceWatcher _midiInDeviceWatcher;
		private readonly MidiDeviceWatcher _midiOutDeviceWatcher;

		/// <summary>
		/// Constructor: Empty device lists, start the device watchers and
		/// set initial states for buttons
		/// </summary>
		public MidiDeviceEnumerationTests()
		{
			InitializeComponent();

			// Start with a clean slate
			ClearAllDeviceValues();

			// Ensure Auto-detect devices toggle is on
			deviceAutoDetectToggle.IsOn = true;

			// Set up the MIDI input and output device watchers
			_midiInDeviceWatcher = new MidiDeviceWatcher(MidiInPort.GetDeviceSelector(), UnitTestDispatcherCompat.From(this), inputDevices, InputDevices);
			_midiOutDeviceWatcher = new MidiDeviceWatcher(MidiOutPort.GetDeviceSelector(), UnitTestDispatcherCompat.From(this), outputDevices, OutputDevices);

			// Start watching for devices
			_midiInDeviceWatcher.Start();
			_midiOutDeviceWatcher.Start();

			// Disable manual enumeration buttons
			listInputDevicesButton.IsEnabled = false;
			listOutputDevicesButton.IsEnabled = false;

			Unloaded += MidiDeviceEnumerationTests_Unloaded;
		}

		public ObservableCollection<string> InputDevices { get; } = new ObservableCollection<string>();

		public ObservableCollection<string> InputDeviceProperties { get; } = new ObservableCollection<string>();

		public ObservableCollection<string> OutputDevices { get; } = new ObservableCollection<string>();

		public ObservableCollection<string> OutputDeviceProperties { get; } = new ObservableCollection<string>();

		private void MidiDeviceEnumerationTests_Unloaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			// Stop the input and output device watchers
			_midiInDeviceWatcher.Stop();
			_midiOutDeviceWatcher.Stop();
		}

		/// <summary>
		/// Clear all input and output MIDI device lists and properties
		/// </summary>
		private void ClearAllDeviceValues()
		{
			// Clear input devices
			InputDevices.Clear();
			InputDevices.Add("Click button to list input MIDI devices");
			inputDevices.IsEnabled = false;

			// Clear output devices
			OutputDevices.Clear();
			OutputDevices.Add("Click button to list output MIDI devices");
			outputDevices.IsEnabled = false;

			// Clear input device properties
			InputDeviceProperties.Clear();
			InputDeviceProperties.Add("Select a MIDI input device to view its properties");
			inputDeviceProperties.IsEnabled = false;

			// Clear output device properties
			OutputDeviceProperties.Clear();
			OutputDeviceProperties.Add("Select a MIDI output device to view its properties");
			outputDeviceProperties.IsEnabled = false;
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
			InputDevices.Clear();
			InputDeviceProperties.Clear();
			inputDeviceProperties.IsEnabled = false;

			// Find all input MIDI devices
			string midiInputQueryString = MidiInPort.GetDeviceSelector();
			DeviceInformationCollection midiInputDevices = await DeviceInformation.FindAllAsync(midiInputQueryString);

			// Return if no external devices are connected
			if (midiInputDevices.Count == 0)
			{
				InputDevices.Add("No MIDI input devices found!");
				inputDevices.IsEnabled = false;

				NotifyUser("Please connect at least one external MIDI device for this demo to work correctly");
				return;
			}

			// Else, add each connected input device to the list
			foreach (DeviceInformation deviceInfo in midiInputDevices)
			{
				InputDevices.Add(deviceInfo.Name);
				inputDevices.IsEnabled = true;
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
			OutputDevices.Clear();
			OutputDeviceProperties.Clear();
			outputDeviceProperties.IsEnabled = false;

			// Find all output MIDI devices
			string midiOutputQueryString = MidiOutPort.GetDeviceSelector();
			DeviceInformationCollection midiOutputDevices = await DeviceInformation.FindAllAsync(midiOutputQueryString);

			// Return if no external devices are connected, and GS synth is not detected
			if (midiOutputDevices.Count == 0)
			{
				OutputDevices.Add("No MIDI output devices found!");
				outputDevices.IsEnabled = false;

				NotifyUser("Please connect at least one external MIDI device for this demo to work correctly");
				return;
			}

			// List specific device information for each output device
			foreach (DeviceInformation deviceInfo in midiOutputDevices)
			{
				OutputDevices.Add(deviceInfo.Name);
				outputDevices.IsEnabled = true;
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
			if (deviceAutoDetectToggle.IsOn)
			{
				listInputDevicesButton.IsEnabled = false;
				listOutputDevicesButton.IsEnabled = false;

				if (_midiInDeviceWatcher != null)
				{
					_midiInDeviceWatcher.Start();
				}
				if (_midiOutDeviceWatcher != null)
				{
					_midiOutDeviceWatcher.Start();
				}
			}
			else
			{
				listInputDevicesButton.IsEnabled = true;
				listOutputDevicesButton.IsEnabled = true;

				if (_midiInDeviceWatcher != null)
				{
					_midiInDeviceWatcher.Stop();
				}
				if (_midiOutDeviceWatcher != null)
				{
					_midiOutDeviceWatcher.Stop();
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
			int selectedInputDeviceIndex = inputDevices.SelectedIndex;

			// Try to display the appropriate device properties
			if (selectedInputDeviceIndex < 0)
			{
				// Clear input device properties
				InputDeviceProperties.Clear();
				InputDeviceProperties.Add("Select a MIDI input device to view its properties");
				inputDeviceProperties.IsEnabled = false;
				NotifyUser("Select a MIDI input device to view its properties");
				return;
			}

			DeviceInformationCollection devInfoCollection = _midiInDeviceWatcher.GetDeviceInformationCollection();
			if (devInfoCollection == null)
			{
				InputDeviceProperties.Clear();
				InputDeviceProperties.Add("Device not found!");
				inputDeviceProperties.IsEnabled = false;
				NotifyUser("Device not found!");
				return;
			}

			DeviceInformation devInfo = devInfoCollection[selectedInputDeviceIndex];
			if (devInfo == null)
			{
				InputDeviceProperties.Clear();
				InputDeviceProperties.Add("Device not found!");
				inputDeviceProperties.IsEnabled = false;
				NotifyUser("Device not found!");
				return;
			}

			// Display the found properties
			DisplayDeviceProperties(devInfo, inputDeviceProperties, InputDeviceProperties);
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
			int selectedOutputDeviceIndex = outputDevices.SelectedIndex;

			// Try to display the appropriate device properties
			if (selectedOutputDeviceIndex < 0)
			{
				// Clear output device properties
				OutputDeviceProperties.Clear();
				OutputDeviceProperties.Add("Select a MIDI output device to view its properties");
				outputDeviceProperties.IsEnabled = false;
				NotifyUser("Select a MIDI output device to view its properties");
				return;
			}

			DeviceInformationCollection devInfoCollection = _midiOutDeviceWatcher.GetDeviceInformationCollection();
			if (devInfoCollection == null)
			{
				OutputDeviceProperties.Clear();
				OutputDeviceProperties.Add("Device not found!");
				outputDeviceProperties.IsEnabled = false;
				NotifyUser("Device not found!");
				return;
			}

			DeviceInformation devInfo = devInfoCollection[selectedOutputDeviceIndex];
			if (devInfo == null)
			{
				OutputDeviceProperties.Clear();
				OutputDeviceProperties.Add("Device not found!");
				outputDeviceProperties.IsEnabled = false;
				NotifyUser("Device not found!");
				return;
			}

			// Display the found properties
			DisplayDeviceProperties(devInfo, outputDeviceProperties, OutputDeviceProperties);
			NotifyUser("Device information found!");
		}

		/// <summary>
		/// Display the properties of the MIDI device to the user
		/// </summary>
		/// <param name="devInfo"></param>
		/// <param name="propertiesList"></param>
		private void DisplayDeviceProperties(DeviceInformation devInfo, ListView propertiesList, ObservableCollection<string> items)
		{
			items.Clear();
			items.Add("Id: " + devInfo.Id);
			items.Add("Name: " + devInfo.Name);
			items.Add("IsDefault: " + devInfo.IsDefault);
			items.Add("IsEnabled: " + devInfo.IsEnabled);
			items.Add("EnclosureLocation: " + devInfo.EnclosureLocation);

			// Add device interface information
			items.Add("----Device Interface----");
			foreach (var deviceProperty in devInfo.Properties)
			{
				items.Add(deviceProperty.Key + ": " + deviceProperty.Value);
			}

			propertiesList.IsEnabled = true;
		}

		private void NotifyUser(string message)
		{
			statusBlock.Text = message;
		}
	}
}
