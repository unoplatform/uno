using System;
using Uno.Disposables;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Devices.Geolocation;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Private.Infrastructure;

using ICommand = System.Windows.Input.ICommand;

namespace UITests.Shared.Windows_Devices
{
	[Sample("Windows.Devices", Name = "Geolocator", Description = "Demonstrates use of Windows.Devices.Geolocation.Geolocator", ViewModelType = typeof(GeolocatorTestsViewModel), IgnoreInSnapshotTests = true)]
	public sealed partial class GeolocatorTests : UserControl
	{
		public GeolocatorTests()
		{
			this.InitializeComponent();
			this.DataContextChanged += GeolocatorTests_DataContextChanged;
		}

		internal GeolocatorTestsViewModel ViewModel { get; private set; }

		private void GeolocatorTests_DataContextChanged(DependencyObject sender, Microsoft.UI.Xaml.DataContextChangedEventArgs args)
		{
			ViewModel = args.NewValue as GeolocatorTestsViewModel;
		}
	}

	[Bindable]
	internal class GeolocatorTestsViewModel : ViewModelBase
	{
		private Geolocator _geolocator = new Geolocator();

		private GeolocationAccessStatus _geolocationAccessStatus;
		private PositionStatus _positionStatus;
		private Geoposition _geoposition;
		private Geoposition _trackedGeoposition;
		private bool _positionChangedAttached;
		private bool _statusChangedAttached;
		private string _error = "";

		public GeolocatorTestsViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
			PositionStatus = _geolocator.LocationStatus;
			timeout = TimeSpan.FromSeconds(10);
			maximumAge = TimeSpan.FromSeconds(15);

			Disposables.Add(Disposable.Create(() =>
			{
				_geolocator.PositionChanged -= Geolocator_PositionChanged;
				_geolocator.StatusChanged -= Geolocator_StatusChanged;
			}));
		}

		public Geoposition Geoposition
		{
			get => _geoposition;
			set
			{
				_geoposition = value;
				RaisePropertyChanged();
			}
		}

		public TimeSpan timeout { get; set; }
		public TimeSpan maximumAge { get; set; }

		public Geoposition TrackedGeoposition
		{
			get => _trackedGeoposition;
			set
			{
				_trackedGeoposition = value;
				RaisePropertyChanged();
			}
		}

		public bool PositionChangedAttached
		{
			get => _positionChangedAttached;
			private set
			{
				_positionChangedAttached = value;
				RaisePropertyChanged();
			}
		}

		public bool StatusChangedAttached
		{
			get => _statusChangedAttached;
			private set
			{
				_statusChangedAttached = value;
				RaisePropertyChanged();
			}
		}

		public uint? DesiredAccuracyInMeters
		{
			get => _geolocator.DesiredAccuracyInMeters;
			set => _geolocator.DesiredAccuracyInMeters = value;
		}



		public PositionStatus PositionStatus
		{
			get => _positionStatus;
			set
			{
				_positionStatus = value;
				RaisePropertyChanged();
			}
		}


		public string Error
		{
			get => _error;
			set
			{
				_error = value;
				RaisePropertyChanged();
			}
		}

		public ICommand RequestAccessCommand =>
			GetOrCreateCommand(RequestAccess);

		public ICommand GetGeopositionCommand =>
			GetOrCreateCommand(GetGeoposition);

		public Command AttachPositionChangedCommand => GetOrCreateCommand(() =>
		{
			_geolocator.PositionChanged += Geolocator_PositionChanged;
			PositionChangedAttached = true;
		});

		public Command DetachPositionChangedCommand => GetOrCreateCommand(() =>
		{
			_geolocator.PositionChanged -= Geolocator_PositionChanged;
			PositionChangedAttached = false;
		});


		public Command AttachStatusChangedCommand => GetOrCreateCommand(() =>
		{
			_geolocator.StatusChanged += Geolocator_StatusChanged;
			StatusChangedAttached = true;
		});

		public Command DetachStatusChangedCommand => GetOrCreateCommand(() =>
		{
			_geolocator.StatusChanged -= Geolocator_StatusChanged;
			StatusChangedAttached = false;
		});

		public GeolocationAccessStatus GeolocationAccessStatus
		{
			get => _geolocationAccessStatus;
			private set
			{
				_geolocationAccessStatus = value;
				RaisePropertyChanged();
			}
		}

		private async void RequestAccess()
		{
			GeolocationAccessStatus = await Geolocator.RequestAccessAsync();
		}

		private async void GetGeoposition()
		{
			try
			{
				var timeout1 = new TimeSpan(0, 0, timeout.Hours, timeout.Seconds);
				var maximumAge1 = new TimeSpan(0, 0, maximumAge.Hours, maximumAge.Seconds);

				var startTime = DateTimeOffset.Now;

				Geoposition = await _geolocator.GetGeopositionAsync(maximumAge1, timeout1);

				if (Geoposition.Coordinate.Timestamp < DateTimeOffset.Now - maximumAge)
				{
					Error = "Implementation error: Position data is too old";
				}

				if (startTime < DateTimeOffset.Now - timeout)
				{
					Error = "Implementation error: no reaction for TimeOut parameter";
				}


			}
			catch (Exception ex)
			{
				Error = ex.Message;
			}
		}

		private async void Geolocator_StatusChanged(Geolocator sender, StatusChangedEventArgs args)
		{
			await Dispatcher.RunAsync(UnitTestDispatcherCompat.Priority.Normal, () =>
			{
				PositionStatus = args.Status;
			});
		}

		private async void Geolocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
		{
			await Dispatcher.RunAsync(UnitTestDispatcherCompat.Priority.Normal, () =>
			{
				TrackedGeoposition = args.Position;
			});
		}
	}
}
