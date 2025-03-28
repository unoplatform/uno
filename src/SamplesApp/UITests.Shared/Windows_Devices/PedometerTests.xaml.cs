using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
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

using ICommand = System.Windows.Input.ICommand;
using EventHandler = System.EventHandler;

namespace UITests.Shared.Windows_Devices
{
	[SampleControlInfo(
		"Windows.Devices",
		"Pedometer",
		description: "Demonstrates the Windows.Devices.Sensors.Pedometer",
		viewModelType: typeof(PedometerTestsViewModel),
		ignoreInSnapshotTests: true)]
	public sealed partial class PedometerTests : UserControl
	{
		public PedometerTests()
		{
			this.InitializeComponent();
		}
	}

	internal class PedometerTestsViewModel : ViewModelBase
	{
		private Pedometer _pedometer = null;
		private string _pedometerStatus;
		private bool _readingChangedAttached;
		private int _cumulativeSteps;
		private double _cumulativeStepsDurationInSeconds;
		private string _timestamp;

		public PedometerTestsViewModel(UnitTestDispatcherCompat dispatcher) :
			base(dispatcher)
		{
		}

		public ICommand GetPedometerCommand => GetOrCreateCommand(GetPedometerAsync);

		public string PedometerStatus
		{
			get => _pedometerStatus;
			private set
			{
				_pedometerStatus = value;
				RaisePropertyChanged();
			}
		}

		public bool IsAvailable => _pedometer != null;

		private async void GetPedometerAsync()
		{
			try
			{
				_pedometer = await Pedometer.GetDefaultAsync();
				_pedometer.ReportInterval = 10000;
				Disposables.Add(Disposable.Create(() =>
				{
					if (_pedometer != null)
					{
						_pedometer.ReadingChanged -= Pedometer_ReadingChanged;
					}
				}));

				RaisePropertyChanged(nameof(IsAvailable));
			}
			catch (Exception ex)
			{
				PedometerStatus = $"Error occurred trying to get pedometer instance: {ex.Message}";
			}
		}

		public Command AttachReadingChangedCommand => new Command((p) =>
		{
			_pedometer.ReadingChanged += Pedometer_ReadingChanged;
			ReadingChangedAttached = true;
		});

		public Command DetachReadingChangedCommand => new Command((p) =>
		{
			_pedometer.ReadingChanged -= Pedometer_ReadingChanged;
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

		public int CumulativeSteps
		{
			get => _cumulativeSteps;
			set
			{
				_cumulativeSteps = value;
				RaisePropertyChanged();
			}
		}

		public double CumulativeStepsDurationInSeconds
		{
			get => _cumulativeStepsDurationInSeconds;
			set
			{
				_cumulativeStepsDurationInSeconds = value;
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

		private async void Pedometer_ReadingChanged(Pedometer sender, PedometerReadingChangedEventArgs args)
		{
			await Dispatcher.RunAsync(UnitTestDispatcherCompat.Priority.Normal, () =>
			{
				CumulativeSteps = args.Reading.CumulativeSteps;
				CumulativeStepsDurationInSeconds = args.Reading.CumulativeStepsDuration.TotalSeconds;
				Timestamp = args.Reading.Timestamp.ToString("R");
			});
		}
	}
}
