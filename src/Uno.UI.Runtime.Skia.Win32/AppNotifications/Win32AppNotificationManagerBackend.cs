extern alias winappsdk;

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Internal;
using Uno.Foundation.Logging;
using NativeAppNotification = winappsdk::Microsoft.Windows.AppNotifications.AppNotification;
using NativeAppNotificationManager = winappsdk::Microsoft.Windows.AppNotifications.AppNotificationManager;
using NativeAppNotificationPriority = winappsdk::Microsoft.Windows.AppNotifications.AppNotificationPriority;
using NativeAppNotificationProgressData = winappsdk::Microsoft.Windows.AppNotifications.AppNotificationProgressData;
using NativeBootstrap = winappsdk::Microsoft.Windows.ApplicationModel.DynamicDependency.Bootstrap;
using NativePackageVersion = winappsdk::Microsoft.Windows.ApplicationModel.DynamicDependency.PackageVersion;

namespace Uno.UI.Runtime.Skia.Win32;

internal sealed class Win32AppNotificationManagerBackend : IAppNotificationManagerBackend
{
	private const string NativeTagPrefix = "u";
	private const string NativeGroup = "uno.appnotifications";
	private const uint WindowsAppSdkMajorMinor = (2u << 16) | 3u;
	private static readonly TimeSpan NativeOperationTimeout = TimeSpan.FromSeconds(10);
	private static readonly object _bootstrapGate = new();
	private static bool _bootstrapAttempted;
	private static bool _bootstrapInitialized;
	private readonly object _gate = new();
	private NativeAppNotificationManager? _manager;
	private bool _isRegistered;

	private Win32AppNotificationManagerBackend()
	{
	}

	public static Win32AppNotificationManagerBackend Instance { get; } = new();

	public bool IsSupported
	{
		get
		{
			try
			{
				return EnsureRuntime() && NativeAppNotificationManager.IsSupported();
			}
			catch (Exception exception)
			{
				LogWarning($"Windows App SDK app notifications are unavailable: {exception.Message}");
				return false;
			}
		}
	}

	public AppNotificationSetting Setting
	{
		get
		{
			if (!IsSupported || GetManager() is not { } manager)
			{
				return AppNotificationSetting.Unsupported;
			}
			return (AppNotificationSetting)(int)manager.Setting;
		}
	}

	public string? BootIdentifier => null;

	public void Register()
	{
		lock (_gate)
		{
			var manager = GetManager() ?? throw new InvalidOperationException("Windows App SDK app notifications are unavailable.");
			if (!_isRegistered)
			{
				manager.NotificationInvoked += OnNotificationInvoked;
				try
				{
					manager.Register();
					_isRegistered = true;
				}
				catch
				{
					manager.NotificationInvoked -= OnNotificationInvoked;
					throw;
				}
			}
		}
	}

	public void Register(string displayName, Uri iconUri)
	{
		lock (_gate)
		{
			var manager = GetManager() ?? throw new InvalidOperationException("Windows App SDK app notifications are unavailable.");
			if (!_isRegistered)
			{
				manager.NotificationInvoked += OnNotificationInvoked;
				try
				{
					manager.Register(displayName, iconUri);
					_isRegistered = true;
				}
				catch
				{
					manager.NotificationInvoked -= OnNotificationInvoked;
					throw;
				}
			}
		}
	}

	public void Unregister()
	{
		lock (_gate)
		{
			if (_isRegistered && _manager is { } manager)
			{
				manager.Unregister();
				manager.NotificationInvoked -= OnNotificationInvoked;
				_isRegistered = false;
			}
		}
	}

	public void UnregisterAll()
	{
		lock (_gate)
		{
			if (_manager is { } manager)
			{
				manager.UnregisterAll();
				manager.NotificationInvoked -= OnNotificationInvoked;
			}
			_isRegistered = false;
		}
	}

	public bool TryShow(AppNotificationEnvelope notification)
	{
		if (!EnsureRegistered())
		{
			return false;
		}
		var native = CreateNativeNotification(notification);
		_manager!.Show(native);
		return native.Id != 0;
	}

	public bool TryUpdate(AppNotificationStateRecord notification)
	{
		if (!EnsureRegistered())
		{
			return false;
		}
		var native = CreateNativeNotification(notification.ToEnvelope());
		_manager!.Show(native);
		return native.Id != 0;
	}

	public void Remove(AppNotificationStateRecord notification)
	{
		if (EnsureRegistered())
		{
			Wait(_manager!.RemoveByTagAndGroupAsync(GetNativeTag(notification.Id), NativeGroup));
		}
	}

	public void RemoveAll()
	{
		if (EnsureRegistered())
		{
			Wait(_manager!.RemoveByGroupAsync(NativeGroup));
		}
	}

	public IReadOnlyCollection<uint>? GetActiveNotificationIds()
	{
		if (!EnsureRegistered())
		{
			return null;
		}
		return Wait(_manager!.GetAllAsync())
			.Where(notification => notification.Group == NativeGroup)
			.Select(notification => TryParseNativeTag(notification.Tag, out var id) ? id : 0)
			.Where(id => id != 0)
			.Distinct()
			.ToArray();
	}

