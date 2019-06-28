using System;
using System.Collections.Generic;
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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_Devices
{
	public sealed partial class AccelerometerTests : UserControl
	{

		public AccelerometerTests()
		{
			this.InitializeComponent();
			DataContext = new AccelerometerTestsViewModel(Dispatcher);
		}

		private class AccelerometerTestsViewModel : ViewModelBase
		{
			private Accelerometer _accelerometer = null;
			private AccelerometerReading _lastReading;
			private DateTimeOffset? _lastShake;

			public AccelerometerTestsViewModel(CoreDispatcher dispatcher) : base(dispatcher)
			{
				_accelerometer = Accelerometer.GetDefault();
				_accelerometer.ReadingChanged += Accelerometer_ReadingChanged;
				_accelerometer.Shaken += Accelerometer_Shaken;
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

			public DateTimeOffset? LastShake
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
				await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
					() => LastReading = args.Reading);
			}

			private async void Accelerometer_Shaken(Accelerometer sender, AccelerometerShakenEventArgs args)
			{
				await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
					() => LastShake = args.Timestamp);
			}
		}
	}
}
