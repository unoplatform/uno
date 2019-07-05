using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Sensors;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Core;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_Devices
{
	[SampleControlInfo("Windows.Devices", "Accelerometer", description: "Demonstrates use of Windows.Devices.Sensors.Accelerometer", viewModelType : typeof(AccelerometerTestsViewModel))]
	public sealed partial class AccelerometerTests : UserControl
	{

		public AccelerometerTests()
		{
			this.InitializeComponent();
		}
	}

	[Bindable]
	public class AccelerometerTestsViewModel : ViewModelBase
	{
		private readonly Accelerometer _accelerometer = null;
		private AccelerometerReading _lastReading;
		private string _lastShake;
		private bool _readingChangedAttached;
		private bool _shakenAttached;

		public AccelerometerTestsViewModel(CoreDispatcher dispatcher) : base(dispatcher)
		{
			_accelerometer = Accelerometer.GetDefault();
			_accelerometer.ReportInterval = 250;
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

		public bool ReadingChangedAttached
		{
			get => _readingChangedAttached;
			set
			{
				_readingChangedAttached = value;
				RaisePropertyChanged();
			}
		}

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

		public bool ShakenAttached
		{
			get => _shakenAttached;
			set
			{
				_shakenAttached = value;
				RaisePropertyChanged();
			}
		}

		public AccelerometerReading LastReading
		{
			get => _lastReading;
			set
			{
				_lastReading = value;
				RaisePropertyChanged();
			}
		}

		public string LastShake
		{
			get => _lastShake;
			set
			{
				_lastShake = value;
				RaisePropertyChanged();
			}
		}

		private async void Accelerometer_ReadingChanged(Accelerometer sender, AccelerometerReadingChangedEventArgs args)
		{
			//the properties are accessed to make sure linker does not throw them away
			Debug.WriteLine(args.Reading.AccelerationX);
			Debug.WriteLine(args.Reading.AccelerationY);
			Debug.WriteLine(args.Reading.AccelerationZ);
			Debug.WriteLine(args.Reading.Timestamp);
			await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
				() =>
				{
					LastReading = args.Reading;
				});
		}

		private async void Accelerometer_Shaken(Accelerometer sender, AccelerometerShakenEventArgs args)
		{
			await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
				() => LastShake = args.Timestamp.ToString("R"));
		}
	}
}

