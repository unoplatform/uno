using System;
using System.Collections.Generic;
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
using Private.Infrastructure;

namespace UITests.Shared.Windows_Devices
{
	[SampleControlInfo("Windows.Devices", "Barometer", description: "Demonstrates use of Windows.Devices.Sensors.Barometer", viewModelType: typeof(BarometerTestsViewModel), ignoreInSnapshotTests: true)]
	public sealed partial class BarometerTests : UserControl
	{
		public BarometerTests()
		{
			this.InitializeComponent();

		}

		[Bindable]
		internal class BarometerTestsViewModel : ViewModelBase
		{
			private Barometer _barometer;
			private bool _readingChangedAttached;
			private string _sensorStatus;
			private double _stationPressureInHectopascals;
			private string _timestamp;

			public BarometerTestsViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
			{

				_barometer = Barometer.GetDefault();
				if (_barometer != null)
				{
					_barometer.ReportInterval = 250;
					SensorStatus = "Barometer created";
				}
				else
				{
					SensorStatus = "Barometer not available on this device";
				}
				Disposables.Add(Disposable.Create(() =>
				{
					if (_barometer != null)
					{
						_barometer.ReadingChanged -= Barometer_ReadingChanged;
					}
				}));
			}

			public Command AttachReadingChangedCommand => new Command((p) =>
			{
				_barometer.ReadingChanged += Barometer_ReadingChanged;
				ReadingChangedAttached = true;
			});

			public Command DetachReadingChangedCommand => new Command((p) =>
			{
				_barometer.ReadingChanged -= Barometer_ReadingChanged;
				ReadingChangedAttached = false;
			});

			public bool BarometerAvailable => _barometer != null;

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
				private set
				{
					_readingChangedAttached = value;
					RaisePropertyChanged();
				}
			}

			public double StationPressureInHectopascals
			{
				get => _stationPressureInHectopascals;
				private set
				{
					_stationPressureInHectopascals = value;
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

			private async void Barometer_ReadingChanged(Barometer sender, BarometerReadingChangedEventArgs args)
			{
				await Dispatcher.RunAsync(UnitTestDispatcherCompat.Priority.Normal, () =>
				{
					StationPressureInHectopascals = args.Reading.StationPressureInHectopascals;
					Timestamp = args.Reading.Timestamp.ToString("R");
				});
			}
		}
	}
}
