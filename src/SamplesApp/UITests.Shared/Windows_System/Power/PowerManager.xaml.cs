using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Uno.Disposables;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using UwpPowerManager = Windows.System.Power.PowerManager;
using Private.Infrastructure;

namespace UITests.Shared.Windows_System.Power;

[Sample("Windows.System", Name = "Power.PowerManager",
	ViewModelType = typeof(PowerManagerTestsViewModel),
	Description = "Shows properties of Power manager and handles its events",
	IsManualTest = true)]
public sealed partial class PowerManager : UserControl
{
	public PowerManager()
	{
		InitializeComponent();
		DataContextChanged += PowerManager_DataContextChanged;
	}

	private async void PowerManager_DataContextChanged(Windows.UI.Xaml.FrameworkElement sender, Windows.UI.Xaml.DataContextChangedEventArgs args)
	{
		Model = args.NewValue as PowerManagerTestsViewModel;
		if (Model is { } viewModel)
		{
			await viewModel.InitializeAsync();
		}
	}

	internal PowerManagerTestsViewModel Model { get; private set; }
}

internal class PowerManagerTestsViewModel : ViewModelBase
{
	private bool _isInitialized;
	private string _batteryStatus = "";
	private string _energySaverStatus = "";
	private string _powerSupplyStatus = "";
	private string _remainingChargePercent = "";
	private string _remainingDischargeTime = "";

	private SerialDisposable _batteryStatusChangedSubscription = new();
	private SerialDisposable _energySaverStatusChangedSubscription = new();
	private SerialDisposable _powerSupplyStatusChangedSubscription = new();
	private SerialDisposable _remainingChargePercentChangedSubscription = new();
	private SerialDisposable _remainingDischargeTimeChangedSubscription = new();

	public PowerManagerTestsViewModel()
	{
		Disposables.Add(_batteryStatusChangedSubscription);
		Disposables.Add(_energySaverStatusChangedSubscription);
		Disposables.Add(_powerSupplyStatusChangedSubscription);
		Disposables.Add(_remainingChargePercentChangedSubscription);
		Disposables.Add(_remainingDischargeTimeChangedSubscription);
	}

	public async Task InitializeAsync()
	{
#if HAS_UNO
		IsInitialized = await UwpPowerManager.InitializeAsync();
#else
		IsInitialized = await Task.FromResult(true);
#endif
	}

	public Command RefreshValuesCommand => GetOrCreateCommand(() => RefreshValues());

	public Command ClearEventLogCommand => GetOrCreateCommand(() => EventLog.Clear());

	public ObservableCollection<string> EventLog { get; } = new();

	public bool IsInitialized
	{
		get => _isInitialized;
		set
		{
			_isInitialized = value;
			RaisePropertyChanged();
		}
	}

	public string BatteryStatus
	{
		get => _batteryStatus;
		set
		{
			_batteryStatus = value;
			RaisePropertyChanged();
		}
	}

	public string EnergySaverStatus
	{
		get => _energySaverStatus;
		set
		{
			_energySaverStatus = value;
			RaisePropertyChanged();
		}
	}

	public string PowerSupplyStatus
	{
		get => _powerSupplyStatus;
		set
		{
			_powerSupplyStatus = value;
			RaisePropertyChanged();
		}
	}

	public string RemainingChargePercent
	{
		get => _remainingChargePercent;
		set
		{
			_remainingChargePercent = value;
			RaisePropertyChanged();
		}
	}

	public string RemainingDischargeTime
	{
		get => _remainingDischargeTime;
		set
		{
			_remainingDischargeTime = value;
			RaisePropertyChanged();
		}
	}

	public bool IsBatteryStatusChangedSubscribed
	{
		get => _batteryStatusChangedSubscription.Disposable != null;
		set
		{
			if (value)
			{
				UwpPowerManager.BatteryStatusChanged += UwpPowerManager_BatteryStatusChanged;
				_batteryStatusChangedSubscription.Disposable = Disposable.Create(() =>
					UwpPowerManager.BatteryStatusChanged -= UwpPowerManager_BatteryStatusChanged);
			}
			else
			{
				_batteryStatusChangedSubscription.Disposable = null;
			}

			RaisePropertyChanged();
		}
	}

