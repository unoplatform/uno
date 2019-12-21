using System.Collections.ObjectModel;
using Uno.Disposables;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
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

		public MidiDeviceEnumerationTestsViewModel(CoreDispatcher dispatcher) : base(dispatcher)
		{
			_midiInDeviceWatcher = new MidiDeviceWatcher(MidiInPort.GetDeviceSelector(), dispatcher, MidiInDevices);

			_midiInDeviceWatcher.Start();

			Disposables.Add(Disposable.Create(() =>
			{
				_midiInDeviceWatcher.Stop();
			}));
		}

		public ObservableCollection<string> MidiInDevices { get; } = new ObservableCollection<string>();
	}
}
