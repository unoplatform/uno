#pragma warning disable 105 // Disabled until the tree is migrate to WinUI

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UITests.Shared.Helpers;
using Uno.Disposables;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Devices.Enumeration;
using Windows.Devices.Midi;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.Threading.Tasks;
using Private.Infrastructure;

#if !WINAPPSDK
using Microsoft.UI.Xaml.Controls;
#endif

namespace UITests.Shared.Windows_Devices.Midi
{
	// Based on https://github.com/microsoft/Windows-universal-samples/blob/master/Samples/MIDI/cs/Scenario3_SendMIDIMessages.xaml.cs
	[Sample("Windows.Devices", Name = "Midi_Output", Description = "Output to a connected MIDI device")]
	public sealed partial class MidiDeviceOutputTests : UserControl
	{
		/// <summary>
		/// Collection of active MidiOutPorts
		/// </summary>
		private readonly List<IMidiOutPort> _midiOutPorts;

		/// <summary>
		/// Device watcher for MIDI out ports
		/// </summary>
		private readonly MidiDeviceWatcher _midiOutDeviceWatcher;

		/// <summary>
		/// Ordered list to keep track of available MIDI message types
		/// </summary>
		private Dictionary<MidiMessageType, string> _messageTypes;

		/// <summary>
		/// Keep track of the type of message the user intends to send
		/// </summary>
		private MidiMessageType _currentMessageType = MidiMessageType.None;

		/// <summary>
		/// Keep track of the current output device (which could also be the GS synth)
		/// </summary>
		private IMidiOutPort _currentMidiOutputDevice;

		/// <summary>
		/// Constructor: Start the device watcher and populate MIDI message types
		/// </summary>
		public MidiDeviceOutputTests()
		{
			InitializeComponent();

			rootGrid.DataContext = this;

			// Initialize the list of active MIDI output devices
			_midiOutPorts = new List<IMidiOutPort>();

			// Set up the MIDI output device watcher
			_midiOutDeviceWatcher = new MidiDeviceWatcher(MidiOutPort.GetDeviceSelector(), UnitTestDispatcherCompat.From(this), outputDevices, OutputDevices);

			// Start watching for devices
			_midiOutDeviceWatcher.Start();

			// Populate message types into list
			PopulateMessageTypes();

			Unloaded += MidiDeviceOutputTests_Unloaded;
		}

		public ObservableCollection<string> OutputDevices { get; } = new ObservableCollection<string>();

		public ObservableCollection<string> MessageTypeItems { get; } = new ObservableCollection<string>();

		private void MidiDeviceOutputTests_Unloaded(object sender, RoutedEventArgs e)
		{
			// Stop the output device watcher
			_midiOutDeviceWatcher.Stop();

			// Close all MidiOutPorts
			foreach (IMidiOutPort outPort in _midiOutPorts)
			{
				outPort.Dispose();
			}
			_midiOutPorts.Clear();
		}

		/// <summary>
		/// Add all available MIDI message types to a map (except for MidiMessageType.None)
		/// and populate the MIDI message combo box
		/// </summary>
		private void PopulateMessageTypes()
		{
			// Build the list of available MIDI messages for reverse lookup later
			_messageTypes = new Dictionary<MidiMessageType, string>();
			_messageTypes.Add(MidiMessageType.ActiveSensing, "Active Sensing");
			_messageTypes.Add(MidiMessageType.ChannelPressure, "Channel Pressure");
			_messageTypes.Add(MidiMessageType.Continue, "Continue");
			_messageTypes.Add(MidiMessageType.ControlChange, "Control Change");
			_messageTypes.Add(MidiMessageType.MidiTimeCode, "MIDI Time Code");
			_messageTypes.Add(MidiMessageType.NoteOff, "Note Off");
			_messageTypes.Add(MidiMessageType.NoteOn, "Note On");
			_messageTypes.Add(MidiMessageType.PitchBendChange, "Pitch Bend Change");
			_messageTypes.Add(MidiMessageType.PolyphonicKeyPressure, "Polyphonic Key Pressure");
			_messageTypes.Add(MidiMessageType.ProgramChange, "Program Change");
			_messageTypes.Add(MidiMessageType.SongPositionPointer, "Song Position Pointer");
			_messageTypes.Add(MidiMessageType.SongSelect, "Song Select");
			_messageTypes.Add(MidiMessageType.Start, "Start");
			_messageTypes.Add(MidiMessageType.Stop, "Stop");
			_messageTypes.Add(MidiMessageType.SystemExclusive, "System Exclusive");
			_messageTypes.Add(MidiMessageType.SystemReset, "System Reset");
			_messageTypes.Add(MidiMessageType.TimingClock, "Timing Clock");
			_messageTypes.Add(MidiMessageType.TuneRequest, "Tune Request");

			// Start with a clean slate
			MessageTypeItems.Clear();

			// Add the message types to the list
			foreach (var messageType in _messageTypes)
			{
				MessageTypeItems.Add(messageType.Value);
			}
		}

