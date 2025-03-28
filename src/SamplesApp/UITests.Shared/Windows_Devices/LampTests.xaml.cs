using System;
using System.Windows.Input;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Devices.Lights;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

using ICommand = System.Windows.Input.ICommand;
using EventHandler = System.EventHandler;

namespace UITests.Shared.Windows_Devices
{
	[Sample(
		"Windows.Devices",
		Name = "Lamp",
		Description = "Demonstrates the Windows.Devices.Lights.Lamp",
		ViewModelType = typeof(LampTestsViewModel),
		IgnoreInSnapshotTests = true)]
	public sealed partial class LampTests : Page
	{
		public LampTests()
		{
			this.InitializeComponent();
		}
	}

	internal class LampTestsViewModel : ViewModelBase
	{
		private Lamp _lamp = null;
		private string _lampStatus;

		public LampTestsViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) :
			base(dispatcher)
		{
		}

		public ICommand GetLampCommand => GetOrCreateCommand(GetLampAsync);

		public ICommand ToggleCommand => GetOrCreateCommand(ToggleLamp);

		public string LampStatus
		{
			get => _lampStatus;
			private set
			{
				_lampStatus = value;
				RaisePropertyChanged();
			}
		}

		public bool IsAvailable => _lamp != null;

		public bool IsEnabled
		{
			get
			{
				return _lamp?.IsEnabled ?? false;
			}
			set
			{
				if (_lamp != null)
				{
					_lamp.IsEnabled = value;
				}
				RaisePropertyChanged();
			}
		}

		public float BrightnessLevel
		{
			get => _lamp?.BrightnessLevel ?? 0.0f;
			set
			{
				if (_lamp != null)
				{
					_lamp.BrightnessLevel = value;
				}
			}
		}

		private async void GetLampAsync()
		{
			_lamp = await Lamp.GetDefaultAsync();
			if (_lamp == null)
			{
				LampStatus = "Lamp is not available";
			}
			else
			{
				LampStatus = "Lamp retrieved";
				Disposables.Add(_lamp);
			}
			RaisePropertyChanged(nameof(IsAvailable));
			RaisePropertyChanged(nameof(IsEnabled));
			RaisePropertyChanged(nameof(BrightnessLevel));
		}

		private void ToggleLamp()
		{
			_lamp.IsEnabled = !_lamp.IsEnabled;
			RaisePropertyChanged(nameof(IsEnabled));
		}
	}
}
