using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
	[SampleControlInfo("Windows.Devices", "Magnetometer", description: "Demonstrates use of Windows.Devices.Sensors.Magnetometer", viewModelType: typeof(MagnetometerTestsViewModel))]
	public sealed partial class MagnetometerTests : UserControl
	{
		public MagnetometerTests()
		{
			this.InitializeComponent();
		}
	}

	[Bindable]
	public class MagnetometerTestsViewModel : ViewModelBase
	{
		private readonly Magnetometer _magnetometer = null;
		private MagnetometerReading _lastReading;
		private bool _readingChangedAttached;

		public MagnetometerTestsViewModel(CoreDispatcher dispatcher) : base(dispatcher)
		{
			_magnetometer = Magnetometer.GetDefault();
			if (_magnetometer != null)
			{
				_magnetometer.ReportInterval = 250;
			}
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

		public bool ReadingChangedAttached
		{
			get => _readingChangedAttached;
			set
			{
				_readingChangedAttached = value;
				RaisePropertyChanged();
			}
		}

		public MagnetometerReading LastReading
		{
			get => _lastReading;
			set
			{
				_lastReading = value;
				RaisePropertyChanged();
			}
		}

		private async void Magnetometer_ReadingChanged(Magnetometer sender, MagnetometerReadingChangedEventArgs args)
		{
			//the properties are accessed to make sure linker does not throw them away
			Debug.WriteLine(args.Reading.MagneticFieldX);
			Debug.WriteLine(args.Reading.MagneticFieldY);
			Debug.WriteLine(args.Reading.MagneticFieldZ);
			Debug.WriteLine(args.Reading.DirectionalAccuracy);
			Debug.WriteLine(args.Reading.Timestamp);
			await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
				() =>
				{
					LastReading = args.Reading;
				});
		}
	}
}