		/// <summary>
		/// Create a new MidiOutPort for the selected device
		/// </summary>
		/// <param name="sender">Element that fired the event</param>
		/// <param name="e">Event arguments</param>
		private async void outputDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Get the selected output MIDI device
			int selectedOutputDeviceIndex = outputDevices.SelectedIndex;
			messageType.IsEnabled = false;
			JingleBells.IsEnabled = false;
			HappyBirthday.IsEnabled = false;
			resetButton.IsEnabled = false;

			// Try to create a MidiOutPort
			if (selectedOutputDeviceIndex < 0)
			{
				NotifyUser("Select a MIDI output device to be able to send messages to it");
				return;
			}

			DeviceInformationCollection devInfoCollection = _midiOutDeviceWatcher.GetDeviceInformationCollection();
			if (devInfoCollection == null)
			{
				NotifyUser("Device not found!");
				return;
			}

			DeviceInformation devInfo = devInfoCollection[selectedOutputDeviceIndex];
			if (devInfo == null)
			{
				NotifyUser("Device not found!");
				return;
			}

			_currentMidiOutputDevice = await MidiOutPort.FromIdAsync(devInfo.Id);
			if (_currentMidiOutputDevice == null)
			{
				NotifyUser("Unable to create MidiOutPort from output device");
				return;
			}

			// We have successfully created a MidiOutPort; add the device to the list of active devices
			if (!_midiOutPorts.Contains(_currentMidiOutputDevice))
			{
				_midiOutPorts.Add(_currentMidiOutputDevice);
			}

			// Enable message type list & reset button
			messageType.IsEnabled = true;
			JingleBells.IsEnabled = true;
			HappyBirthday.IsEnabled = true;
			resetButton.IsEnabled = true;

			NotifyUser("Output Device selected successfully! Waiting for message type selection...");
		}

