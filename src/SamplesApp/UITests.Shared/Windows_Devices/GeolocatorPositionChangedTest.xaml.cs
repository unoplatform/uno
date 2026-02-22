using System;
using Uno.Disposables;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Devices.Geolocation;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

using ICommand = System.Windows.Input.ICommand;
using System.Threading.Tasks;

namespace UITests.Shared.Windows_Devices;

[Sample("Windows.Devices", Name = "GeolocatorPositionChanged",
	Description = "Test the Geolocator PositionChanged reading from non-UI thread",
	ViewModelType = typeof(GeolocatorPositionChangedTestViewModel),
	IsManualTest = true)]
public sealed partial class GeolocatorPositionChangedTest : UserControl
{
	public GeolocatorPositionChangedTest()
	{
		this.InitializeComponent();
	}
}


[Bindable]
internal class GeolocatorPositionChangedTestViewModel : ViewModelBase
{
	private readonly Geolocator _geolocator;

	public GeolocatorPositionChangedTestViewModel()
	{
		_geolocator = new Geolocator
		{
			MovementThreshold = 1,
			ReportInterval = 2000
		};
	}

	private string _status;
	public string Status
	{
		get => _status;
		set
		{
			_status = value;
			RaisePropertyChanged();
		}
	}

	public ICommand AskPermissionsCommand =>
		GetOrCreateCommand(async () => await AskPermissions());

	private Task AskPermissions() => Task.Run(AskPermissionsAsync);

	private async Task AskPermissionsAsync()
	{
		UpdateStatus("Asking Permissions...");

		var accessStatus = await Geolocator.RequestAccessAsync();

		if (accessStatus == GeolocationAccessStatus.Allowed)
		{
			UpdateStatus("Permission Granted");

			_geolocator.PositionChanged -= Locator_PositionChanged;
			_geolocator.PositionChanged += Locator_PositionChanged;
		}
	}

	private void Locator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
	{
		var loc = args.Position.Coordinate;
		UpdateStatus($"Location Changed: Lon: {loc.Longitude} and Lat {loc.Latitude}");
	}

	private void UpdateStatus(string status) => Status += $"{status}\n";
}
