#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Foundation;
using Uno.Foundation.Logging;
using UIKit;
using UserNotifications;
using Windows.UI.Notifications.Internal;

namespace Microsoft.Windows.AppNotifications.Internal;

internal static class AppleAppNotificationRuntime
{
	private static readonly object _gate = new();
	private static readonly TimeSpan NativeOperationTimeout = TimeSpan.FromSeconds(10);
	private static readonly AppleAppNotificationSettingCache _settingCache = new();
	private static readonly List<NSObject> _settingsRefreshObservers = new();
	private static readonly HashSet<string> _registeredCategoryIdentifiers = new(StringComparer.Ordinal);
	private static AppleUserNotificationCenterDelegate? _delegate;

	public static AppNotificationSetting Setting
	{
		get
		{
			InitializeEarly();
			if (!_settingCache.TryWaitForCurrentRefresh(NativeOperationTimeout, out var status))
			{
				LogWarning("Timed out while reading Apple notification settings.");
				return AppNotificationSetting.DisabledForApplication;
			}
			return AppleAppNotificationSettingEvaluator.Evaluate(status);
		}
	}

	public static void InitializeEarly()
	{
		lock (_gate)
		{
			var center = UNUserNotificationCenter.Current;
			EnsureSettingsRefreshObservers();
			if (_delegate is not null && ReferenceEquals(center.Delegate, _delegate))
			{
				EnsureSettingsRefreshStarted(center);
				return;
			}

			var previous = center.Delegate;
			if (previous is not null && previous is not UNUserNotificationCenterDelegate)
			{
				LogWarning("An existing notification-center delegate could not be chained, so Uno app-notification activation was not installed.");
				EnsureSettingsRefreshStarted(center);
				return;
			}

			_delegate = new AppleUserNotificationCenterDelegate(previous as UNUserNotificationCenterDelegate);
			center.Delegate = _delegate;
			EnsureSettingsRefreshStarted(center);
		}
	}

	public static void RequestAuthorization()
	{
		InitializeEarly();
		UNUserNotificationCenter.Current.RequestAuthorization(
			UNAuthorizationOptions.Alert | UNAuthorizationOptions.Sound | UNAuthorizationOptions.Badge,
			(granted, error) =>
			{
				if (error is not null)
				{
					LogWarning($"Apple rejected app-notification authorization: {error.LocalizedDescription}");
				}
				RefreshSettings(UNUserNotificationCenter.Current);
			});
	}

	public static bool TryPost(AppleAppNotificationCommand command, UNNotificationTrigger? trigger = null)
	{
		InitializeEarly();
		lock (_gate)
		{
			if (!EnsureCategory(command))
			{
				return false;
			}
			try
			{
				return AppleAppNotificationPosting.TryPost(
					command,
					postingCommand => GetReplacedRequestIdentifiers(postingCommand),
					postingCommand => AddRequest(UNNotificationRequest.FromIdentifier(
						postingCommand.RequestIdentifier,
						AppleAppNotificationNativeContent.Create(postingCommand),
						trigger)),
					Remove);
			}
			catch (TimeoutException exception)
			{
				LogWarning(exception.Message);
				return false;
			}
		}
	}

	public static void Remove(string requestIdentifier)
	{
		var identifiers = new[] { requestIdentifier };
		var center = UNUserNotificationCenter.Current;
		center.RemovePendingNotificationRequests(identifiers);
		center.RemoveDeliveredNotifications(identifiers);
	}

	public static void RemoveNotification(uint id)
	{
		Remove(AppleAppNotificationTranslator.GetNotificationRequestIdentifier(id));
		RemoveAll(AppleAppNotificationTranslator.GetNotificationRequestIdentifierPrefix(id));
	}

	public static void RemoveScheduled(string scheduleIdentifier)
	{
		Remove(AppleAppNotificationTranslator.GetScheduledRequestIdentifier(scheduleIdentifier));
		RemoveAll(AppleAppNotificationTranslator.GetScheduledRequestIdentifierPrefix(scheduleIdentifier));
	}

