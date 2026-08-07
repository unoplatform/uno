#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using Uno.Foundation.Logging;
using UserNotifications;
using Windows.UI.Notifications.Internal;

namespace Microsoft.Windows.AppNotifications.Internal;

internal static class AppleAppNotificationRuntime
{
	private static readonly object _gate = new();
	private static readonly TimeSpan NativeOperationTimeout = TimeSpan.FromSeconds(10);
	private static AppleUserNotificationCenterDelegate? _delegate;
	private static AppleAppNotificationAuthorizationStatus _authorizationStatus;

	public static AppNotificationSetting Setting
		=> AppleAppNotificationSettingEvaluator.Evaluate(_authorizationStatus);

	public static void InitializeEarly()
	{
		lock (_gate)
		{
			var center = UNUserNotificationCenter.Current;
			if (_delegate is not null && ReferenceEquals(center.Delegate, _delegate))
			{
				RefreshSettings(center);
				return;
			}

			var previous = center.Delegate;
			if (previous is not null && previous is not UNUserNotificationCenterDelegate)
			{
				LogWarning("An existing notification-center delegate could not be chained, so Uno app-notification activation was not installed.");
				RefreshSettings(center);
				return;
			}

			_delegate = new AppleUserNotificationCenterDelegate(previous as UNUserNotificationCenterDelegate);
			center.Delegate = _delegate;
			RefreshSettings(center);
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
			var request = UNNotificationRequest.FromIdentifier(
				command.RequestIdentifier,
				AppleAppNotificationNativeContent.Create(command),
				trigger);
			return AddRequest(request);
		}
	}

	public static void Remove(string requestIdentifier)
	{
		var identifiers = new[] { requestIdentifier };
		var center = UNUserNotificationCenter.Current;
		center.RemovePendingNotificationRequests(identifiers);
		center.RemoveDeliveredNotifications(identifiers);
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

	private static bool EnsureCategory(AppleAppNotificationCommand command)
	{
		var category = AppleAppNotificationNativeContent.CreateCategory(command);
		if (category is null)
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
		var completion = new TaskCompletionSource<NSError?>(TaskCreationOptions.RunContinuationsAsynchronously);
		var timedOut = 0;
		var center = UNUserNotificationCenter.Current;
		center.AddNotificationRequest(request, error =>
		{
			completion.TrySetResult(error);
			if (error is null && Volatile.Read(ref timedOut) != 0)
			{
				center.RemovePendingNotificationRequests(new[] { request.Identifier });
				center.RemoveDeliveredNotifications(new[] { request.Identifier });
			}
		});
		if (!completion.Task.Wait(NativeOperationTimeout))
		{
			Interlocked.Exchange(ref timedOut, 1);
			center.RemovePendingNotificationRequests(new[] { request.Identifier });
			center.RemoveDeliveredNotifications(new[] { request.Identifier });
			LogWarning("Timed out while adding an Apple app notification.");
			return false;
		}
		if (completion.Task.Result is { } error)
		{
			LogWarning($"Apple rejected the app notification: {error.LocalizedDescription}");
			return false;
		}
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

	private static void RefreshSettings(UNUserNotificationCenter center)
		=> center.GetNotificationSettings(settings =>
		{
			_authorizationStatus = settings.AuthorizationStatus switch
			{
				UNAuthorizationStatus.Denied => AppleAppNotificationAuthorizationStatus.Denied,
				UNAuthorizationStatus.Authorized => AppleAppNotificationAuthorizationStatus.Authorized,
				UNAuthorizationStatus.Provisional => AppleAppNotificationAuthorizationStatus.Provisional,
				UNAuthorizationStatus.Ephemeral => AppleAppNotificationAuthorizationStatus.Ephemeral,
				_ => AppleAppNotificationAuthorizationStatus.NotDetermined,
			};
		});

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