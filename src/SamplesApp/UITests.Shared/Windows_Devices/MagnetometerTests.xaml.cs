using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.Disposables;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Private.Infrastructure;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_Devices
{
	[Sample("Windows.Devices", Name = "Magnetometer", Description = "Demonstrates use of Windows.Devices.Sensors.Magnetometer", ViewModelType = typeof(MagnetometerTestsViewModel), IgnoreInSnapshotTests = true)]
	public sealed partial class MagnetometerTests : UserControl
	{
		public MagnetometerTests()
		{
			this.InitializeComponent();
		}
	}

	[Bindable]
	internal class MagnetometerTestsViewModel : ViewModelBase
	{
		private readonly Magnetometer _magnetometer = null;
		private bool _readingChangedAttached;
		private string _sensorStatus;
		private MagnetometerAccuracy _directionalAccuracy;
		private float _magneticFieldZ;
		private float _magneticFieldY;
		private float _magneticFieldX;
		private string _timestamp;

		public MagnetometerTestsViewModel(UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
			_magnetometer = Magnetometer.GetDefault();
			if (_magnetometer != null)
			{
				_magnetometer.ReportInterval = 250;
				SensorStatus = "Magnetometer created";
			}
			else
			{
				SensorStatus = "Magnetometer not available on this device";
			}
			Disposables.Add(Disposable.Create(() =>
			{
				if (_magnetometer != null)
				{
					_magnetometer.ReadingChanged -= Magnetometer_ReadingChanged;
				}
			}));
		}

		public Command AttachReadingChangedCommand => new Command((p) =>
		{
			_magnetometer.ReadingChanged += Magnetometer_ReadingChanged;
			ReadingChangedAttached = true;
		});

		public Command DetachReadingChangedCommand => new Command((p) =>
		{
			_magnetometer.ReadingChanged -= Magnetometer_ReadingChanged;
			ReadingChangedAttached = false;
		});

		public bool MagnetometerAvailable => _magnetometer != null;

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

		public float MagneticFieldX
		{
			get => _magneticFieldX;
			private set
			{
				_magneticFieldX = value;
				RaisePropertyChanged();
			}
		}

		public float MagneticFieldY
		{
			get => _magneticFieldY;
			private set
			{
				_magneticFieldY = value;
				RaisePropertyChanged();
			}
		}

		public float MagneticFieldZ
		{
			get => _magneticFieldZ;
			private set
			{
				_magneticFieldZ = value;
				RaisePropertyChanged();
			}
		}

		public MagnetometerAccuracy DirectionalAccuracy
		{
			get => _directionalAccuracy;
			private set
			{
				_directionalAccuracy = value;
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

		private async void Magnetometer_ReadingChanged(Magnetometer sender, MagnetometerReadingChangedEventArgs args)
		{
			await Dispatcher.RunAsync(UnitTestDispatcherCompat.Priority.Normal, () =>
			{
				MagneticFieldX = args.Reading.MagneticFieldX;
				MagneticFieldY = args.Reading.MagneticFieldY;
				MagneticFieldZ = args.Reading.MagneticFieldZ;
				DirectionalAccuracy = args.Reading.DirectionalAccuracy;
				Timestamp = args.Reading.Timestamp.ToString("R");
			});
		}
	}
}