	public static void RemoveAll(string requestIdentifierPrefix)
	{
		var center = UNUserNotificationCenter.Current;
		var pending = GetPendingRequests()
			.Where(request => request.Identifier.StartsWith(requestIdentifierPrefix, StringComparison.Ordinal))
			.Select(request => request.Identifier)
			.ToArray();
		var delivered = GetDeliveredNotifications()
			.Where(notification => notification.Request.Identifier.StartsWith(requestIdentifierPrefix, StringComparison.Ordinal))
			.Select(notification => notification.Request.Identifier)
			.ToArray();
		if (pending.Length > 0)
		{
			center.RemovePendingNotificationRequests(pending);
		}
		if (delivered.Length > 0)
		{
			center.RemoveDeliveredNotifications(delivered);
		}
	}

	public static IReadOnlyCollection<uint>? GetActiveNotificationIds()
	{
		try
		{
			return GetPendingRequests()
				.Select(request => request.Identifier)
				.Concat(GetDeliveredNotifications().Select(notification => notification.Request.Identifier))
				.Select(identifier => AppleAppNotificationTranslator.TryGetNotificationId(identifier, out var id) ? id : 0)
				.Where(id => id != 0)
				.Distinct()
				.ToArray();
		}
		catch (Exception exception)
		{
			LogWarning($"Apple app-notification history could not be read: {exception.Message}");
			return null;
		}
	}

	public static IReadOnlyCollection<string>? GetPendingScheduleIdentifiers()
	{
		try
		{
			return GetScheduleIdentifiers(GetPendingRequests().Select(request => request.Identifier));
		}
		catch (Exception exception)
		{
			LogWarning($"Apple pending notification schedules could not be read: {exception.Message}");
			return null;
		}
	}

	public static IReadOnlyCollection<string>? GetDeliveredScheduleIdentifiers()
	{
		try
		{
			return GetScheduleIdentifiers(GetDeliveredNotifications().Select(notification => notification.Request.Identifier));
		}
		catch (Exception exception)
		{
			LogWarning($"Apple delivered notification schedules could not be read: {exception.Message}");
			return null;
		}
	}

	private static IReadOnlyCollection<string> GetScheduleIdentifiers(IEnumerable<string> identifiers)
		=> identifiers
			.Select(identifier => AppleAppNotificationTranslator.TryGetScheduleIdentifier(identifier, out var scheduleIdentifier) ? scheduleIdentifier : string.Empty)
			.Where(identifier => identifier.Length > 0)
			.Distinct(StringComparer.Ordinal)
			.ToArray();

	private static IReadOnlyCollection<string> GetReplacedRequestIdentifiers(AppleAppNotificationCommand command)
		=> AppleAppNotificationTranslator.GetReplacedRequestIdentifiers(
			command,
			GetPendingRequests()
				.Select(request => request.Identifier)
				.Concat(GetDeliveredNotifications().Select(notification => notification.Request.Identifier)));

	private static void Remove(IReadOnlyCollection<string> requestIdentifiers)
	{
		if (requestIdentifiers.Count == 0)
		{
			return;
		}
		var identifiers = requestIdentifiers.ToArray();
		var center = UNUserNotificationCenter.Current;
		center.RemovePendingNotificationRequests(identifiers);
		center.RemoveDeliveredNotifications(identifiers);
	}

	private static bool EnsureCategory(AppleAppNotificationCommand command)
	{
		var category = AppleAppNotificationNativeContent.CreateCategory(command);
		if (category is null)
		{
			return true;
		}

		// The identifier is derived from the action set, so an identifier already registered in this
		// process describes an equivalent category and does not need another native round-trip.
		if (_registeredCategoryIdentifiers.Contains(category.Identifier))
		{
			return true;
		}

		try
		{
			var center = UNUserNotificationCenter.Current;
			var completion = new TaskCompletionSource<NSSet<UNNotificationCategory>>(TaskCreationOptions.RunContinuationsAsynchronously);
			center.GetNotificationCategories(categories => completion.TrySetResult(categories));
			if (!completion.Task.Wait(NativeOperationTimeout))
			{
				LogWarning("Timed out while reading Apple notification categories.");
				return false;
			}
			var categories = completion.Task.Result.ToArray()
				.Where(item => item.Identifier != category.Identifier)
				.Append(category)
				.ToArray();
			center.SetNotificationCategories(new NSSet<UNNotificationCategory>(categories));
			_registeredCategoryIdentifiers.Add(category.Identifier);
			return true;
		}
		catch (Exception exception)
		{
			LogWarning($"Apple notification actions could not be registered: {exception.Message}");
			return false;
		}
	}

