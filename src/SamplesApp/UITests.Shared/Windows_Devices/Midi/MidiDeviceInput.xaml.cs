using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using UITests.Shared.Windows_Devices.Midi;
using Uno.Disposables;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Devices.Enumeration;
using Windows.Devices.Midi;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Private.Infrastructure;

namespace UITests.Windows_Devices.Midi
{
	// Based on https://github.com/microsoft/Windows-universal-samples/blob/master/Samples/MIDI/cs/Scenario2_ReceiveMIDIMessages.xaml.cs
	[Sample("Windows.Devices", Name = "Midi_Input")]
	public sealed partial class MidiDeviceInput : UserControl
	{
		/// <summary>
		/// Collection of active MidiInPorts
		/// </summary>
		private readonly List<MidiInPort> _midiInPorts;

		/// <summary>
		/// Device watcher for MIDI in ports
		/// </summary>
		private readonly MidiDeviceWatcher _midiInDeviceWatcher;

		/// <summary>
		/// Constructor: Start the device watcher
		/// </summary>
		public MidiDeviceInput()
		{
			InitializeComponent();

			rootGrid.DataContext = this;

			// Initialize the list of active MIDI input devices
			_midiInPorts = new List<MidiInPort>();

			// Set up the MIDI input device watcher
			_midiInDeviceWatcher = new MidiDeviceWatcher(MidiInPort.GetDeviceSelector(), UnitTestDispatcherCompat.From(this), inputDevices, InputDevices);

			// Start watching for devices
			_midiInDeviceWatcher.Start();

			Unloaded += MidiDeviceInput_Unloaded;
		}

		public ObservableCollection<string> InputDevices { get; } = new ObservableCollection<string>();

		public ObservableCollection<string> InputDeviceMessages { get; } = new ObservableCollection<string>();

		private void MidiDeviceInput_Unloaded(object sender, RoutedEventArgs e)
		{
			// Stop the input device watcher
			_midiInDeviceWatcher.Stop();

			// Close all MidiInPorts
			foreach (MidiInPort inPort in _midiInPorts)
			{
				inPort.Dispose();
			}
			_midiInPorts.Clear();
		}

		/// <summary>
		/// Change the input MIDI device from which to receive messages
		/// </summary>
		/// <param name="sender">Element that fired the event</param>
		/// <param name="e">Event arguments</param>
		private async void inputDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Get the selected input MIDI device
			int selectedInputDeviceIndex = (sender as ListView).SelectedIndex;

			// Try to create a MidiInPort
			if (selectedInputDeviceIndex < 0)
			{
				// Clear input device messages
				InputDeviceMessages.Clear();
				InputDeviceMessages.Add("Select a MIDI input device to be able to see its messages");
				inputDeviceMessages.IsEnabled = false;
				NotifyUser("Select a MIDI input device to be able to see its messages");
				return;
			}

			DeviceInformationCollection devInfoCollection = _midiInDeviceWatcher.GetDeviceInformationCollection();
			if (devInfoCollection == null)
			{
				InputDeviceMessages.Clear();
				InputDeviceMessages.Add("Device not found!");
				inputDeviceMessages.IsEnabled = false;
				NotifyUser("Device not found!");
				return;
			}

			DeviceInformation devInfo = devInfoCollection[selectedInputDeviceIndex];
			if (devInfo == null)
			{
				InputDeviceMessages.Clear();
				InputDeviceMessages.Add("Device not found!");
				inputDeviceMessages.IsEnabled = false;
				NotifyUser("Device not found!");
				return;
			}

			var currentMidiInputDevice = await MidiInPort.FromIdAsync(devInfo.Id);
			if (currentMidiInputDevice == null)
			{
				NotifyUser("Unable to create MidiInPort from input device");
				return;
			}

			// We have successfully created a MidiInPort; add the device to the list of active devices, and set up message receiving
			if (!_midiInPorts.Contains(currentMidiInputDevice))
			{
				_midiInPorts.Add(currentMidiInputDevice);
				currentMidiInputDevice.MessageReceived += MidiInputDevice_MessageReceived;
			}

			// Clear any previous input messages
			InputDeviceMessages.Clear();
			inputDeviceMessages.IsEnabled = true;

			NotifyUser("Input Device selected successfully! Waiting for messages...");
		}

