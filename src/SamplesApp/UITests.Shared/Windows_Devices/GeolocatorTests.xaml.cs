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
		private Geoposition _geoposition;
		private Geoposition _trackedGeoposition;
		private bool _positionChangedAttached;

		public GeolocatorTestsViewModel(CoreDispatcher dispatcher) : base(dispatcher)
		{
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

		public uint? DesiredAccuracyInMeters
		{
			get => _geolocator.DesiredAccuracyInMeters;
			set => _geolocator.DesiredAccuracyInMeters = value;
		}

		public ICommand RequestAccessCommand =>
			GetOrCreateCommand(RequestAccess);

		public ICommand GetGeopositionCommand =>
			GetOrCreateCommand(GetGeoposition);

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
			Geoposition = await _geolocator.GetGeopositionAsync();
			Debug.WriteLine(Geoposition.Coordinate.Longitude);
		}

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
		
		private async void Geolocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
		{
			await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				TrackedGeoposition = args.Position;
			});
		}
	}
}
