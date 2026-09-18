#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Uno.UI.Samples.Controls;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace UITests.Windows_UI_Notifications;

[Sample(
	"Microsoft.Windows.AppNotifications",
	"Windows.UI.Notifications",
	Name = "AppNotificationManager",
	Description = "Exercises registration, posting, progress updates, scheduling where supported, activation, history, and removal.",
	IsManualTest = true,
	IgnoreInSnapshotTests = true)]
public sealed partial class AppNotificationManagerSample : Page
{
	private const string NotificationTag = "samples-app-progress";
	private const string NotificationGroup = "samples-app";
	private const string ScheduledTag = "samples-app-scheduled";
	private static Func<Task<bool>> _requestPermissionAsync = static () => Task.FromResult(true);
	private readonly AppNotificationManager _manager = AppNotificationManager.Default;
	private bool _isHandlerAttached;
	private bool _isLoaded;
	private bool _isRegistered;
	private uint _lastNotificationId;
	private uint _progressSequence = 1;
	private double _progressValue = 0.25;

	public AppNotificationManagerSample()
	{
		InitializeComponent();
	}

	public static void ConfigurePermissionRequest(Func<Task<bool>> requestPermissionAsync)
		=> _requestPermissionAsync = requestPermissionAsync ?? throw new ArgumentNullException(nameof(requestPermissionAsync));

	private void Page_Loaded(object sender, RoutedEventArgs e)
	{
		_isLoaded = true;
		if (OperatingSystem.IsBrowser())
		{
			ScheduleButton.Visibility = Visibility.Collapsed;
			RemoveScheduledButton.Visibility = Visibility.Collapsed;
			HistoryStatusText.Text = "Active: not queried";
		}
		if (!_isHandlerAttached)
		{
			_manager.NotificationInvoked += Manager_NotificationInvoked;
			_isHandlerAttached = true;
		}
		RefreshRegistrationStatus();
		Log("Sample loaded.");
	}

	private void Page_Unloaded(object sender, RoutedEventArgs e)
	{
		_isLoaded = false;
		if (_isRegistered)
		{
			try
			{
				_manager.Unregister();
			}
			catch (Exception exception)
			{
				LogError("Unregister", exception);
			}
			_isRegistered = false;
		}
		if (_isHandlerAttached)
		{
			_manager.NotificationInvoked -= Manager_NotificationInvoked;
			_isHandlerAttached = false;
		}
	}

	private async void Register_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (!AppNotificationManager.IsSupported())
			{
				Log("App notifications are unsupported on this host.");
				RefreshRegistrationStatus();
				return;
			}
			if (_isRegistered)
			{
				Log("Foreground activation is already registered.");
				return;
			}

