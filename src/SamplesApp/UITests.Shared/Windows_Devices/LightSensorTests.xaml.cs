using System;
using Uno.Disposables;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Devices.Sensors;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Private.Infrastructure;

namespace UITests.Windows_Devices
{
	[Sample("Windows.Devices", Name = "LightSensor", ViewModelType = typeof(LightSensorTestsViewModel), IgnoreInSnapshotTests = true, Description = "Demonstrates use of Windows.Devices.Sensors.LightSensor")]
	public sealed partial class LightSensorTests : Page
	{
		public LightSensorTests() => InitializeComponent();
	}

	[Bindable]
	internal class LightSensorTestsViewModel : ViewModelBase
	{
		private readonly LightSensor _LightSensor = null;
		private bool _readingChangedAttached;
		private string _sensorStatus;
		private float _illuminance;
		private string _timestamp;

		public LightSensorTestsViewModel(UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
			_LightSensor = LightSensor.GetDefault();
			if (_LightSensor != null)
			{
				_LightSensor.ReportInterval = 250;
				SensorStatus = "LightSensor created";
			}
			else
			{
				SensorStatus = "LightSensor not available on this device";
			}
			Disposables.Add(Disposable.Create(() =>
			{
				if (_LightSensor != null)
				{
					_LightSensor.ReadingChanged -= LightSensor_ReadingChanged;
				}
			}));
		}

		public Command AttachReadingChangedCommand => new Command((p) =>
		{
			_LightSensor.ReadingChanged += LightSensor_ReadingChanged;
			ReadingChangedAttached = true;
		});

		public Command DetachReadingChangedCommand => new Command((p) =>
		{
			_LightSensor.ReadingChanged -= LightSensor_ReadingChanged;
			ReadingChangedAttached = false;
		});

		public bool LightSensorAvailable => _LightSensor != null;

		public string SensorStatus
		{
			get => _sensorStatus;
			private set
			{
				_sensorStatus = value;
				RaisePropertyChanged();
			}
		}

		public bool ReadingChangedAttached
		{
			get => _readingChangedAttached;
			set
			{
				_readingChangedAttached = value;
				RaisePropertyChanged();
			}
		}

		public float Illuminance
		{
			get => _illuminance;
			private set
			{
				_illuminance = value;
				RaisePropertyChanged();
			}
		}

		public string Timestamp
		{
			get => _timestamp;
			private set
			{
				_timestamp = value;
				RaisePropertyChanged();
			}
		}

		private async void LightSensor_ReadingChanged(LightSensor sender, LightSensorReadingChangedEventArgs args)
		{
			await Dispatcher.RunAsync(UnitTestDispatcherCompat.Priority.Normal, () =>
			{
				Illuminance = args.Reading.IlluminanceInLux;
				Timestamp = args.Reading.Timestamp.ToString("R");
			});
		}
	}
}
