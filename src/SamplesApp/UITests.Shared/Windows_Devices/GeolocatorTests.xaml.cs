using System;
using System.Diagnostics;
using System.Windows.Input;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Devices.Geolocation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_Devices
{
	[SampleControlInfo("Windows.Devices", "Geolocator", description: "Demonstrates use of Windows.Devices.Geolocation.Geolocator", viewModelType: typeof(GeolocatorTestsViewModel))]
	public sealed partial class GeolocatorTests : UserControl
	{
		public GeolocatorTests()
		{
			this.InitializeComponent();
			this.DataContextChanged += GeolocatorTests_DataContextChanged;
		}

		public GeolocatorTestsViewModel ViewModel { get; private set; }

		private void GeolocatorTests_DataContextChanged(DependencyObject sender, Windows.UI.Xaml.DataContextChangedEventArgs args)
		{
			ViewModel = args.NewValue as GeolocatorTestsViewModel;
		}
	}

	[Bindable]
	public class GeolocatorTestsViewModel : ViewModelBase
	{
		private Geolocator _geolocator = new Geolocator();

		private GeolocationAccessStatus _geolocationAccessStatus;
		private PositionStatus _positionStatus;
		private Geoposition _geoposition;
		private Geoposition _trackedGeoposition;
		private bool _positionChangedAttached;
		private bool _statusChangedAttached;
		private string _error = "";

		public GeolocatorTestsViewModel(CoreDispatcher dispatcher) : base(dispatcher)
		{
			PositionStatus = _geolocator.LocationStatus;
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


		public Command AttachPositionChangedCommand => new Command((p) =>
		{
			_geolocator.PositionChanged += Geolocator_PositionChanged;
			PositionChangedAttached = true;
		});

		public Command DetachPositionChangedCommand => new Command((p) =>
		{
			_geolocator.PositionChanged -= Geolocator_PositionChanged;
			PositionChangedAttached = false;
		});


		public Command AttachStatusChangedCommand => new Command((p) =>
		{
			_geolocator.StatusChanged += Geolocator_StatusChanged;
			StatusChangedAttached = true;
		});

		public Command DetachStatusChangedCommand => new Command((p) =>
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

		private async void RequestAccess() =>
			GeolocationAccessStatus = await Geolocator.RequestAccessAsync();

		private async void GetGeoposition()
		{
			try
			{
				Geoposition = await _geolocator.GetGeopositionAsync();
			}
			catch (Exception ex)
			{
				Error = ex.Message;
			}
		}

		private async void Geolocator_StatusChanged(Geolocator sender, StatusChangedEventArgs args)
		{
			await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				PositionStatus = args.Status;
			});
		}

		private async void Geolocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
		{
			await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				TrackedGeoposition = args.Position;
			});
		}
	}
}