	public bool IsEnergySaverStatusChangedSubscribed
	{
		get => _energySaverStatusChangedSubscription.Disposable != null;
		set
		{
			if (value)
			{
				UwpPowerManager.EnergySaverStatusChanged += UwpPowerManager_EnergySaverStatusChanged;
				_energySaverStatusChangedSubscription.Disposable = Disposable.Create(() =>
					UwpPowerManager.EnergySaverStatusChanged -= UwpPowerManager_EnergySaverStatusChanged);
			}
			else
			{
				_energySaverStatusChangedSubscription.Disposable = null;
			}

			RaisePropertyChanged();
		}
	}

	public bool IsPowerSupplyStatusChangedSubscribed
	{
		get => _powerSupplyStatusChangedSubscription.Disposable != null;
		set
		{
			if (value)
			{
				UwpPowerManager.PowerSupplyStatusChanged += UwpPowerManager_PowerSupplyStatusChanged;
				_powerSupplyStatusChangedSubscription.Disposable = Disposable.Create(() =>
					UwpPowerManager.PowerSupplyStatusChanged -= UwpPowerManager_PowerSupplyStatusChanged);
			}
			else
			{
				_powerSupplyStatusChangedSubscription.Disposable = null;
			}

			RaisePropertyChanged();
		}
	}

	public bool IsRemainingChargePercentChangedSubscribed
	{
		get => _remainingChargePercentChangedSubscription.Disposable != null;
		set
		{
			if (value)
			{
				UwpPowerManager.RemainingChargePercentChanged += UwpPowerManager_RemainingChargePercentChanged;
				_remainingChargePercentChangedSubscription.Disposable = Disposable.Create(() =>
					UwpPowerManager.RemainingChargePercentChanged -= UwpPowerManager_RemainingChargePercentChanged);
			}
			else
			{
				_remainingChargePercentChangedSubscription.Disposable = null;
			}

			RaisePropertyChanged();
		}
	}

	public bool IsRemainingDischargeTimeChangedSubscribed
	{
		get => _remainingDischargeTimeChangedSubscription.Disposable != null;
		set
		{
			if (value)
			{
				UwpPowerManager.RemainingDischargeTimeChanged += UwpPowerManager_RemainingDischargeTimeChanged;
				_remainingDischargeTimeChangedSubscription.Disposable = Disposable.Create(() =>
					UwpPowerManager.RemainingDischargeTimeChanged -= UwpPowerManager_RemainingDischargeTimeChanged);
			}
			else
			{
				_remainingDischargeTimeChangedSubscription.Disposable = null;
			}

			RaisePropertyChanged();
		}
	}

	private void RefreshValues()
	{
		BatteryStatus = SafeGetter(() => UwpPowerManager.BatteryStatus.ToString("g"));
		EnergySaverStatus = SafeGetter(() => UwpPowerManager.EnergySaverStatus.ToString("g"));
		PowerSupplyStatus = SafeGetter(() => UwpPowerManager.PowerSupplyStatus.ToString("g"));
		RemainingChargePercent = SafeGetter(() => UwpPowerManager.RemainingChargePercent.ToString() + " %");
		RemainingDischargeTime = SafeGetter(() => UwpPowerManager.RemainingDischargeTime.ToString());
	}

	private string SafeGetter(Func<string> action)
	{
		try
		{
			return action();
		}
		catch (Exception)
		{
			// Ignore
			return "Not implemented";
		}
	}

	private void UwpPowerManager_PowerSupplyStatusChanged(object sender, object e) =>
		LogEvent(nameof(UwpPowerManager.PowerSupplyStatusChanged));

	private void UwpPowerManager_RemainingChargePercentChanged(object sender, object e) =>
		LogEvent(nameof(UwpPowerManager.RemainingChargePercentChanged));

	private void UwpPowerManager_RemainingDischargeTimeChanged(object sender, object e) =>
		LogEvent(nameof(UwpPowerManager.RemainingDischargeTimeChanged));

	private void UwpPowerManager_EnergySaverStatusChanged(object sender, object e) =>
		LogEvent(nameof(UwpPowerManager.EnergySaverStatusChanged));

	private void UwpPowerManager_BatteryStatusChanged(object sender, object e) =>
		LogEvent(nameof(UwpPowerManager.BatteryStatusChanged));

	private async void LogEvent(string eventName) =>
		await ExecuteOnUiThreadAsync(() =>
		{
			EventLog.Add($"{DateTime.Now.ToShortTimeString()} | {eventName}");
			RefreshValues();
		});

	private async Task ExecuteOnUiThreadAsync(Action action) =>
		await Dispatcher.RunAsync(UnitTestDispatcherCompat.Priority.Normal, () => action());
}
