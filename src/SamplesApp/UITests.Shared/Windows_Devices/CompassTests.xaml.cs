using System;
using Uno.Disposables;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Devices.Sensors;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Private.Infrastructure;

namespace UITests.Shared.Windows_Devices
{
	[Sample(
		"Windows.Devices",
		Name = "Compass",
		Description = "Demonstrates use of Windows.Devices.Sensors.Compass",
		ViewModelType = typeof(CompassTestsViewModel),
		IgnoreInSnapshotTests = true)]
	public sealed partial class CompassTests : UserControl
	{
		public CompassTests()
		{
			this.InitializeComponent();

		}

		[Bindable]
		internal class CompassTestsViewModel : ViewModelBase
		{
			private Compass _compass;
			private bool _readingChangedAttached;
			private string _sensorStatus;
			private double _headingMagneticNorth;
			private double _headingTrueNorth;
			private string _timestamp;

			public CompassTestsViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
			{

				_compass = Compass.GetDefault();
				if (_compass != null)
				{
					_compass.ReportInterval = 250;
					SensorStatus = "Compass created";
				}
				else
				{
					SensorStatus = "Compass not available on this device";
				}
				Disposables.Add(Disposable.Create(() =>
				{
					if (_compass != null)
					{
						_compass.ReadingChanged -= Compass_ReadingChanged;
					}
				}));
			}

			public Command AttachReadingChangedCommand => new Command((p) =>
			{
				_compass.ReadingChanged += Compass_ReadingChanged;
				ReadingChangedAttached = true;
			});

			public Command DetachReadingChangedCommand => new Command((p) =>
			{
				_compass.ReadingChanged -= Compass_ReadingChanged;
				ReadingChangedAttached = false;
			});

			public bool CompassAvailable => _compass != null;

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

			public double HeadingMagneticNorth
			{
				get => _headingMagneticNorth;
				private set
				{
					_headingMagneticNorth = value;
					RaisePropertyChanged();
				}
			}

			public double HeadingTrueNorth
			{
				get => _headingTrueNorth;
				private set
				{
					_headingTrueNorth = value;
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

			private async void Compass_ReadingChanged(Compass sender, CompassReadingChangedEventArgs args)
			{
				await Dispatcher.RunAsync(UnitTestDispatcherCompat.Priority.Normal, () =>
				{
					HeadingMagneticNorth = args.Reading.HeadingMagneticNorth;
					HeadingTrueNorth = args.Reading.HeadingTrueNorth ?? double.NaN;
					Timestamp = args.Reading.Timestamp.ToString("R");
				});
			}
		}
	}
}
