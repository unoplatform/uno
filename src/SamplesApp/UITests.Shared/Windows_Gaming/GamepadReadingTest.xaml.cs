using System.Linq;
using System.Windows.Input;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Gaming.Input;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UITests.Windows_Gaming
{
	[Sample("Windows.Gaming", Name = "Gamepad_CurrentReading", ViewModelType = typeof(GamepadReadingTestViewModel))]
	public sealed partial class GamepadReadingTest : Page
	{
		public GamepadReadingTest()
		{
			this.InitializeComponent();
			this.DataContextChanged += GamepadReadingTest_DataContextChanged;
		}

		public GamepadReadingTestViewModel ViewModel { get; private set; }

		private void GamepadReadingTest_DataContextChanged(DependencyObject sender, DataContextChangedEventArgs args)
		{
			ViewModel = args.NewValue as GamepadReadingTestViewModel;
		}
	}

	public class GamepadReadingTestViewModel : ViewModelBase
	{
		public GamepadReadingTestViewModel(CoreDispatcher dispatcher) : base(dispatcher)
		{
		}

		public ICommand GetCurrentReadingCommand => GetOrCreateCommand(GetCurrentReading);

		public ulong Timestamp => CurrentReading.Timestamp;

		public GamepadButtons Buttons => CurrentReading.Buttons;

		public double LeftTrigger => CurrentReading.LeftTrigger;

		public double RightTrigger => CurrentReading.RightTrigger;

		public double LeftThumbstickX => CurrentReading.LeftThumbstickX;

		public double LeftThumbstickY => CurrentReading.LeftThumbstickY;

		public double RightThumbstickX => CurrentReading.RightThumbstickX;

		public double RightThumbstickY => CurrentReading.RightThumbstickY;

		public GamepadReading CurrentReading { get; set; }

		private void GetCurrentReading()
		{
			var gamepad = Gamepad.Gamepads.FirstOrDefault();
			if (gamepad != null)
			{				
				CurrentReading = gamepad.GetCurrentReading();
			}
			else
			{
				CurrentReading = new GamepadReading();
			}
			Refresh();
		}

		private void Refresh()
		{
			RaisePropertyChanged(nameof(Timestamp));
			RaisePropertyChanged(nameof(Buttons));
			RaisePropertyChanged(nameof(LeftTrigger));
			RaisePropertyChanged(nameof(RightTrigger));
			RaisePropertyChanged(nameof(LeftThumbstickX));
			RaisePropertyChanged(nameof(LeftThumbstickY));
			RaisePropertyChanged(nameof(RightThumbstickX));
			RaisePropertyChanged(nameof(RightThumbstickY));
		}
	}
}
