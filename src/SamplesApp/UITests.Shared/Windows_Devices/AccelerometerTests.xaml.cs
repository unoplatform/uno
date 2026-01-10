using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Devices.Sensors;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Core;
using Uno.UI.Samples.Controls;
using Uno.Disposables;
using Private.Infrastructure;

namespace UITests.Shared.Windows_Devices
{
	[Sample("Windows.Devices", Name = "Accelerometer", ViewModelType = typeof(AccelerometerTestsViewModel), IgnoreInSnapshotTests = true, Description = "Demonstrates use of Windows.Devices.Sensors.Accelerometer")]
	public sealed partial class AccelerometerTests : UserControl
	{

		public AccelerometerTests()
		{
			this.InitializeComponent();
		}
	}

	[Bindable]
	internal class AccelerometerTestsViewModel : ViewModelBase
	{
		private readonly Accelerometer _accelerometer = null;
		private bool _readingChangedAttached;
		private bool _shakenAttached;
		private double _accelerationX;
		private double _accelerationY;
		private double _accelerationZ;
		private string _readingTimestamp;
		private string _shakenTimestamp;
		private string _sensorStatus;

		public AccelerometerTestsViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
			_accelerometer = Accelerometer.GetDefault();
			if (_accelerometer != null)
			{
				_accelerometer.ReportInterval = 250;
				SensorStatus = "Accelerometer created";
			}
			else
			{
				SensorStatus = "Accelerometer not available on this device";
			}
			Disposables.Add(Disposable.Create(() =>
			{
				if (_accelerometer != null)
				{
					_accelerometer.ReadingChanged -= Accelerometer_ReadingChanged;
					_accelerometer.Shaken -= Accelerometer_Shaken;
				}
			}));
		}

		public Command AttachReadingChangedCommand => new Command((p) =>
		{
			_accelerometer.ReadingChanged += Accelerometer_ReadingChanged;
			ReadingChangedAttached = true;
		});

		public Command DetachReadingChangedCommand => new Command((p) =>
		{
			_accelerometer.ReadingChanged -= Accelerometer_ReadingChanged;
			ReadingChangedAttached = false;
		});

		public Command AttachShakenCommand => new Command((p) =>
		{
			_accelerometer.Shaken += Accelerometer_Shaken;
			ShakenAttached = true;
		});

		public Command DetachShakenCommand => new Command((p) =>
		{
			_accelerometer.Shaken -= Accelerometer_Shaken;
			ShakenAttached = false;
		});

		public bool AccelerometerAvailable => _accelerometer != null;

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

		public bool ShakenAttached
		{
			get => _shakenAttached;
			set
			{
				_shakenAttached = value;
				RaisePropertyChanged();
			}
		}

		public double AccelerationX
		{
			get => _accelerationX;
			set
			{
				_accelerationX = value;
				RaisePropertyChanged();
			}
		}

		public double AccelerationY
		{
			get => _accelerationY;
			set
			{
				_accelerationY = value;
				RaisePropertyChanged();
			}
		}

		public double AccelerationZ
		{
			get => _accelerationZ;
			set
			{
				_accelerationZ = value;
				RaisePropertyChanged();
			}
		}

		public string ReadingTimestamp
		{
			get => _readingTimestamp;
			set
			{
				_readingTimestamp = value;
				RaisePropertyChanged();
			}
		}

		public string ShakenTimestamp
		{
			get => _shakenTimestamp;
			set
			{
				_shakenTimestamp = value;
				RaisePropertyChanged();
			}
		}

		private async void Accelerometer_ReadingChanged(Accelerometer sender, AccelerometerReadingChangedEventArgs args)
		{
			await Dispatcher.RunAsync(UnitTestDispatcherCompat.Priority.Normal, () =>
			{
				AccelerationX = args.Reading.AccelerationX;
				AccelerationY = args.Reading.AccelerationY;
				AccelerationZ = args.Reading.AccelerationZ;
				ReadingTimestamp = args.Reading.Timestamp.ToString("R");
			});
		}

		private async void Accelerometer_Shaken(Accelerometer sender, AccelerometerShakenEventArgs args)
		{
			await Dispatcher.RunAsync(UnitTestDispatcherCompat.Priority.Normal,
				() => ShakenTimestamp = args.Timestamp.ToString("R"));
		}
	}
}

