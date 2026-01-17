using System;
using Uno.Disposables;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Devices.Sensors;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Private.Infrastructure;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_Devices
{
	[Sample("Windows.Devices", Name = "Gyrometer", ViewModelType = typeof(GyrometerTestsViewModel), IgnoreInSnapshotTests = true, Description = "Demonstrates use of Windows.Devices.Sensors.Gyrometer")]
	public sealed partial class GyrometerTests : UserControl
	{
		public GyrometerTests()
		{
			this.InitializeComponent();
		}
	}

	[Bindable]
	internal class GyrometerTestsViewModel : ViewModelBase
	{
		private readonly Gyrometer _gyrometer = null;
		private bool _readingChangedAttached;
		private string _sensorStatus;
		private double _angularVelocityX;
		private double _angularVelocityY;
		private double _angularVelocityZ;
		private string _timestamp;

		public GyrometerTestsViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
			_gyrometer = Gyrometer.GetDefault();
			if (_gyrometer != null)
			{
				_gyrometer.ReportInterval = 250;
				SensorStatus = "Gyrometer created";
			}
			else
			{
				SensorStatus = "Gyrometer not available on this device";
			}
			Disposables.Add(Disposable.Create(() =>
			{
				if (_gyrometer != null)
				{
					_gyrometer.ReadingChanged -= Gyrometer_ReadingChanged;
				}
			}));
		}

		public Command AttachReadingChangedCommand => new Command((p) =>
		{
			_gyrometer.ReadingChanged += Gyrometer_ReadingChanged;
			ReadingChangedAttached = true;
		});

		public Command DetachReadingChangedCommand => new Command((p) =>
		{
			_gyrometer.ReadingChanged -= Gyrometer_ReadingChanged;
			ReadingChangedAttached = false;
		});

		public bool GyrometerAvailable => _gyrometer != null;

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

		public double AngularVelocityX
		{
			get => _angularVelocityX;
			private set
			{
				_angularVelocityX = value;
				RaisePropertyChanged();
			}
		}

		public double AngularVelocityY
		{
			get => _angularVelocityY;
			private set
			{
				_angularVelocityY = value;
				RaisePropertyChanged();
			}
		}

		public double AngularVelocityZ
		{
			get => _angularVelocityZ;
			private set
			{
				_angularVelocityZ = value;
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

		private async void Gyrometer_ReadingChanged(Gyrometer sender, GyrometerReadingChangedEventArgs args)
		{
			await Dispatcher.RunAsync(UnitTestDispatcherCompat.Priority.Normal, () =>
			{
				AngularVelocityX = args.Reading.AngularVelocityX;
				AngularVelocityY = args.Reading.AngularVelocityY;
				AngularVelocityZ = args.Reading.AngularVelocityZ;
				Timestamp = args.Reading.Timestamp.ToString("R");
			});
		}
	}
}
