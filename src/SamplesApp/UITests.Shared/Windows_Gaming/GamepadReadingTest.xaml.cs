using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Uno.Disposables;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Gaming.Input;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Private.Infrastructure;

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

		internal GamepadReadingTestViewModel ViewModel { get; private set; }

		private void GamepadReadingTest_DataContextChanged(DependencyObject sender, DataContextChangedEventArgs args)
		{
			ViewModel = args.NewValue as GamepadReadingTestViewModel;
		}
	}

	internal class GamepadReadingTestViewModel : ViewModelBase
	{
		private bool _isCheckingAutomatically = false;

		private DispatcherTimer _checkTimer = new DispatcherTimer()
		{
			Interval = TimeSpan.FromMilliseconds(100)
		};

		public GamepadReadingTestViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
			_checkTimer.Tick += CheckTimer_Tick;
			Disposables.Add(Disposable.Create(() =>
			{
				_checkTimer.Stop();
			}));
		}

		public ObservableCollection<GamepadReadingViewModel> Gamepads { get; } = new ObservableCollection<GamepadReadingViewModel>();

		public ICommand UpdateReadingsCommand => GetOrCreateCommand(() =>
		{
			UpdateReadings();
		});

		public ICommand UpdateGamepadsCommand => GetOrCreateCommand(async () =>
		{
			await UpdateGamepadsAsync();
		});

		private void CheckTimer_Tick(object sender, object e)
		{
			UpdateReadings();
		}

		private void UpdateReadings()
		{
			foreach (var gamepad in Gamepads)
			{
				gamepad.Update();
			}
		}

		public bool IsCheckingAutomatically
		{
			get => _isCheckingAutomatically;
			set
			{
				_isCheckingAutomatically = value;
				if (_isCheckingAutomatically)
				{
					_checkTimer.Start();
				}
				else
				{
					_checkTimer.Stop();
				}
			}
		}


		private async Task UpdateGamepadsAsync()
		{
			await Dispatcher.RunAsync(UnitTestDispatcherCompat.Priority.Normal, () =>
			{
				var gamepads = Gamepad.Gamepads.ToArray();

				var existingGamepads = new HashSet<Gamepad>(Gamepads.Select(g => g.Gamepad));

				var toRemove = existingGamepads.Except(gamepads).ToArray();
				var toAdd = gamepads.Except(existingGamepads).ToArray();

				foreach (var gamepad in toRemove)
				{
					var vmToRemove = Gamepads.FirstOrDefault(g => g.Gamepad == gamepad);
					Gamepads.Remove(vmToRemove);
				}

				foreach (var gamepad in toAdd)
				{
					var vmToAdd = new GamepadReadingViewModel(gamepad);
					Gamepads.Add(vmToAdd);
				}
			});
		}
	}
}
