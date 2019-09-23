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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_Devices
{
	[SampleControlInfo("Windows.Devices", "Gyrometer", description: "Demonstrates use of Windows.Devices.Sensors.Gyrometer", viewModelType: typeof(GyrometerTestsViewModel))]
	public sealed partial class GyrometerTests : UserControl
	{
		public GyrometerTests()
		{
			this.InitializeComponent();
		}
	}

	[Bindable]
	public class GyrometerTestsViewModel : ViewModelBase
	{
		private readonly Gyrometer _gyrometer = null;
		private bool _readingChangedAttached;
		private string _sensorStatus;
		private float _rotationX;
		private float _rotationY;
		private float _rotationZ;
		private string _timestamp;

		public GyrometerTestsViewModel(CoreDispatcher dispatcher) : base(dispatcher)
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

		public float RotationX
		{
			get => _rotationX;
			private set
			{
				_rotationX = value;
				RaisePropertyChanged();
			}
		}

		public float RotationY
		{
			get => _rotationY;
			private set
			{
				_rotationY = value;
				RaisePropertyChanged();
			}
		}

		public float RotationZ
		{
			get => _rotationZ;
			private set
			{
				_rotationZ = value;
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
			await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				MagneticFieldX = args.Reading.;
				MagneticFieldY = args.Reading.MagneticFieldY;
				MagneticFieldZ = args.Reading.MagneticFieldZ;
				DirectionalAccuracy = args.Reading.DirectionalAccuracy;
				Timestamp = args.Reading.Timestamp.ToString("R");
			});
		}
	}
}
