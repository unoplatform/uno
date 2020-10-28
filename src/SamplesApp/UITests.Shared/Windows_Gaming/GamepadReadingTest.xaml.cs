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
			RaisePropertyChanged(nameof(CurrentReading));
		}
	}
}
