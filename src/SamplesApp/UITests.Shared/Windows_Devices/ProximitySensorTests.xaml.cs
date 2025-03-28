using System;
using System.Linq;
using System.Threading.Tasks;
using Uno.Disposables;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Devices.Enumeration;
using Windows.Devices.Sensors;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Private.Infrastructure;

namespace UITests.Windows_Devices;

[Sample(
	"Windows.Devices",
	Name = "ProximitySensor",
	Description = "Demonstrates use of Windows.Devices.Sensors.ProximitySensor",
	ViewModelType = typeof(ProximitySensorTestsViewModel),
	IgnoreInSnapshotTests = true,
	IsManualTest = true)]
public sealed partial class ProximitySensorTests : Page
{
	public ProximitySensorTests()
	{
		InitializeComponent();
		DataContextChanged += ProximitySensorTests_DataContextChanged;
	}

	private async void ProximitySensorTests_DataContextChanged(Windows.UI.Xaml.FrameworkElement sender, Windows.UI.Xaml.DataContextChangedEventArgs args)
	{
		var viewModel = (ProximitySensorTestsViewModel)DataContext;
		if (viewModel is null)
		{
			return;
		}

		try
		{
			await viewModel.InitializeAsync();
		}
		catch (Exception ex)
		{
			var dialog = new ContentDialog
			{
				Title = "Error ocurred",
				Content = ex.ToString(),
				XamlRoot = XamlRoot,
				CloseButtonText = "OK",
			};

			await dialog.ShowAsync();
		}
	}
}

[Bindable]
internal class ProximitySensorTestsViewModel : ViewModelBase
{
	private ProximitySensor _proximitySensor = null;
	private bool _readingChangedAttached;
	private string _sensorStatus;
	private uint? _distanceInMillimeters;
	private string _timestamp;
	private bool _isDetected;

	public ProximitySensorTestsViewModel(UnitTestDispatcherCompat dispatcher) : base(dispatcher)
	{
	}

	internal async Task InitializeAsync()
	{
		var selector = ProximitySensor.GetDeviceSelector();
		var devices = await DeviceInformation.FindAllAsync(selector);
		var device = devices.FirstOrDefault();
		if (device != null)
		{
			_proximitySensor = ProximitySensor.FromId(device.Id);
		}

		if (_proximitySensor is not null)
		{
			RaisePropertyChanged(nameof(ProximitySensorAvailable));

			SensorStatus = "ProximitySensor created";
		}
		else
		{
			SensorStatus = "ProximitySensor not available on this device";
		}

		Disposables.Add(Disposable.Create(() =>
		{
			if (_proximitySensor != null)
			{
				_proximitySensor.ReadingChanged -= ProximitySensor_ReadingChanged;
			}
		}));
	}

	public Command AttachReadingChangedCommand => new Command((p) =>
	{
		_proximitySensor.ReadingChanged += ProximitySensor_ReadingChanged;
		ReadingChangedAttached = true;
	});

	public Command DetachReadingChangedCommand => new Command((p) =>
	{
		_proximitySensor.ReadingChanged -= ProximitySensor_ReadingChanged;
		ReadingChangedAttached = false;
	});

	public bool ProximitySensorAvailable => _proximitySensor != null;

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
		set
		{
			_readingChangedAttached = value;
			RaisePropertyChanged();
		}
	}

	public uint? DistanceInMillimeters
	{
		get => _distanceInMillimeters;
		private set
		{
			_distanceInMillimeters = value;
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

	public bool IsDetected
	{
		get => _isDetected;
		private set
		{
			_isDetected = value;
			RaisePropertyChanged();
		}
	}

	private async void ProximitySensor_ReadingChanged(ProximitySensor sender, ProximitySensorReadingChangedEventArgs args)
	{
		await Dispatcher.RunAsync(UnitTestDispatcherCompat.Priority.Normal, () =>
		{
			DistanceInMillimeters = args.Reading.DistanceInMillimeters;
			Timestamp = args.Reading.Timestamp.ToString("R");
			IsDetected = args.Reading.IsDetected;
		});
	}
}
