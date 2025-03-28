#nullable enable

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Devices.Haptics;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace UITests.Windows_Devices.Haptics
{
	[Sample("Windows.Devices", Name = "Haptics.VibrationDevice", ViewModelType = typeof(VibrationDeviceTestsViewModel))]
	public sealed partial class VibrationDeviceTests : Page
	{
		public VibrationDeviceTests()
		{
			this.InitializeComponent();
			this.Loaded += VibrationDeviceTests_Loaded;
			this.DataContextChanged += VibrationDeviceTests_DataContextChanged;
		}

		private async void VibrationDeviceTests_Loaded(object sender, RoutedEventArgs e)
		{
			await Model!.LoadAsync();
		}

		private void VibrationDeviceTests_DataContextChanged(DependencyObject sender, DataContextChangedEventArgs args)
		{
			Model = args.NewValue as VibrationDeviceTestsViewModel;
		}

		internal VibrationDeviceTestsViewModel? Model { get; private set; }
	}

	internal class VibrationDeviceTestsViewModel : ViewModelBase
	{
		private string _status = string.Empty;
		private bool _isAvailable;

		private SimpleHapticsController? _simpleHapticsController;

		public VibrationDeviceTestsViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
		}

		public async Task LoadAsync()
		{
			var result = await VibrationDevice.RequestAccessAsync();
			if (result == VibrationAccessStatus.Allowed)
			{
				var vibrationDevice = await VibrationDevice.GetDefaultAsync();
				if (vibrationDevice != null)
				{
					_simpleHapticsController = vibrationDevice.SimpleHapticsController;

					Status = "VibrationDevice is available";
					IsAvailable = true;
					return;
				}
			}

			Status = $"VibrationDevice is not available ({result})";
		}

		public string Status
		{
			get => _status;
			set
			{
				_status = value;
				RaisePropertyChanged();
			}
		}

		public bool IsAvailable
		{
			get => _isAvailable;
			set
			{
				_isAvailable = value;
				RaisePropertyChanged();
			}
		}

		public ICommand PressCommand => GetOrCreateCommand(Press);

		public ICommand ClickCommand => GetOrCreateCommand(Click);

		private void Press() => RunHapticFeedback(KnownSimpleHapticsControllerWaveforms.Press);

		private void Click() => RunHapticFeedback(KnownSimpleHapticsControllerWaveforms.Click);

		private void RunHapticFeedback(ushort waveform)
		{
			var feedback = FindFeedback(_simpleHapticsController!, waveform);
			if (feedback != null)
			{
				_simpleHapticsController!.SendHapticFeedback(feedback);
			}
			else
			{
				Status = "Feedback type not supported";
			}
		}

		private static SimpleHapticsControllerFeedback? FindFeedback(SimpleHapticsController controller, ushort type) =>
			controller.SupportedFeedback.FirstOrDefault(feedback => feedback.Waveform == type);
	}
}