		/// <summary>
		/// Reset all input fields, including message type
		/// </summary>
		/// <param name="sender">Element that fired the event</param>
		/// <param name="e">Event arguments</param>
		private void resetButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			ResetMessageTypeAndParameters(true);
		}

		/// <summary>
		/// Reset all input fields
		/// </summary>
		/// <param name="resetMessageType">If true, reset message type list as well</param>
		private void ResetMessageTypeAndParameters(bool resetMessageType)
		{
			// If the flag is set, reset the message type list as well
			if (resetMessageType)
			{
				messageType.SelectedIndex = -1;
				_currentMessageType = MidiMessageType.None;
			}

			// Ensure the message type list and reset button are enabled
			messageType.IsEnabled = true;
			resetButton.IsEnabled = true;

			// Reset selections on parameters
#if !WINAPPSDK
			parameter1.Value = 0;
			parameter2.Value = 0;
			parameter3.Value = 0;
#else
			parameter1.Text = "0";
			parameter2.Text = "0";
			parameter3.Text = "0";
#endif

			// New selection values will cause parameter boxes to be hidden and disabled
			UpdateParameterList1();
			UpdateParameterList2();
			UpdateParameterList3();

			// Disable send button & hide/clear the SysEx buffer text
			sendButton.IsEnabled = true;
			rawBufferHeader.Visibility = Visibility.Collapsed;
			sysExMessageContent.Text = "";
			sysExMessageContent.Visibility = Visibility.Collapsed;
		}

		/// <summary>
		/// Create a new MIDI message based on the message type and parameter(s) values,
		/// and send it to the chosen output device
		/// </summary>
		/// <param name="sender">Element that fired the event</param>
		/// <param name="e">Event arguments</param>
		private void sendButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			IMidiMessage midiMessageToSend = null;

			switch (_currentMessageType)
			{
				case MidiMessageType.NoteOff:
					midiMessageToSend = new MidiNoteOffMessage(Convert.ToByte(GetParameterValue(parameter1)), Convert.ToByte(GetParameterValue(parameter2)), Convert.ToByte(GetParameterValue(parameter3)));
					break;
				case MidiMessageType.NoteOn:
					midiMessageToSend = new MidiNoteOnMessage(Convert.ToByte(GetParameterValue(parameter1)), Convert.ToByte(GetParameterValue(parameter2)), Convert.ToByte(GetParameterValue(parameter3)));
					break;
				case MidiMessageType.PolyphonicKeyPressure:
					midiMessageToSend = new MidiPolyphonicKeyPressureMessage(Convert.ToByte(GetParameterValue(parameter1)), Convert.ToByte(GetParameterValue(parameter2)), Convert.ToByte(GetParameterValue(parameter3)));
					break;
				case MidiMessageType.ControlChange:
					midiMessageToSend = new MidiControlChangeMessage(Convert.ToByte(GetParameterValue(parameter1)), Convert.ToByte(GetParameterValue(parameter2)), Convert.ToByte(GetParameterValue(parameter3)));
					break;
				case MidiMessageType.ProgramChange:
					midiMessageToSend = new MidiProgramChangeMessage(Convert.ToByte(GetParameterValue(parameter1)), Convert.ToByte(GetParameterValue(parameter2)));
					break;
				case MidiMessageType.ChannelPressure:
					midiMessageToSend = new MidiChannelPressureMessage(Convert.ToByte(GetParameterValue(parameter1)), Convert.ToByte(GetParameterValue(parameter2)));
					break;
				case MidiMessageType.PitchBendChange:
					midiMessageToSend = new MidiPitchBendChangeMessage(Convert.ToByte(GetParameterValue(parameter1)), Convert.ToUInt16(GetParameterValue(parameter2)));
					break;
				case MidiMessageType.SystemExclusive:
					var dataWriter = new DataWriter();
					var sysExMessage = sysExMessageContent.Text;
					var sysExMessageLength = sysExMessage.Length;

					// Do not send a blank SysEx message
					if (sysExMessageLength == 0)
					{
						return;
					}

					// SysEx messages are two characters long with 1-character space in between them
					// So we add 1 to the message length, so that it is perfectly divisible by 3
					// The loop count tracks the number of individual message pieces
					int loopCount = (sysExMessageLength + 1) / 3;

					// Expecting a string of format "F0 NN NN NN NN.... F7", where NN is a byte in hex
					for (int i = 0; i < loopCount; i++)
					{
						var messageString = sysExMessage.Substring(3 * i, 2);
						var messageByte = Convert.ToByte(messageString, 16);
						dataWriter.WriteByte(messageByte);
					}
					midiMessageToSend = new MidiSystemExclusiveMessage(dataWriter.DetachBuffer());
					break;
				case MidiMessageType.MidiTimeCode:
					midiMessageToSend = new MidiTimeCodeMessage(Convert.ToByte(GetParameterValue(parameter1)), Convert.ToByte(GetParameterValue(parameter2)));
					break;
				case MidiMessageType.SongPositionPointer:
					midiMessageToSend = new MidiSongPositionPointerMessage(Convert.ToUInt16(GetParameterValue(parameter1)));
					break;
				case MidiMessageType.SongSelect:
					midiMessageToSend = new MidiSongSelectMessage(Convert.ToByte(GetParameterValue(parameter1)));
					break;
				case MidiMessageType.TuneRequest:
					midiMessageToSend = new MidiTuneRequestMessage();
					break;
				case MidiMessageType.TimingClock:
					midiMessageToSend = new MidiTimingClockMessage();
					break;
				case MidiMessageType.Start:
					midiMessageToSend = new MidiStartMessage();
					break;
				case MidiMessageType.Continue:
					midiMessageToSend = new MidiContinueMessage();
					break;
				case MidiMessageType.Stop:
					midiMessageToSend = new MidiStopMessage();
					break;
				case MidiMessageType.ActiveSensing:
					midiMessageToSend = new MidiActiveSensingMessage();
					break;
				case MidiMessageType.SystemReset:
					midiMessageToSend = new MidiSystemResetMessage();
					break;
				case MidiMessageType.None:
				default:
					return;
			}

			// Send the message
			_currentMidiOutputDevice.SendMessage(midiMessageToSend);
			NotifyUser("Message sent successfully");
		}

		/// <summary>
		/// Construct a MIDI message possibly with additional parameters,
		/// depending on the type of message
		/// </summary>
		/// <param name="sender">Element that fired the event</param>
		/// <param name="e">Event arguments</param>
		private void messageType_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Find the index of the user's choice
			int messageTypeSelectedIndex = messageType.SelectedIndex;

			// Return if reset
			if (messageTypeSelectedIndex == -1)
			{
				return;
			}

			// Clear the UI
			ResetMessageTypeAndParameters(false);

			// Find the key by index; that's our message type
			int count = 0;
			foreach (var messageType in _messageTypes)
			{
				if (messageTypeSelectedIndex == count)
				{
					_currentMessageType = messageType.Key;
					break;
				}
				count++;
			}

			// Some MIDI message types don't need additional parameters
			// For them, show the Send button as soon as user selects message type from the list
			switch (_currentMessageType)
			{
				// SysEx messages need to be in a particular format
				case MidiMessageType.SystemExclusive:
					rawBufferHeader.Visibility = Visibility.Visible;
					sysExMessageContent.Visibility = Visibility.Visible;
					// Provide start (0xF0) and end (0xF7) of SysEx values
					sysExMessageContent.Text = "F0 F7";
					// Let the user know the expected format of the message
					NotifyUser("Expecting a string of format 'NN NN NN NN....', where NN is a byte in hex");
					sendButton.IsEnabled = true;
					break;

				// These messages do not need additional parameters
				case MidiMessageType.ActiveSensing:
				case MidiMessageType.Continue:
				case MidiMessageType.Start:
				case MidiMessageType.Stop:
				case MidiMessageType.SystemReset:
				case MidiMessageType.TimingClock:
				case MidiMessageType.TuneRequest:
					sendButton.IsEnabled = true;
					break;

				default:
					sendButton.IsEnabled = false;
					break;
			}

			// Update the first parameter lists depending on the MIDI message type
			// If no further parameters are required, the list is emptied and hidden
			UpdateParameterList1();
			UpdateParameterList2();
			UpdateParameterList3();
		}

		/// <summary>
		/// For MIDI message types that need the first parameter, populate the list
		/// based on the message type. For message types that don't need the first
		/// parameter, empty and hide it
		/// </summary>
		private void UpdateParameterList1()
		{
			// The first parameter is different for different message types
			switch (_currentMessageType)
			{
				// For message types that require a first parameter...
				case MidiMessageType.NoteOff:
				case MidiMessageType.NoteOn:
				case MidiMessageType.PolyphonicKeyPressure:
				case MidiMessageType.ControlChange:
				case MidiMessageType.ProgramChange:
				case MidiMessageType.ChannelPressure:
				case MidiMessageType.PitchBendChange:
					// This list is for Channels, of which there are 16
					PopulateParameterList(parameter1, 16, "Channel");
					break;

				case MidiMessageType.MidiTimeCode:
					// This list is for further Message Types, of which there are 8
					PopulateParameterList(parameter1, 8, "Message Type");
					break;

				case MidiMessageType.SongPositionPointer:
					// This list is for Beats, of which there are 16384
					PopulateParameterList(parameter1, 16384, "Beats");
					break;

				case MidiMessageType.SongSelect:
					// This list is for Songs, of which there are 128
					PopulateParameterList(parameter1, 128, "Song");
					break;

				case MidiMessageType.SystemExclusive:
					// Start with a clean slate
					// Hide the first parameter
					parameter1.Header = "";
					parameter1.IsEnabled = false;
					parameter1.Visibility = Visibility.Collapsed;
					NotifyUser("Please edit the message in the textbox by clicking on 'F0 F7'");
					break;

				default:
					// Start with a clean slate
					// Hide the first parameter
					parameter1.Header = "";
					parameter1.IsEnabled = false;
					parameter1.Visibility = Visibility.Collapsed;
					NotifyUser("");
					break;
			}
		}

		/// <summary>
		/// React to Parameter1 selection change as appropriate
		/// </summary>
		/// <param name="sender">Element that fired the event</param>
		/// <param name="e">Event arguments</param>
		private void Parameter1_SelectionChanged(object sender, object e)
		{
#if WINAPPSDK
			int parameter1SelectedIndex = int.Parse(this.parameter1.Text);
#else
			// Find the index of the user's choice
			int parameter1SelectedIndex = (int)GetParameterValue(parameter1);
#endif

			// Some MIDI message types don't need additional parameters past parameter 1
			// For them, show the Send button as soon as user selects parameter 1 value from the list
			switch (_currentMessageType)
			{
				case MidiMessageType.SongPositionPointer:
				case MidiMessageType.SongSelect:

					if (parameter1SelectedIndex != -1)
					{
						sendButton.IsEnabled = true;
					}
					break;

				default:
					sendButton.IsEnabled = false;
					break;
			}

			// Update the second parameter list depending on the first parameter selection
			// If no further parameters are required, the list is emptied and hidden
			UpdateParameterList2();
		}

		/// <summary>
		/// For MIDI message types that need the second parameter, populate the list
		/// based on the message type. For message types that don't need the second
		/// parameter, empty and hide it
		/// </summary>
		private void UpdateParameterList2()
		{
#if WINAPPSDK
			int parameter2SelectedIndex = int.Parse(this.parameter2.Text);
#else
			// Find the index of the user's choice
			int parameter2SelectedIndex = (int)GetParameterValue(parameter2);
#endif

			// Some MIDI message types don't need additional parameters past parameter 2
			// For them, show the Send button as soon as user selects parameter 2 value from the list
			switch (_currentMessageType)
			{
				case MidiMessageType.ProgramChange:
				case MidiMessageType.ChannelPressure:
				case MidiMessageType.PitchBendChange:
				case MidiMessageType.MidiTimeCode:

					if (parameter2SelectedIndex != -1)
					{
						sendButton.IsEnabled = true;
					}
					break;

				default:
					sendButton.IsEnabled = false;
					break;
			}

			switch (_currentMessageType)
			{
				case MidiMessageType.NoteOff:
				case MidiMessageType.NoteOn:
				case MidiMessageType.PolyphonicKeyPressure:
					// This list is for Notes, of which there are 128
					PopulateParameterList(parameter2, 128, "Note");
					break;

				case MidiMessageType.ControlChange:
					// This list is for Controllers, of which there are 128
					PopulateParameterList(parameter2, 128, "Controller");
					break;

				case MidiMessageType.ProgramChange:
					// This list is for Program Numbers, of which there are 128
					PopulateParameterList(parameter2, 128, "Program Number");
					break;

				case MidiMessageType.ChannelPressure:
					// This list is for Pressure Values, of which there are 128
					PopulateParameterList(parameter2, 128, "Pressure Value");
					break;

				case MidiMessageType.PitchBendChange:
					// This list is for Pitch Bend Values, of which there are 16384
					PopulateParameterList(parameter2, 16384, "Pitch Bend Value");
					break;

				case MidiMessageType.MidiTimeCode:
					// This list is for Values, of which there are 16
					PopulateParameterList(parameter2, 16, "Value");
					break;

				default:
					// Start with a clean slate
					// Hide the first parameter
					parameter2.Header = "";
					parameter2.IsEnabled = false;
					parameter2.Visibility = Visibility.Collapsed;
					break;
			}
		}

		/// <summary>
		/// For MIDI message types that need the third parameter, populate the list
		/// based on the message type. For message types that don't need the third
		/// parameter, empty and hide it
		/// </summary>
		private void UpdateParameterList3()
		{
#if WINAPPSDK
			int parameter3SelectedIndex = int.Parse(this.parameter3.Text);
#else
			// Find the index of the user's choice
			int parameter3SelectedIndex = (int)GetParameterValue(parameter3);
#endif

			// The last set of MIDI message types don't need additional parameters
			// For them, show the Send button as soon as user selects parameter 3 value from the list
			// Set default to disable Send button for any message types that fall through
			switch (_currentMessageType)
			{
				case MidiMessageType.NoteOff:
				case MidiMessageType.NoteOn:
				case MidiMessageType.PolyphonicKeyPressure:
				case MidiMessageType.ControlChange:

					if (parameter3SelectedIndex != -1)
					{
						sendButton.IsEnabled = true;
					}
					break;

				default:
					sendButton.IsEnabled = false;
					break;
			}

			switch (_currentMessageType)
			{
				case MidiMessageType.NoteOff:
				case MidiMessageType.NoteOn:
					// This list is for Velocity Values, of which there are 128
					PopulateParameterList(parameter3, 128, "Velocity");
					break;

				case MidiMessageType.PolyphonicKeyPressure:
					// This list is for Pressure Values, of which there are 128
					PopulateParameterList(parameter3, 128, "Pressure");
					break;

				case MidiMessageType.ControlChange:
					// This list is for Values, of which there are 128
					PopulateParameterList(parameter3, 128, "Value");
					break;

				default:
					// Start with a clean slate
					// Hide the first parameter
					parameter3.Header = "";
					parameter3.IsEnabled = false;
					parameter3.Visibility = Visibility.Collapsed;
					break;
			}
		}