			var permissionGranted = await _requestPermissionAsync();
			if (!_isLoaded)
			{
				return;
			}
			_manager.Register();
			_isRegistered = true;
			RefreshRegistrationStatus();
			Log(OperatingSystem.IsBrowser()
				? $"Register completed. Browser permission requested. Setting: {_manager.Setting}."
				: $"Register completed. Platform permission granted: {permissionGranted}. Setting: {_manager.Setting}.");
			await RefreshBrowserSettingAsync();
		}
		catch (Exception exception)
		{
			LogError("Register", exception);
			RefreshRegistrationStatus();
		}
	}

	private void RefreshRegistrationStatus_Click(object sender, RoutedEventArgs e)
	{
		RefreshRegistrationStatus();
		Log("Registration status refreshed.");
	}

	private void Unregister_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (!_isRegistered)
			{
				Log("Foreground activation is not registered.");
				return;
			}
			_manager.Unregister();
			_isRegistered = false;
			RefreshRegistrationStatus();
			Log("Foreground activation was unregistered; persistent notification registration remains.");
		}
		catch (Exception exception)
		{
			LogError("Unregister", exception);
		}
	}

	private void UnregisterAll_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			_manager.UnregisterAll();
			_isRegistered = false;
			RefreshRegistrationStatus();
			Log("All app notification registration was removed. Register again before showing another notification.");
		}
		catch (Exception exception)
		{
			LogError("Unregister all", exception);
		}
	}

	private void Show_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			var notification = CreateProgressNotification();
			_manager.Show(notification);
			_lastNotificationId = notification.Id;
			Log(notification.Id == 0
				? $"Show was a no-op. Setting: {_manager.Setting}. Register and grant permission first."
				: $"Notification {notification.Id} was posted.");
		}
		catch (Exception exception)
		{
			LogError("Show", exception);
		}
	}

	private async void AdvanceProgress_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			_progressValue = _progressValue >= 1 ? 0.25 : Math.Min(1, _progressValue + 0.25);
			var progress = CreateProgressData(++_progressSequence, _progressValue);
			var result = await _manager.UpdateAsync(progress, NotificationTag, NotificationGroup);
			Log($"Progress update result: {result}; value: {_progressValue:P0}.");
		}
		catch (Exception exception)
		{
			LogError("Update progress", exception);
		}
	}

	private async void Schedule_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			var content = new XmlDocument();
			content.LoadXml(CreateScheduledNotification().Payload);
			var deliveryTime = DateTimeOffset.Now.AddSeconds(10);
			var scheduled = new ScheduledToastNotification(content, deliveryTime)
			{
				Tag = ScheduledTag,
				Group = NotificationGroup,
			};
			ToastNotificationManager.CreateToastNotifier().AddToSchedule(scheduled);
			Log($"Notification scheduled for {deliveryTime:T}.");
			await RefreshHistoryCore();
		}
		catch (Exception exception)
		{
			LogError("Schedule", exception);
		}
	}

	private async void RefreshHistory_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			await RefreshHistoryCore();
		}
		catch (Exception exception)
		{
			LogError("Refresh history", exception);
		}
	}

	private async void RemoveLast_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_lastNotificationId == 0)
			{
				Log("No posted notification ID is available to remove.");
				return;
			}
			await _manager.RemoveByIdAsync(_lastNotificationId);
			Log($"Notification {_lastNotificationId} was removed.");
			_lastNotificationId = 0;
			await RefreshHistoryCore();
		}
		catch (Exception exception)
		{
			LogError("Remove last", exception);
		}
	}

	private async void RemoveAll_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			await _manager.RemoveAllAsync();
			_lastNotificationId = 0;
			Log("All active app notifications were removed.");
			await RefreshHistoryCore();
		}
		catch (Exception exception)
		{
			LogError("Remove all", exception);
		}
	}

	private async void RemoveScheduled_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			var notifier = ToastNotificationManager.CreateToastNotifier();
			var scheduled = notifier.GetScheduledToastNotifications();
			foreach (var notification in scheduled)
			{
				notifier.RemoveFromSchedule(notification);
			}
			Log($"Removed {scheduled.Count} scheduled notification(s).");
			await RefreshHistoryCore();
		}
		catch (Exception exception)
		{
			LogError("Remove scheduled", exception);
		}
	}

	private AppNotification CreateProgressNotification()
	{
		var notification = new AppNotificationBuilder()
			.AddArgument("action", "open-sample")
			.AddText(TitleTextBox.Text)
			.AddText(BodyTextBox.Text)
			.AddProgressBar(new AppNotificationProgressBar()
				.BindTitle()
				.BindValue()
				.BindValueStringOverride()
				.BindStatus())
			.AddButton(new AppNotificationButton("Open sample").AddArgument("action", "open-sample"))
			.SetTag(NotificationTag)
			.SetGroup(NotificationGroup)
			.BuildNotification();
		notification.Progress = CreateProgressData(_progressSequence, _progressValue);
		return notification;
	}

	private AppNotification CreateScheduledNotification()
		=> new AppNotificationBuilder()
			.AddArgument("action", "scheduled-sample")
			.AddText(TitleTextBox.Text)
			.AddText("This notification was scheduled 10 seconds ago.")
			.SetTag(ScheduledTag)
			.SetGroup(NotificationGroup)
			.BuildNotification();

	private static AppNotificationProgressData CreateProgressData(uint sequenceNumber, double value)
		=> new(sequenceNumber)
		{
			Title = "Sample progress",
			Status = value >= 1 ? "Complete" : "Working",
			Value = value,
			ValueStringOverride = value.ToString("P0"),
		};

	private async Task RefreshHistoryCore()
	{
		var active = await _manager.GetAllAsync();
		var activeIds = active.Count == 0 ? "none" : string.Join(", ", active.Select(notification => notification.Id));
		if (OperatingSystem.IsBrowser())
		{
			HistoryStatusText.Text = $"Active: {active.Count} ({activeIds})";
		}
		else
		{
			var scheduled = ToastNotificationManager.CreateToastNotifier().GetScheduledToastNotifications();
			HistoryStatusText.Text = $"Active: {active.Count} ({activeIds}) | Scheduled: {scheduled.Count}";
		}
		Log("History refreshed.");
	}

	private void Manager_NotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
	{
		_ = DispatcherQueue.TryEnqueue(() =>
		{
			var decodedArguments = args.Arguments.Count == 0
				? "none"
				: string.Join(", ", args.Arguments.Select(argument => $"{argument.Key}={argument.Value}"));
			ActivationStatusText.Text = $"Raw: {args.Argument}\nDecoded: {decodedArguments}\nUser input: {args.UserInput.Count}";
			Log("Notification activation received.");
		});
	}

	private void RefreshRegistrationStatus()
		=> RegistrationStatusText.Text = $"Supported: {AppNotificationManager.IsSupported()} | Setting: {_manager.Setting} | Foreground registered: {_isRegistered}";

	private async Task RefreshBrowserSettingAsync()
	{
		if (!OperatingSystem.IsBrowser() || _manager.Setting != AppNotificationSetting.DisabledForApplication)
		{
			return;
		}

		for (var attempt = 0; attempt < 40 && _isLoaded; attempt++)
		{
			await Task.Delay(250);
			if (_manager.Setting != AppNotificationSetting.DisabledForApplication)
			{
				RefreshRegistrationStatus();
				Log($"Browser permission resolved. Setting: {_manager.Setting}.");
				return;
			}
		}
	}

	private void LogError(string operation, Exception exception)
		=> Log($"{operation} failed: {exception.GetType().Name}: {exception.Message}");

	private void Log(string message)
	{
		var entry = $"{DateTime.Now:T}  {message}";
		ActivityLogTextBox.Text = string.IsNullOrEmpty(ActivityLogTextBox.Text)
			? entry
			: $"{ActivityLogTextBox.Text}\n{entry}";
	}
}