		/// <summary>
		/// Display the received MIDI message in a readable format
		/// </summary>
		/// <param name="sender">Element that fired the event</param>
		/// <param name="args">The received message</param>
		private async void MidiInputDevice_MessageReceived(MidiInPort sender, MidiMessageReceivedEventArgs args)
		{
			IMidiMessage receivedMidiMessage = args.Message;

			// Build the received MIDI message into a readable format
			StringBuilder outputMessage = new StringBuilder();
			outputMessage.Append(receivedMidiMessage.Timestamp.ToString()).Append(", Type: ").Append(receivedMidiMessage.Type);

			// Add MIDI message parameters to the output, depending on the type of message
			switch (receivedMidiMessage.Type)
			{
				case MidiMessageType.NoteOff:
					var noteOffMessage = (MidiNoteOffMessage)receivedMidiMessage;
					outputMessage.Append(", Channel: ").Append(noteOffMessage.Channel).Append(", Note: ").Append(noteOffMessage.Note).Append(", Velocity: ").Append(noteOffMessage.Velocity);
					break;
				case MidiMessageType.NoteOn:
					var noteOnMessage = (MidiNoteOnMessage)receivedMidiMessage;
					outputMessage.Append(", Channel: ").Append(noteOnMessage.Channel).Append(", Note: ").Append(noteOnMessage.Note).Append(", Velocity: ").Append(noteOnMessage.Velocity);
					break;
				case MidiMessageType.PolyphonicKeyPressure:
					var polyphonicKeyPressureMessage = (MidiPolyphonicKeyPressureMessage)receivedMidiMessage;
					outputMessage.Append(", Channel: ").Append(polyphonicKeyPressureMessage.Channel).Append(", Note: ").Append(polyphonicKeyPressureMessage.Note).Append(", Pressure: ").Append(polyphonicKeyPressureMessage.Pressure);
					break;
				case MidiMessageType.ControlChange:
					var controlChangeMessage = (MidiControlChangeMessage)receivedMidiMessage;
					outputMessage.Append(", Channel: ").Append(controlChangeMessage.Channel).Append(", Controller: ").Append(controlChangeMessage.Controller).Append(", Value: ").Append(controlChangeMessage.ControlValue);
					break;
				case MidiMessageType.ProgramChange:
					var programChangeMessage = (MidiProgramChangeMessage)receivedMidiMessage;
					outputMessage.Append(", Channel: ").Append(programChangeMessage.Channel).Append(", Program: ").Append(programChangeMessage.Program);
					break;
				case MidiMessageType.ChannelPressure:
					var channelPressureMessage = (MidiChannelPressureMessage)receivedMidiMessage;
					outputMessage.Append(", Channel: ").Append(channelPressureMessage.Channel).Append(", Pressure: ").Append(channelPressureMessage.Pressure);
					break;
				case MidiMessageType.PitchBendChange:
					var pitchBendChangeMessage = (MidiPitchBendChangeMessage)receivedMidiMessage;
					outputMessage.Append(", Channel: ").Append(pitchBendChangeMessage.Channel).Append(", Bend: ").Append(pitchBendChangeMessage.Bend);
					break;
				case MidiMessageType.SystemExclusive:
					var systemExclusiveMessage = (MidiSystemExclusiveMessage)receivedMidiMessage;
					outputMessage.Append(", ");

					// Read the SysEx bufffer
					var sysExDataReader = DataReader.FromBuffer(systemExclusiveMessage.RawData);
					while (sysExDataReader.UnconsumedBufferLength > 0)
					{
						byte byteRead = sysExDataReader.ReadByte();
						// Pad with leading zero if necessary
						outputMessage.Append(byteRead.ToString("X2", CultureInfo.InvariantCulture)).Append(' ');
					}
					break;
				case MidiMessageType.MidiTimeCode:
					var timeCodeMessage = (MidiTimeCodeMessage)receivedMidiMessage;
					outputMessage.Append(", FrameType: ").Append(timeCodeMessage.FrameType).Append(", Values: ").Append(timeCodeMessage.Values);
					break;
				case MidiMessageType.SongPositionPointer:
					var songPositionPointerMessage = (MidiSongPositionPointerMessage)receivedMidiMessage;
					outputMessage.Append(", Beats: ").Append(songPositionPointerMessage.Beats);
					break;
				case MidiMessageType.SongSelect:
					var songSelectMessage = (MidiSongSelectMessage)receivedMidiMessage;
					outputMessage.Append(", Song: ").Append(songSelectMessage.Song);
					break;
				case MidiMessageType.None:
					throw new InvalidOperationException();
			}

			// Skip TimingClock and ActiveSensing messages to avoid overcrowding the list. Commment this check out to see all messages
			if ((receivedMidiMessage.Type != MidiMessageType.TimingClock) && (receivedMidiMessage.Type != MidiMessageType.ActiveSensing))
			{
				// Use the Dispatcher to update the messages on the UI thread
				await UnitTestDispatcherCompat.From(this).RunAsync(UnitTestDispatcherCompat.Priority.Normal, () =>
				{

					InputDeviceMessages.Insert(0, outputMessage.ToString());
					NotifyUser("Message received successfully!");

				});
			}
		}

		private void NotifyUser(string message)
		{
			statusBlock.Text = message;
		}
	}
}
