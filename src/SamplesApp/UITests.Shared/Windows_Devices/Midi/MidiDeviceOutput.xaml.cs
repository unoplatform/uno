using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Uno.Disposables;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Devices.Enumeration;
using Windows.Devices.Midi;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_Devices.Midi
{
	[SampleControlInfo("Windows.Devices", "Midi_DeviceOutput", description: "Output to a connected MIDI device")]
	public sealed partial class MidiDeviceOutputTests : UserControl
	{
		public MidiDeviceOutputTests()
		{
			this.InitializeComponent();
			mainGrid.DataContext = this;
			// Initialize the list of active MIDI output devices
			_midiOutPorts = new List<IMidiOutPort>();

			// Set up the MIDI output device watcher
			_midiOutDeviceWatcher = new MidiDeviceWatcher(
				MidiOutPort.GetDeviceSelector(),
				Dispatcher,
				OutputDevices);

			// Start watching for devices
			_midiOutDeviceWatcher.Start();

			// Populate message types into list
			PopulateMessageTypes();

			this.Unloaded += MidiDeviceOutputTests_Unloaded;
		}

		/// <summary>
		/// Collection of active MidiOutPorts
		/// </summary>
		private List<IMidiOutPort> _midiOutPorts;

		/// <summary>
		/// Device watcher for MIDI out ports
		/// </summary>
		private MidiDeviceWatcher _midiOutDeviceWatcher;

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

		private string _userMessage = "";

		private void MidiDeviceOutputTests_Unloaded(object sender, RoutedEventArgs e)
		{
			// Stop the output device watcher
			_midiOutDeviceWatcher.Stop();

			// Close all MidiOutPorts
			foreach (var outPort in this._midiOutPorts)
			{
				outPort.Dispose();
			}
			_midiOutPorts.Clear();
		}

		public ObservableCollection<string> OutputDevices { get; } = new ObservableCollection<string>();

		public ObservableCollection<string> MessageTypes { get; } = new ObservableCollection<string>();

		public string UserMessage
		{
			get => _userMessage;
			set
			{
				_userMessage = value;
			}
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
			this.messageType.Items.Clear();

			// Add the message types to the list
			foreach (var messageType in _messageTypes)
			{
				this.messageType.Items.Add(messageType.Value);
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
			int selectedOutputDeviceIndex = this.outputDevices.SelectedIndex;

			// Try to create a MidiOutPort
			if (selectedOutputDeviceIndex < 0)
			{
				UserMessage = "Select a MIDI output device to be able to send messages to it";
				return;
			}

			var devInfoCollection = _midiOutDeviceWatcher.GetDeviceInformationCollection();
			if (devInfoCollection == null)
			{
				UserMessage = "Device not found!";
				return;
			}

			var devInfo = devInfoCollection[selectedOutputDeviceIndex];
			if (devInfo == null)
			{
				UserMessage = "Device not found!";
				return;
			}

			_currentMidiOutputDevice = await MidiOutPort.FromIdAsync(devInfo.Id);
			if (_currentMidiOutputDevice == null)
			{
				UserMessage = "Unable to create MidiOutPort from output device";
				return;
			}

			// We have successfully created a MidiOutPort; add the device to the list of active devices
			if (!_midiOutPorts.Contains(_currentMidiOutputDevice))
			{
				_midiOutPorts.Add(_currentMidiOutputDevice);
			}

			// Enable message type list & reset button
			this.messageType.IsEnabled = true;
			this.resetButton.IsEnabled = true;

			UserMessage = "Output Device selected successfully! Waiting for message type selection...";
		}

		/// <summary>
		/// Reset all input fields, including message type
		/// </summary>
		/// <param name="sender">Element that fired the event</param>
		/// <param name="e">Event arguments</param>
		private void resetButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
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
				this.messageType.SelectedIndex = -1;
				this._currentMessageType = MidiMessageType.None;
			}

			// Ensure the message type list and reset button are enabled
			this.messageType.IsEnabled = true;
			this.resetButton.IsEnabled = true;

			// Reset selections on parameters
			this.parameter1.SelectedIndex = -1;
			this.parameter2.SelectedIndex = -1;
			this.parameter3.SelectedIndex = -1;

			// New selection values will cause parameter boxes to be hidden and disabled
			UpdateParameterList1();
			UpdateParameterList2();
			UpdateParameterList3();

			// Disable send button & hide/clear the SysEx buffer text
			this.sendButton.IsEnabled = false;
			this.rawBufferHeader.Visibility = Visibility.Collapsed;
			this.sysExMessageContent.Text = "";
			this.sysExMessageContent.Visibility = Visibility.Collapsed;
		}

		/// <summary>
		/// Create a new MIDI message based on the message type and parameter(s) values,
		/// and send it to the chosen output device
		/// </summary>
		/// <param name="sender">Element that fired the event</param>
		/// <param name="e">Event arguments</param>
		private void sendButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			IMidiMessage midiMessageToSend = null;

			switch (this._currentMessageType)
			{
				case MidiMessageType.NoteOff:
					midiMessageToSend = new MidiNoteOffMessage(Convert.ToByte(this.parameter1.SelectedValue), Convert.ToByte(this.parameter2.SelectedValue), Convert.ToByte(this.parameter3.SelectedValue));
					break;
				case MidiMessageType.NoteOn:
					midiMessageToSend = new MidiNoteOnMessage(Convert.ToByte(this.parameter1.SelectedValue), Convert.ToByte(this.parameter2.SelectedValue), Convert.ToByte(this.parameter3.SelectedValue));
					break;
				case MidiMessageType.PolyphonicKeyPressure:
					midiMessageToSend = new MidiPolyphonicKeyPressureMessage(Convert.ToByte(this.parameter1.SelectedValue), Convert.ToByte(this.parameter2.SelectedValue), Convert.ToByte(this.parameter3.SelectedValue));
					break;
				case MidiMessageType.ControlChange:
					midiMessageToSend = new MidiControlChangeMessage(Convert.ToByte(this.parameter1.SelectedValue), Convert.ToByte(this.parameter2.SelectedValue), Convert.ToByte(this.parameter3.SelectedValue));
					break;
				case MidiMessageType.ProgramChange:
					midiMessageToSend = new MidiProgramChangeMessage(Convert.ToByte(this.parameter1.SelectedValue), Convert.ToByte(this.parameter2.SelectedValue));
					break;
				case MidiMessageType.ChannelPressure:
					midiMessageToSend = new MidiChannelPressureMessage(Convert.ToByte(this.parameter1.SelectedValue), Convert.ToByte(this.parameter2.SelectedValue));
					break;
				case MidiMessageType.PitchBendChange:
					midiMessageToSend = new MidiPitchBendChangeMessage(Convert.ToByte(this.parameter1.SelectedValue), Convert.ToUInt16(this.parameter2.SelectedValue));
					break;
				case MidiMessageType.SystemExclusive:
					var dataWriter = new DataWriter();
					var sysExMessage = this.sysExMessageContent.Text;
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
					midiMessageToSend = new MidiTimeCodeMessage(Convert.ToByte(this.parameter1.SelectedValue), Convert.ToByte(this.parameter2.SelectedValue));
					break;
				case MidiMessageType.SongPositionPointer:
					midiMessageToSend = new MidiSongPositionPointerMessage(Convert.ToUInt16(this.parameter1.SelectedValue));
					break;
				case MidiMessageType.SongSelect:
					midiMessageToSend = new MidiSongSelectMessage(Convert.ToByte(this.parameter1.SelectedValue));
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
			this._currentMidiOutputDevice.SendMessage(midiMessageToSend);
			UserMessage = "Message sent successfully";
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
			int messageTypeSelectedIndex = this.messageType.SelectedIndex;

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
					this._currentMessageType = messageType.Key;
					break;
				}
				count++;
			}

			// Some MIDI message types don't need additional parameters
			// For them, show the Send button as soon as user selects message type from the list
			switch (this._currentMessageType)
			{
				// SysEx messages need to be in a particular format
				case MidiMessageType.SystemExclusive:
					this.rawBufferHeader.Visibility = Visibility.Visible;
					this.sysExMessageContent.Visibility = Visibility.Visible;
					// Provide start (0xF0) and end (0xF7) of SysEx values
					sysExMessageContent.Text = "F0 F7";
					// Let the user know the expected format of the message
					UserMessage = "Expecting a string of format 'NN NN NN NN....', where NN is a byte in hex";
					this.sendButton.IsEnabled = true;
					break;

				// These messages do not need additional parameters
				case MidiMessageType.ActiveSensing:
				case MidiMessageType.Continue:
				case MidiMessageType.Start:
				case MidiMessageType.Stop:
				case MidiMessageType.SystemReset:
				case MidiMessageType.TimingClock:
				case MidiMessageType.TuneRequest:
					this.sendButton.IsEnabled = true;
					break;

				default:
					this.sendButton.IsEnabled = false;
					break;
			}

			// Update the first parameter list depending on the MIDI message type
			// If no further parameters are required, the list is emptied and hidden
			UpdateParameterList1();
		}

		/// <summary>
		/// For MIDI message types that need the first parameter, populate the list
		/// based on the message type. For message types that don't need the first
		/// parameter, empty and hide it
		/// </summary>
		private void UpdateParameterList1()
		{
			// The first parameter is different for different message types
			switch (this._currentMessageType)
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
					PopulateParameterList(this.parameter1, 16, "Channel");
					break;

				case MidiMessageType.MidiTimeCode:
					// This list is for further Message Types, of which there are 8
					PopulateParameterList(this.parameter1, 8, "Message Type");
					break;

				case MidiMessageType.SongPositionPointer:
					// This list is for Beats, of which there are 16384
					PopulateParameterList(this.parameter1, 16384, "Beats");
					break;

				case MidiMessageType.SongSelect:
					// This list is for Songs, of which there are 128
					PopulateParameterList(this.parameter1, 128, "Song");
					break;

				case MidiMessageType.SystemExclusive:
					// Start with a clean slate
					this.parameter1.Items.Clear();

					// Hide the first parameter
					this.parameter1.Header = "";
					this.parameter1.IsEnabled = false;
					this.parameter1.Visibility = Visibility.Collapsed;
					UserMessage = "Please edit the message in the textbox by clicking on 'F0 F7'";
					break;

				default:
					// Start with a clean slate
					this.parameter1.Items.Clear();

					// Hide the first parameter
					this.parameter1.Header = "";
					this.parameter1.IsEnabled = false;
					this.parameter1.Visibility = Visibility.Collapsed;
					UserMessage = "";
					break;
			}
		}

		/// <summary>
		/// React to Parameter1 selection change as appropriate
		/// </summary>
		/// <param name="sender">Element that fired the event</param>
		/// <param name="e">Event arguments</param>
		private void Parameter1_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Find the index of the user's choice
			int parameter1SelectedIndex = this.parameter1.SelectedIndex;

			// Some MIDI message types don't need additional parameters past parameter 1
			// For them, show the Send button as soon as user selects parameter 1 value from the list
			switch (this._currentMessageType)
			{
				case MidiMessageType.SongPositionPointer:
				case MidiMessageType.SongSelect:

					if (parameter1SelectedIndex != -1)
					{
						this.sendButton.IsEnabled = true;
					}
					break;

				default:
					this.sendButton.IsEnabled = false;
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
			// Do not proceed if Parameter 1 is not chosen
			if (this.parameter1.SelectedIndex == -1)
			{
				this.parameter2.Items.Clear();
				this.parameter2.Header = "";
				this.parameter2.IsEnabled = false;
				this.parameter2.Visibility = Visibility.Collapsed;

				return;
			}

			switch (this._currentMessageType)
			{
				case MidiMessageType.NoteOff:
				case MidiMessageType.NoteOn:
				case MidiMessageType.PolyphonicKeyPressure:
					// This list is for Notes, of which there are 128
					PopulateParameterList(this.parameter2, 128, "Note");
					break;

				case MidiMessageType.ControlChange:
					// This list is for Controllers, of which there are 128
					PopulateParameterList(this.parameter2, 128, "Controller");
					break;

				case MidiMessageType.ProgramChange:
					// This list is for Program Numbers, of which there are 128
					PopulateParameterList(this.parameter2, 128, "Program Number");
					break;

				case MidiMessageType.ChannelPressure:
					// This list is for Pressure Values, of which there are 128
					PopulateParameterList(this.parameter2, 128, "Pressure Value");
					break;

				case MidiMessageType.PitchBendChange:
					// This list is for Pitch Bend Values, of which there are 16384
					PopulateParameterList(this.parameter2, 16384, "Pitch Bend Value");
					break;

				case MidiMessageType.MidiTimeCode:
					// This list is for Values, of which there are 16
					PopulateParameterList(this.parameter2, 16, "Value");
					break;

				default:
					// Start with a clean slate
					this.parameter2.Items.Clear();

					// Hide the first parameter
					this.parameter2.Header = "";
					this.parameter2.IsEnabled = false;
					this.parameter2.Visibility = Visibility.Collapsed;
					break;
			}
		}

		/// <summary>
		/// React to Parameter2 selection change as appropriate
		/// </summary>
		/// <param name="sender">Element that fired the event</param>
		/// <param name="e">Event arguments</param>
		private void Parameter2_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Find the index of the user's choice
			int parameter2SelectedIndex = this.parameter2.SelectedIndex;

			// Some MIDI message types don't need additional parameters past parameter 2
			// For them, show the Send button as soon as user selects parameter 2 value from the list
			switch (this._currentMessageType)
			{
				case MidiMessageType.ProgramChange:
				case MidiMessageType.ChannelPressure:
				case MidiMessageType.PitchBendChange:
				case MidiMessageType.MidiTimeCode:

					if (parameter2SelectedIndex != -1)
					{
						this.sendButton.IsEnabled = true;
					}
					break;

				default:
					this.sendButton.IsEnabled = false;
					break;
			}

			// Update the third parameter list depending on the second parameter selection
			// If no further parameters are required, the list is emptied and hidden
			UpdateParameterList3();
		}

		/// <summary>
		/// For MIDI message types that need the third parameter, populate the list
		/// based on the message type. For message types that don't need the third
		/// parameter, empty and hide it
		/// </summary>
		private void UpdateParameterList3()
		{
			// Do not proceed if Parameter 2 is not chosen
			if (this.parameter2.SelectedIndex == -1)
			{
				this.parameter3.Items.Clear();
				this.parameter3.Header = "";
				this.parameter3.IsEnabled = false;
				this.parameter3.Visibility = Visibility.Collapsed;

				return;
			}

			switch (this._currentMessageType)
			{
				case MidiMessageType.NoteOff:
				case MidiMessageType.NoteOn:
					// This list is for Velocity Values, of which there are 128
					PopulateParameterList(this.parameter3, 128, "Velocity");
					break;

				case MidiMessageType.PolyphonicKeyPressure:
					// This list is for Pressure Values, of which there are 128
					PopulateParameterList(this.parameter3, 128, "Pressure");
					break;

				case MidiMessageType.ControlChange:
					// This list is for Values, of which there are 128
					PopulateParameterList(this.parameter3, 128, "Value");
					break;

				default:
					// Start with a clean slate
					this.parameter3.Items.Clear();

					// Hide the first parameter
					this.parameter3.Header = "";
					this.parameter3.IsEnabled = false;
					this.parameter3.Visibility = Visibility.Collapsed;
					break;
			}
		}

		/// <summary>
		/// React to Parameter3 selection change as appropriate
		/// </summary>
		/// <param name="sender">Element that fired the event</param>
		/// <param name="e">Event arguments</param>
		private void Parameter3_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Find the index of the user's choice
			int parameter3SelectedIndex = this.parameter3.SelectedIndex;

			// The last set of MIDI message types don't need additional parameters
			// For them, show the Send button as soon as user selects parameter 3 value from the list
			// Set default to disable Send button for any message types that fall through
			switch (this._currentMessageType)
			{
				case MidiMessageType.NoteOff:
				case MidiMessageType.NoteOn:
				case MidiMessageType.PolyphonicKeyPressure:
				case MidiMessageType.ControlChange:

					if (parameter3SelectedIndex != -1)
					{
						this.sendButton.IsEnabled = true;
					}
					break;

				default:
					this.sendButton.IsEnabled = false;
					break;
			}
		}

		/// <summary>
		/// Helper function to populate a dropdown lists with options
		/// </summary>
		/// <param name="list">The parameter list to populate</param>
		/// <param name="numberOfOptions">Number of options in the list</param>
		/// <param name="listName">The header to display to the user</param>
		private void PopulateParameterList(ComboBox list, int numberOfOptions, string listName)
		{
			// Start with a clean slate
			list.Items.Clear();

			// Add the options to the list
			for (int i = 0; i < numberOfOptions; i++)
			{
				list.Items.Add(i);
			}

			// Show the list, so that the user can make the next choice
			list.Header = listName;
			list.IsEnabled = true;
			list.Visibility = Visibility.Visible;
		}
	}
}