	private NativeAppNotificationManager? GetManager()
	{
		lock (_gate)
		{
			if (_manager is null && IsSupported)
			{
				_manager = NativeAppNotificationManager.Default;
			}
			return _manager;
		}
	}

	private bool EnsureRegistered()
	{
		try
		{
			Register();
			return true;
		}
		catch (Exception exception)
		{
			LogWarning($"Windows App SDK app notification registration failed: {exception.Message}");
			return false;
		}
	}

	private static NativeAppNotification CreateNativeNotification(AppNotificationEnvelope notification)
	{
		var native = new NativeAppNotification(notification.RawPayload.Length > 0 ? notification.RawPayload : BuildFallbackPayload(notification))
		{
			Tag = GetNativeTag(notification.Id),
			Group = NativeGroup,
			Expiration = notification.Expiration,
			ExpiresOnReboot = notification.ExpiresOnReboot,
			SuppressDisplay = notification.SuppressDisplay,
			Priority = notification.Priority == AppNotificationPriority.High
				? NativeAppNotificationPriority.High
				: NativeAppNotificationPriority.Default,
		};
		if (notification.Progress is { } progress)
		{
			native.Progress = CreateProgressData(progress);
		}
		return native;
	}

	private static NativeAppNotificationProgressData CreateProgressData(AppNotificationProgressSnapshot progress)
		=> new(progress.SequenceNumber)
		{
			Title = progress.Title,
			Value = progress.Value,
			ValueStringOverride = progress.ValueStringOverride,
			Status = progress.Status,
		};

	private static string BuildFallbackPayload(AppNotificationEnvelope notification)
		=> $"<toast><visual><binding template='ToastGeneric'><text>{System.Security.SecurityElement.Escape(notification.Payload.Title?.Content ?? string.Empty)}</text><text>{System.Security.SecurityElement.Escape(notification.Payload.Body?.Content ?? string.Empty)}</text></binding></visual></toast>";

	private static string GetNativeTag(uint id)
		=> NativeTagPrefix + id.ToString("x8", System.Globalization.CultureInfo.InvariantCulture);

	private static bool TryParseNativeTag(string value, out uint id)
	{
		id = 0;
		return value.Length == 9 && value[0] == NativeTagPrefix[0] &&
			uint.TryParse(value.AsSpan(1), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out id) &&
			id != 0;
	}

	private static bool EnsureRuntime()
	{
		lock (_bootstrapGate)
		{
			if (_bootstrapAttempted)
			{
				return _bootstrapInitialized;
			}
			_bootstrapAttempted = true;
			try
			{
				var options = NativeBootstrap.InitializeOptions.OnPackageIdentity_NOOP;
				_bootstrapInitialized = NativeBootstrap.TryInitialize(
					WindowsAppSdkMajorMinor,
					string.Empty,
					new NativePackageVersion(2, 3, 0, 0),
					options,
					out var error);
				if (!_bootstrapInitialized)
				{
					LogWarning($"Windows App SDK bootstrap failed with HRESULT 0x{error:X8}.");
				}
			}
			catch (DllNotFoundException exception)
			{
				LogWarning($"Windows App SDK bootstrap DLL is unavailable: {exception.Message}");
			}
			catch (Exception exception)
			{
				LogWarning($"Windows App SDK bootstrap failed: {exception.Message}");
			}
			return _bootstrapInitialized;
		}
	}

	private void OnNotificationInvoked(NativeAppNotificationManager sender, winappsdk::Microsoft.Windows.AppNotifications.AppNotificationActivatedEventArgs args)
		=> AppNotificationActivationBroker.Publish(new AppNotificationActivation(
			args.Argument ?? string.Empty,
			new Dictionary<string, string>(args.UserInput ?? new Dictionary<string, string>())));

	private static void Wait(winappsdk::Windows.Foundation.IAsyncAction action)
	{
		using var completed = new ManualResetEventSlim();
		action.Completed = (_, _) => completed.Set();
		if (!completed.Wait(NativeOperationTimeout))
		{
			action.Cancel();
			throw new TimeoutException("Timed out while waiting for a Windows app-notification operation.");
		}
		action.GetResults();
	}

	private static T Wait<T>(winappsdk::Windows.Foundation.IAsyncOperation<T> operation)
	{
		using var completed = new ManualResetEventSlim();
		operation.Completed = (_, _) => completed.Set();
		if (!completed.Wait(NativeOperationTimeout))
		{
			operation.Cancel();
			throw new TimeoutException("Timed out while waiting for a Windows app-notification operation.");
		}
		return operation.GetResults();
	}

	private static void LogWarning(string message)
	{
		if (typeof(Win32AppNotificationManagerBackend).Log().IsEnabled(LogLevel.Warning))
		{
			typeof(Win32AppNotificationManagerBackend).Log().LogWarning(message);
		}
	}
}