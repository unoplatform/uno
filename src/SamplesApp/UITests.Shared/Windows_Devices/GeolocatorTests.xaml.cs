using System;
using System.Windows.Input;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Devices.Geolocation;
using Windows.UI.Core;
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
		}
	}

	[Bindable]
	public class GeolocatorTestsViewModel : ViewModelBase
	{
		private GeolocationAccessStatus _geolocationAccessStatus;
		private Geoposition _geoposition;

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
			var geolocator = new Geolocator();
			Geoposition = await geolocator.GetGeopositionAsync();
		}
	}
}
