using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Gaming.Input;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace UITests.Windows_Gaming
{
	[Sample("Windows.Gaming", Name = "Gamepad_Enumeration", ViewModelType = typeof(GamepadEnumerationViewModel))]
	public sealed partial class GamepadEnumerationTest : Page
	{
		public GamepadEnumerationTest()
		{
			this.InitializeComponent();
			this.DataContextChanged += GamepadEnumerationTest_DataContextChanged;
		}

		private void GamepadEnumerationTest_DataContextChanged(DependencyObject sender, DataContextChangedEventArgs args)
		{
			Model = args.NewValue as GamepadEnumerationViewModel;
		}

		internal GamepadEnumerationViewModel Model { get; private set; }
	}

	internal class GamepadEnumerationViewModel : ViewModelBase
	{
		private bool _isObservingGamepadAdded;
		private bool _isObservingGamepadRemoved;

		private string _gamepadAddedInvokeInfo = "Not triggered yet.";
		private string _gamepadRemovedInvokeInfo = "Not triggered yet.";

		public GamepadEnumerationViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher)
			: base(dispatcher)
		{
		}

		public ICommand RefreshGamepadCountCommand =>
			GetOrCreateCommand(RefreshGamepadCount);

		private void RefreshGamepadCount()
		{
			GamepadCount = Gamepad.Gamepads.Count;
			RaisePropertyChanged(nameof(GamepadCount));
		}

		public bool IsObservingGamepadAdded
		{
			get => _isObservingGamepadAdded;
			set
			{
				_isObservingGamepadAdded = value;
				RaisePropertyChanged();
				UpdateGamepadAdded();
			}
		}

		public bool IsObservingGamepadRemoved
		{
			get => _isObservingGamepadRemoved;
			set
			{
				_isObservingGamepadRemoved = value;
				RaisePropertyChanged();
				UpdateGamepadRemoved();
			}
		}

		public int GamepadCount { get; private set; }

		public string GamepadAddedInvokeInfo
		{
			get => _gamepadAddedInvokeInfo;
			private set
			{
				_gamepadAddedInvokeInfo = value;
				RaisePropertyChanged();
			}
		}
		public string GamepadRemovedInvokeInfo
		{
			get => _gamepadRemovedInvokeInfo;
			private set
			{
				_gamepadRemovedInvokeInfo = value;
				RaisePropertyChanged();
			}
		}
		private void UpdateGamepadAdded()
		{
			if (IsObservingGamepadAdded)
			{
				Gamepad.GamepadAdded += Gamepad_GamepadAdded;
				GamepadAddedInvokeInfo = "Gamepad added subscribed.";
			}
			else
			{
				Gamepad.GamepadAdded -= Gamepad_GamepadAdded;
				GamepadAddedInvokeInfo = "Gamepad added unsubscribed.";
			}
		}

		private void Gamepad_GamepadAdded(object sender, Gamepad e)
		{
			GamepadAddedInvokeInfo = $"Gamepad added at {DateTimeOffset.UtcNow}";
			RefreshGamepadCount();
		}

		private void UpdateGamepadRemoved()
		{
			if (IsObservingGamepadRemoved)
			{
				Gamepad.GamepadRemoved += Gamepad_GamepadRemoved;
				GamepadRemovedInvokeInfo = "Gamepad removed subscribed.";
			}
			else
			{
				Gamepad.GamepadRemoved -= Gamepad_GamepadRemoved;
				GamepadRemovedInvokeInfo = "Gamepad removed unsubscribed.";
			}
		}

		private void Gamepad_GamepadRemoved(object sender, Gamepad e)
		{
			GamepadRemovedInvokeInfo = $"Gamepad removed at {DateTimeOffset.UtcNow}";
			RefreshGamepadCount();
		}
	}
}
