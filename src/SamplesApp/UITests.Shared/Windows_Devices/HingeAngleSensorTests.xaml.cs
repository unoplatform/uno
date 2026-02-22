using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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

namespace UITests.Shared.Windows_Devices
{
	[Sample("Windows.Devices", Name = "HingeAngleSensor", Description = "Demonstrates use of Windows.Devices.Sensors.HingeAngleSensor", ViewModelType = typeof(HingeAngleSensorTestsViewModel), IgnoreInSnapshotTests = true)]
	public sealed partial class HingeAngleSensorTests : UserControl
	{
		public HingeAngleSensorTests()
		{
			this.InitializeComponent();
			this.Loaded += HingeAngleSensorTests_Loaded;
		}

		private async void HingeAngleSensorTests_Loaded(object sender, RoutedEventArgs e)
		{
			await ((HingeAngleSensorTestsViewModel)DataContext).InitializeAsync();
		}

		[Bindable]
		internal class HingeAngleSensorTestsViewModel : ViewModelBase
		{
			private HingeAngleSensor _hinge;
			private bool _readingChangedAttached;
			private string _sensorStatus;
			private double _angle;
			private string _timestamp;

			public HingeAngleSensorTestsViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
			{

			}

			public async Task InitializeAsync()
			{
				_hinge = await HingeAngleSensor.GetDefaultAsync();
				if (_hinge != null)
				{
					SensorStatus = "HingeAngleSensor created";
				}
				else
				{
					SensorStatus = "HingeAngleSensor not available on this device";
				}
				RaisePropertyChanged(nameof(HingeAngleSensorAvailable));
				Disposables.Add(Disposable.Create(() =>
				{
					if (_hinge != null)
					{
						_hinge.ReadingChanged -= HingeAngleSensor_ReadingChanged;
					}
				}));
			}

			public Command AttachReadingChangedCommand => GetOrCreateCommand(() =>
			{
				_hinge.ReadingChanged += HingeAngleSensor_ReadingChanged;
				ReadingChangedAttached = true;
			});

			public Command DetachReadingChangedCommand => GetOrCreateCommand(() =>
			{
				_hinge.ReadingChanged -= HingeAngleSensor_ReadingChanged;
				ReadingChangedAttached = false;
			});

			public bool HingeAngleSensorAvailable => _hinge != null;

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

			public double Angle
			{
				get => _angle;
				private set
				{
					_angle = value;
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

			private async void HingeAngleSensor_ReadingChanged(HingeAngleSensor sender, HingeAngleSensorReadingChangedEventArgs args)
			{
				await Dispatcher.RunAsync(UnitTestDispatcherCompat.Priority.Normal, () =>
				{
					Angle = args.Reading.AngleInDegrees;
					Timestamp = args.Reading.Timestamp.ToString("R");
				});
			}
		}
	}
}