	private static bool AddRequest(UNNotificationRequest request)
	{
		// Immediate notifications complete on the UI thread, so waiting here prevents their presentation.
		UNUserNotificationCenter.Current.AddNotificationRequest(request, error =>
		{
			if (error is not null)
			{
				LogWarning($"Apple rejected the app notification: {error.LocalizedDescription}");
			}
		});
		return true;
	}

	private static UNNotificationRequest[] GetPendingRequests()
	{
		var completion = new TaskCompletionSource<UNNotificationRequest[]>(TaskCreationOptions.RunContinuationsAsynchronously);
		UNUserNotificationCenter.Current.GetPendingNotificationRequests(requests => completion.TrySetResult(requests ?? Array.Empty<UNNotificationRequest>()));
		return completion.Task.Wait(NativeOperationTimeout)
			? completion.Task.Result
			: throw new TimeoutException("Timed out while reading pending Apple notifications.");
	}

	private static UNNotification[] GetDeliveredNotifications()
	{
		var completion = new TaskCompletionSource<UNNotification[]>(TaskCreationOptions.RunContinuationsAsynchronously);
		UNUserNotificationCenter.Current.GetDeliveredNotifications(notifications => completion.TrySetResult(notifications ?? Array.Empty<UNNotification>()));
		return completion.Task.Wait(NativeOperationTimeout)
			? completion.Task.Result
			: throw new TimeoutException("Timed out while reading delivered Apple notifications.");
	}

	private static void EnsureSettingsRefreshObservers()
	{
		if (_settingsRefreshObservers.Count > 0)
		{
			return;
		}

		var center = NSNotificationCenter.DefaultCenter;
		_settingsRefreshObservers.Add(center.AddObserver(
			UIApplication.WillEnterForegroundNotification,
			_ => RefreshSettings(UNUserNotificationCenter.Current)));
		_settingsRefreshObservers.Add(center.AddObserver(
			UIApplication.DidBecomeActiveNotification,
			_ => RefreshSettings(UNUserNotificationCenter.Current)));
		if (OperatingSystem.IsIOSVersionAtLeast(13) || OperatingSystem.IsMacCatalystVersionAtLeast(13))
		{
			_settingsRefreshObservers.Add(center.AddObserver(
				UIScene.WillEnterForegroundNotification,
				_ => RefreshSettings(UNUserNotificationCenter.Current)));
			_settingsRefreshObservers.Add(center.AddObserver(
				UIScene.DidActivateNotification,
				_ => RefreshSettings(UNUserNotificationCenter.Current)));
		}
	}

	private static void EnsureSettingsRefreshStarted(UNUserNotificationCenter center)
	{
		if (!_settingCache.HasRefresh)
		{
			RefreshSettings(center);
		}
	}

	private static void RefreshSettings(UNUserNotificationCenter center)
	{
		var generation = _settingCache.BeginRefresh();
		center.GetNotificationSettings(settings =>
		{
			_settingCache.CompleteRefresh(generation, settings.AuthorizationStatus switch
			{
				UNAuthorizationStatus.Denied => AppleAppNotificationAuthorizationStatus.Denied,
				UNAuthorizationStatus.Authorized => AppleAppNotificationAuthorizationStatus.Authorized,
				UNAuthorizationStatus.Provisional => AppleAppNotificationAuthorizationStatus.Provisional,
				UNAuthorizationStatus.Ephemeral => AppleAppNotificationAuthorizationStatus.Ephemeral,
				_ => AppleAppNotificationAuthorizationStatus.NotDetermined,
			});
		});
	}