#if !WINAPPSDK
		/// <summary>
		/// Helper function to populate a dropdown lists with options
		/// </summary>
		/// <param name="numberOfOptions">Number of options in the list</param>
		/// <param name="listName">The header to display to the user</param>
		private void PopulateParameterList(NumberBox numberBox, int numberOfOptions, string listName)
		{
			numberBox.Maximum = numberOfOptions;

			// Show the list, so that the user can make the next choice
			numberBox.Header = listName;
			numberBox.IsEnabled = true;
			numberBox.Visibility = Visibility.Visible;
		}

		private int GetParameterValue(NumberBox numberBox) => (int)numberBox.Value;
#else
		private void PopulateParameterList(TextBox numberBox, int numberOfOptions, string listName)
		{
			// Show the list, so that the user can make the next choice
			numberBox.Header = listName;
			numberBox.IsEnabled = true;
			numberBox.Visibility = Visibility.Visible;
		}

		private int GetParameterValue(TextBox numberBox) => int.Parse(numberBox.Text);
#endif

		private void NotifyUser(string message)
		{
			statusBlock.Text = message;
		}


		#region Jingle Bells & Happy Birthday
		private const byte ENote = 64;
		private const byte DNote = 62;
		private const byte CNote = 60;
		private const byte HighCNote = 72;
		private const byte FNote = 65;
		private const byte GNote = 67;
		private const byte ANote = 69;
		private const byte ASharpNote = 70;
		private const int Skip = 400;

		private async void JingleBells_Click(object sender, RoutedEventArgs args)
		{
			if (_currentMidiOutputDevice != null)
			{
				await PlayJingleBellsAsync();
			}
		}

		private async void HappyBirthday_Click(object sender, RoutedEventArgs args)
		{
			if (_currentMidiOutputDevice != null)
			{
				await PlayHappyBirthday();
			}
		}

		private async Task PlayJingleBellsAsync()
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

		private async Task PlayHappyBirthday()
		{
			await PlayNoteAsync(CNote, Skip / 2, 127);
			await PlayNoteAsync(CNote, Skip / 2, 127);
			await PlayNoteAsync(DNote);
			await PlayNoteAsync(CNote);
			await PlayNoteAsync(FNote);
			await PlayNoteAsync(ENote, Skip * 2, 127);

			await PlayNoteAsync(CNote, Skip / 2, 127);
			await PlayNoteAsync(CNote, Skip / 2, 127);
			await PlayNoteAsync(DNote);
			await PlayNoteAsync(CNote);
			await PlayNoteAsync(GNote);
			await PlayNoteAsync(FNote, Skip * 2, 127);

			await PlayNoteAsync(CNote, Skip / 2, 127);
			await PlayNoteAsync(CNote, Skip / 2, 127);
			await PlayNoteAsync(HighCNote, Skip * 2, 127);
			await PlayNoteAsync(ANote);
			await PlayNoteAsync(FNote);
			await PlayNoteAsync(ENote);
			await PlayNoteAsync(DNote);

			await PlayNoteAsync(ASharpNote, Skip / 2, 127);
			await PlayNoteAsync(ASharpNote, Skip / 2, 127);
			await PlayNoteAsync(ANote);
			await PlayNoteAsync(FNote);
			await PlayNoteAsync(GNote);
			await PlayNoteAsync(FNote, Skip * 2, 127);
		}

		private async Task PlayNoteAsync(byte noteNumber, int duration = Skip, byte velocity = 127)
		{
			_currentMidiOutputDevice?.SendMessage(new MidiNoteOnMessage(0, noteNumber, velocity));
			await Task.Delay(duration);
			_currentMidiOutputDevice?.SendMessage(new MidiNoteOffMessage(0, noteNumber, velocity));
		}
		#endregion
	}
}
