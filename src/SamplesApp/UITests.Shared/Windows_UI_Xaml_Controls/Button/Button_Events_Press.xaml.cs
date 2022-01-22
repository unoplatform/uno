using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.Button
{
	[SampleControlInfo("Button", "Button_Events_Press", Description = $"Button Event Tests for {nameof(ClickMode)}.{nameof(ClickMode.Press)}"
#if __WASM__
		, IgnoreInSnapshotTests = true
#endif
		)]
	public sealed partial class Button_Events_Press : Page, System.ComponentModel.INotifyPropertyChanged
	{
		public Button_Events_Press()
		{
			this.InitializeComponent();

			PreviousEventCommand = new Windows.UI.Xaml.Input.XamlUICommand
			{
				Label = "Previous Event Test"
			};
			PreviousEventCommand.ExecuteRequested += PreviousEventCommand_ExecuteRequested;
			PreviousEventCommand.CanExecuteRequested += PreviousEventCommand_CanExecuteRequested;

			NextEventCommand = new Windows.UI.Xaml.Input.XamlUICommand
			{
				Label = "Next Event Test"
			};
			NextEventCommand.ExecuteRequested += NextEventCommand_ExecuteRequested;
			NextEventCommand.CanExecuteRequested += NextEventCommand_CanExecuteRequested;

			_viewModels = new System.Collections.Generic.List<ButtonEventsViewModel>();

			var clickMode = ClickMode.Press;
			var dataViewModel = new ButtonEventsDataViewModel("Tapped", "On UWP, TAPPED will be raised when the pointer is both pressed and released over the button and the pointer didn't moved significantly while being pressed. For touch input, the release must occur within approximately 1 second after the press otherwise TAP will not be raised.");
			var testBtn = new Windows.UI.Xaml.Controls.Button() { Content = "ON TAPPED", ClickMode = clickMode };
			testBtn.Tapped += dataViewModel.IncrementCount;
			testBtn.HorizontalAlignment = HorizontalAlignment.Stretch;
			testBtn.VerticalAlignment = VerticalAlignment.Stretch;
			_viewModels.Add(new ButtonEventsViewModel(testBtn, dataViewModel));

			dataViewModel = new ButtonEventsDataViewModel("Double Tapped", "On UWP, DOUBLE TAPPED will be raised on the PRESS right after a rapid press and release on the same control. For UWP the precise amount of time for the press and release depends on the Double-click speed setting in the Windows settings but is slightly less than 1 second at the setting that allows the most time to be considered a double click.");
			testBtn = new Windows.UI.Xaml.Controls.Button() { Content = "ON DOUBLE TAPPED", ClickMode = clickMode };
			testBtn.DoubleTapped += dataViewModel.IncrementCount;
			testBtn.HorizontalAlignment = HorizontalAlignment.Stretch;
			testBtn.VerticalAlignment = VerticalAlignment.Stretch;
			_viewModels.Add(new ButtonEventsViewModel(testBtn, dataViewModel));

			dataViewModel = new ButtonEventsDataViewModel("Click", "On UWP, CLICK will be raised when the pointer is pressed over the button.");
			testBtn = new Windows.UI.Xaml.Controls.Button() { Content = "ON CLICK", ClickMode = clickMode };
			testBtn.Click += dataViewModel.IncrementCount;
			testBtn.HorizontalAlignment = HorizontalAlignment.Stretch;
			testBtn.VerticalAlignment = VerticalAlignment.Stretch;
			_viewModels.Add(new ButtonEventsViewModel(testBtn, dataViewModel));

			dataViewModel = new ButtonEventsDataViewModel("Pointer Pressed", "On UWP, POINTER PRESSED is never raised from a button.");
			testBtn = new Windows.UI.Xaml.Controls.Button() { Content = "ON POINTER PRESSED", ClickMode = clickMode };
			testBtn.PointerPressed += dataViewModel.IncrementCount;
			testBtn.HorizontalAlignment = HorizontalAlignment.Stretch;
			testBtn.VerticalAlignment = VerticalAlignment.Stretch;
			_viewModels.Add(new ButtonEventsViewModel(testBtn, dataViewModel));

			dataViewModel = new ButtonEventsDataViewModel("Pointer Released", "On UWP, POINTER RELEASED is NOT RAISED from a button when the pointer is a MOUSE but IS RAISED when the pointer is TOUCH input and the touch input is both pressed and released over the button and didn't move significantly while being pressed. The touch can be held for any amount of time.");
			testBtn = new Windows.UI.Xaml.Controls.Button() { Content = "ON POINTER RELEASED", ClickMode = clickMode };
			testBtn.PointerReleased += dataViewModel.IncrementCount;
			testBtn.HorizontalAlignment = HorizontalAlignment.Stretch;
			testBtn.VerticalAlignment = VerticalAlignment.Stretch;
			_viewModels.Add(new ButtonEventsViewModel(testBtn, dataViewModel));

			dataViewModel = new ButtonEventsDataViewModel("Pointer Entered", "On UWP, ENTERED is raised when the pointer is entering the button's bounds.");
			testBtn = new Windows.UI.Xaml.Controls.Button() { Content = "ON POINTER ENTERED", ClickMode = clickMode };
			testBtn.PointerEntered += dataViewModel.IncrementCount;
			testBtn.HorizontalAlignment = HorizontalAlignment.Stretch;
			testBtn.VerticalAlignment = VerticalAlignment.Stretch;
			_viewModels.Add(new ButtonEventsViewModel(testBtn, dataViewModel));

			dataViewModel = new ButtonEventsDataViewModel("Pointer Exited", "On UWP, EXITED is raised when the pointer is leaving the button's bounds.");
			testBtn = new Windows.UI.Xaml.Controls.Button() { Content = "ON POINTER EXITED", ClickMode = clickMode };
			testBtn.PointerExited += dataViewModel.IncrementCount;
			testBtn.HorizontalAlignment = HorizontalAlignment.Stretch;
			testBtn.VerticalAlignment = VerticalAlignment.Stretch;
			_viewModels.Add(new ButtonEventsViewModel(testBtn, dataViewModel));

			var timer = new DispatcherTimer() { Interval = new System.TimeSpan(0, 0, 2) };
			testBtn = new Windows.UI.Xaml.Controls.Button() { Content = "ON POINTER CAPTURE LOST", ClickMode = clickMode };
			dataViewModel = new ButtonEventsDataViewModel("Pointer Capture Lost", "On UWP, CAPTURE LOST is raised when an event occurs that causes capture to change to another control or otherwise be released programmatically (press and hold the button for 2+ seconds to trigger this) or when the button is released, whichever occurs first. It will only be raised once regardless of the cause. The main difference between CAPTURE LOST and CLICK is that Click is only raised if the pointer is over the button when released while Capture Lost is raised regardless of the Pointer's location.", timer, testBtn);
			timer.Tick += dataViewModel.TimerTick;
			testBtn.Click += dataViewModel.StartDispatcherTimer;
			testBtn.PointerReleased += dataViewModel.StopDispatcherTimer;
			testBtn.PointerCanceled += dataViewModel.StopDispatcherTimer;
			testBtn.PointerCaptureLost += dataViewModel.IncrementCount;
			testBtn.HorizontalAlignment = HorizontalAlignment.Stretch;
			testBtn.VerticalAlignment = VerticalAlignment.Stretch;
			_viewModels.Add(new ButtonEventsViewModel(testBtn, dataViewModel));

			CurrentViewModel = _viewModels[0];

			Unloaded += (snd, evt) =>
			{
				var vms = _viewModels;
				if (vms != null)
				{
					foreach (var item in vms)
					{
						item.DataViewModel.StopDispatcherTimer(null, null);
					}
				}
			};
		}

		private void PreviousEventCommand_ExecuteRequested(Windows.UI.Xaml.Input.XamlUICommand sender, Windows.UI.Xaml.Input.ExecuteRequestedEventArgs args)
		{
			_currentIndex = System.Math.Max(0, _currentIndex - 1);
			CurrentViewModel = _viewModels[_currentIndex];
			if (_currentIndex == 0)
			{
				sender.NotifyCanExecuteChanged();
			}
			NextEventCommand.NotifyCanExecuteChanged();
		}

		private void PreviousEventCommand_CanExecuteRequested(Windows.UI.Xaml.Input.XamlUICommand sender, Windows.UI.Xaml.Input.CanExecuteRequestedEventArgs args) => args.CanExecute = _currentIndex > 0;

		private void NextEventCommand_ExecuteRequested(Windows.UI.Xaml.Input.XamlUICommand sender, Windows.UI.Xaml.Input.ExecuteRequestedEventArgs args)
		{
			_currentIndex = System.Math.Min(_viewModels.Count - 1, _currentIndex + 1);
			CurrentViewModel = _viewModels[_currentIndex];
			if (_currentIndex == _viewModels.Count - 1)
			{
				sender.NotifyCanExecuteChanged();
			}
			PreviousEventCommand.NotifyCanExecuteChanged();
		}

		private void NextEventCommand_CanExecuteRequested(Windows.UI.Xaml.Input.XamlUICommand sender, Windows.UI.Xaml.Input.CanExecuteRequestedEventArgs args) => args.CanExecute = _currentIndex < _viewModels.Count - 1;

		public Windows.UI.Xaml.Input.XamlUICommand PreviousEventCommand { get; set; }
		public Windows.UI.Xaml.Input.XamlUICommand NextEventCommand { get; set; }

		private ButtonEventsViewModel _currentViewModel;
		public ButtonEventsViewModel CurrentViewModel
		{
			get => _currentViewModel;
			set
			{
				if (_currentViewModel != value)
				{
					_currentViewModel = value;
					RaisePropertyChanged();
				}
			}
		}

		private readonly System.Collections.Generic.List<ButtonEventsViewModel> _viewModels;
		private int _currentIndex;

		public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
		private void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
	}
}