	private static void LogWarning(string message)
	{
		if (typeof(AppleAppNotificationRuntime).Log().IsEnabled(LogLevel.Warning))
		{
			typeof(AppleAppNotificationRuntime).Log().LogWarning(message);
		}
	}

	private sealed class AppleUserNotificationCenterDelegate : UNUserNotificationCenterDelegate
	{
		private readonly UNUserNotificationCenterDelegate? _previous;

		public AppleUserNotificationCenterDelegate(UNUserNotificationCenterDelegate? previous)
		{
			_previous = previous;
		}

		public override void WillPresentNotification(
			UNUserNotificationCenter center,
			UNNotification notification,
			Action<UNNotificationPresentationOptions> completionHandler)
		{
			var identifier = notification.Request.Identifier;
			if (!IsUnoRequest(identifier))
			{
				if (_previous is not null)
				{
					_previous.WillPresentNotification(center, notification, completionHandler);
				}
				else
				{
					completionHandler(UNNotificationPresentationOptions.None);
				}
				return;
			}

			try
			{
				CompleteSchedule(identifier);
				var content = notification.Request.Content;
				if (AppleAppNotificationNativeContent.IsSuppressed(content))
				{
					completionHandler(UNNotificationPresentationOptions.List);
					return;
				}
				var presentation = OperatingSystem.IsIOSVersionAtLeast(14) || OperatingSystem.IsMacCatalystVersionAtLeast(14)
					? UNNotificationPresentationOptions.List | UNNotificationPresentationOptions.Banner
					: UNNotificationPresentationOptions.Alert;
				if (!AppleAppNotificationNativeContent.IsMuted(content))
				{
					presentation |= UNNotificationPresentationOptions.Sound;
				}
				completionHandler(presentation);
			}
			catch (Exception exception)
			{
				LogWarning($"Apple app-notification presentation failed: {exception.Message}");
				completionHandler(UNNotificationPresentationOptions.None);
			}
		}

		public override void DidReceiveNotificationResponse(
			UNUserNotificationCenter center,
			UNNotificationResponse response,
			Action completionHandler)
		{
			var identifier = response.Notification.Request.Identifier;
			if (!IsUnoRequest(identifier))
			{
				if (_previous is not null)
				{
					_previous.DidReceiveNotificationResponse(center, response, completionHandler);
				}
				else
				{
					completionHandler();
				}
				return;
			}

			try
			{
				CompleteSchedule(identifier);
				if (AppleAppNotificationNativeContent.TryGetActivation(response, out var argument, out var protocolUri, out var userInput))
				{
					if (protocolUri is not null && Uri.TryCreate(protocolUri, UriKind.Absolute, out var uri))
					{
						_ = global::Windows.System.Launcher.LaunchUriPlatformAsync(uri);
					}
					else
					{
						AppNotificationActivationBroker.Publish(new AppNotificationActivation(argument, userInput));
					}
				}
			}
			catch (Exception exception)
			{
				LogWarning($"Apple app-notification activation failed: {exception.Message}");
			}
			finally
			{
				completionHandler();
			}
		}

		public override void OpenSettings(UNUserNotificationCenter center, UNNotification? notification)
			=> _previous?.OpenSettings(center, notification);

		private static bool IsUnoRequest(string identifier)
			=> identifier.StartsWith(AppleAppNotificationTranslator.RequestIdentifierPrefix, StringComparison.Ordinal) ||
				identifier.StartsWith(AppleAppNotificationTranslator.ScheduledRequestIdentifierPrefix, StringComparison.Ordinal);

		private static void CompleteSchedule(string requestIdentifier)
		{
			if (AppleAppNotificationTranslator.TryGetScheduleIdentifier(requestIdentifier, out var scheduleIdentifier))
			{
				ToastNotificationSchedulerRuntime.CompleteNativeDelivery(scheduleIdentifier);
			}
		}
	}
}