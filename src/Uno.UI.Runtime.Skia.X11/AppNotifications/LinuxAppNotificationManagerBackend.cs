#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Internal;
using Tmds.DBus.Protocol;
using Uno.Foundation.Logging;
using Uno.WinUI.Runtime.Skia.X11.DBus;
using Windows.ApplicationModel;
using DBusProxy = Uno.WinUI.Runtime.Skia.X11.DBus.DBus;

namespace Uno.WinUI.Runtime.Skia.X11;

internal sealed class LinuxAppNotificationManagerBackend : IAppNotificationManagerBackend
{
	private const string ServiceName = "org.freedesktop.Notifications";
	private const string ServicePath = "/org/freedesktop/Notifications";
	private const string BusServiceName = "org.freedesktop.DBus";
	private const string BusServicePath = "/org/freedesktop/DBus";
	private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(10);
	private static readonly IReadOnlyCollection<uint> EmptyNotificationIds = Array.Empty<uint>();
	private readonly object _initializationGate = new();
	private readonly LinuxAppNotificationNativeStateStore _nativeState = LinuxAppNotificationNativeStateStoreFactory.Create();
	private Task<ConnectionContext?>? _initialization;

	private LinuxAppNotificationManagerBackend()
	{
	}

	public static LinuxAppNotificationManagerBackend Instance { get; } = new();

	public bool IsSupported => GetContext() is not null;

	public AppNotificationSetting Setting => IsSupported ? AppNotificationSetting.Enabled : AppNotificationSetting.Unsupported;

	public string? BootIdentifier => null;

	public void Register() => _ = GetContext();

	public void Register(string displayName, Uri iconUri) => Register();

	public void Unregister()
	{
	}

	public void UnregisterAll()
	{
	}

	public bool TryShow(AppNotificationEnvelope notification)
		=> TryPost(LinuxAppNotificationTranslator.Translate(notification, DateTimeOffset.UtcNow), replaceExisting: false);

	public bool TryUpdate(AppNotificationStateRecord notification)
		=> TryPost(LinuxAppNotificationTranslator.Translate(notification.ToEnvelope(), DateTimeOffset.UtcNow), replaceExisting: true);

	public void Remove(AppNotificationStateRecord notification)
	{
		if (GetContext() is not { } context)
		{
			throw new InvalidOperationException("The Linux notification service is unavailable.");
		}
		LinuxAppNotificationNativeStateSession nativeState;
		try
		{
			nativeState = GetNativeState(context);
			EnsureServerOwner(context);
			if (nativeState.GetNativeId(notification.Id) is not { } nativeId)
			{
				return;
			}
			Wait(context.Notifications.CloseNotificationAsync(nativeId));
		}
		catch (Exception exception)
		{
			InvalidateContext(context);
			LogWarning($"Linux could not close app notification {notification.Id}: {exception.Message}");
			throw;
		}
		nativeState.RemoveByNotificationId(notification.Id);
	}

	public void RemoveAll()
	{
		var context = GetContext()
			?? throw new InvalidOperationException("The Linux notification service is unavailable.");
		LinuxAppNotificationNativeStateSession nativeState;
		try
		{
			EnsureServerOwner(context);
			nativeState = GetNativeState(context);
		}
		catch (Exception exception)
		{
			InvalidateContext(context);
			LogWarning($"Linux could not read app notifications before closing them: {exception.Message}");
			throw;
		}
		var records = nativeState.GetAll();
		if (records.Count == 0)
		{
			return;
		}
		Exception? failure = null;
		foreach (var record in records)
		{
			try
			{
				EnsureServerOwner(context);
				Wait(context.Notifications.CloseNotificationAsync(record.NativeId));
			}
			catch (Exception exception)
			{
				failure = exception;
				InvalidateContext(context);
				LogWarning($"Linux could not close app notification {record.NotificationId}: {exception.Message}");
				break;
			}
		}
		if (failure is not null)
		{
			throw new InvalidOperationException("Linux could not close all app notifications.", failure);
		}
		nativeState.RemoveAll();
	}

	public IReadOnlyCollection<uint>? GetActiveNotificationIds()
	{
		if (GetContext() is not { } context)
		{
			return EmptyNotificationIds;
		}
		try
		{
			EnsureServerOwner(context);
			return GetNativeState(context).GetAll().Select(record => record.NotificationId).ToArray();
		}
		catch (Exception exception)
		{
			InvalidateContext(context);
			LogWarning($"Linux app-notification history could not be read: {exception.Message}");
			return EmptyNotificationIds;
		}
	}

	private bool TryPost(LinuxAppNotificationCommand command, bool replaceExisting)
	{
		var context = GetContext();
		if (context is null)
		{
			return false;
		}

		uint nativeId = 0;
		try
		{
			EnsureServerOwner(context);
			var nativeState = GetNativeState(context);
			var replacesId = replaceExisting ? nativeState.GetNativeId(command.Id) ?? 0 : 0;
			var actions = BuildActions(command, context.Capabilities);
			var hints = BuildHints(command, context.Capabilities);
			nativeId = Wait(context.Notifications.NotifyAsync(
				GetApplicationName(),
				replacesId,
				LinuxAppNotificationAssetResolver.ResolveIcon(command.AppIcon),
				command.Summary,
				context.Capabilities.Contains("body") ? FormatBody(command.Body, context.Capabilities) : string.Empty,
				actions,
				hints,
				command.ExpireTimeoutMilliseconds));
			if (nativeId == 0)
			{
				return false;
			}
			if (!nativeState.TrySet(command.Id, nativeId, command))
			{
				throw new InvalidOperationException("The Linux notification connection changed while posting.");
			}
			if (command.UnsupportedFeatures.Length > 0)
			{
				LogWarning($"Linux app notifications do not support {string.Join(", ", command.UnsupportedFeatures)}; those features were ignored.");
			}
			return true;
		}
		catch (Exception exception)
		{
			if (nativeId != 0)
			{
				try
				{
					Wait(context.Notifications.CloseNotificationAsync(nativeId));
				}
				catch
				{
				}
			}
			InvalidateContext(context);
			LogWarning($"Linux rejected the app notification: {exception.Message}");
			return false;
		}
	}

	private ConnectionContext? GetContext()
	{
		Task<ConnectionContext?> initialization;
		lock (_initializationGate)
		{
			initialization = _initialization ??= Task.Run(InitializeAsync);
		}
		try
		{
			if (!initialization.Wait(OperationTimeout))
			{
				AbandonInitialization(initialization);
				return null;
			}
			var context = initialization.Result;
			if (context is null)
			{
				ResetInitialization(initialization);
				return null;
			}

			var isCurrent = false;
			lock (_initializationGate)
			{
				if (ReferenceEquals(_initialization, initialization))
				{
					if (context.IsDisposed)
					{
						_initialization = null;
					}
					else
					{
						if (context.NativeState is null)
						{
							context.Activate(_nativeState);
						}
						isCurrent = true;
					}
				}
			}
			if (!isCurrent)
			{
				context.Dispose();
				return null;
			}
			return context;
		}
		catch (Exception exception)
		{
			ResetInitialization(initialization);
			LogWarning($"Linux notification service initialization failed: {exception.GetBaseException().Message}");
			return null;
		}
	}

	private async Task<ConnectionContext?> InitializeAsync()
	{
		if (DBusAddress.Session is not { } address)
		{
			return null;
		}
		var connection = new DBusConnection(address);
		ConnectionContext? context = null;
		try
		{
			await connection.ConnectAsync().ConfigureAwait(false);
			var bus = new DBusService(connection, BusServiceName).CreateDBus(BusServicePath);
			if (!await bus.NameHasOwnerAsync(ServiceName).ConfigureAwait(false))
			{
				connection.Dispose();
				return null;
			}
			var owner = await bus.GetNameOwnerAsync(ServiceName).ConfigureAwait(false);
			var notifications = new DBusService(connection, owner).CreateNotifications(ServicePath);
			var capabilities = (await notifications.GetCapabilitiesAsync().ConfigureAwait(false)).ToHashSet(StringComparer.Ordinal);
			context = new ConnectionContext(connection, bus, notifications, capabilities, owner);
			var capturedContext = context;
			context.Subscriptions.Add(await notifications.WatchNotificationClosedAsync(
				(exception, signal) => OnNotificationClosed(capturedContext, exception, signal),
				emitOnCapturedContext: false).ConfigureAwait(false));
			context.Subscriptions.Add(await notifications.WatchActionInvokedAsync(
				(exception, signal) => OnActionInvoked(capturedContext, exception, signal),
				emitOnCapturedContext: false).ConfigureAwait(false));
			context.Subscriptions.Add(await notifications.WatchActivationTokenAsync(
				(exception, signal) => OnActivationToken(capturedContext, exception, signal),
				emitOnCapturedContext: false).ConfigureAwait(false));
			return context;
		}
		catch
		{
			if (context is not null)
			{
				context.Dispose();
			}
			else
			{
				connection.Dispose();
			}
			throw;
		}
	}

	private void EnsureServerOwner(ConnectionContext context)
	{
		var nativeState = GetNativeState(context);
		var owner = Wait(context.Bus.GetNameOwnerAsync(ServiceName));
		if (!string.Equals(owner, nativeState.ServerOwner, StringComparison.Ordinal))
		{
			InvalidateContext(context);
			throw new InvalidOperationException("The Linux notification service restarted.");
		}
	}

	private void OnNotificationClosed(ConnectionContext context, Exception? exception, (uint Id, uint Reason) signal)
	{
		if (exception is not null)
		{
			InvalidateContext(context, deferDisposal: true);
			LogWarning($"Linux notification closure signal failed: {exception.Message}");
			return;
		}
		if (context.NativeState is { IsActive: true } nativeState)
		{
			nativeState.RemoveByNativeId(signal.Id);
		}
	}

	private void OnActionInvoked(ConnectionContext context, Exception? exception, (uint Id, string ActionKey) signal)
	{
		if (exception is not null)
		{
			InvalidateContext(context, deferDisposal: true);
			LogWarning($"Linux notification action signal failed: {exception.Message}");
			return;
		}
		if (context.NativeState is not { IsActive: true } nativeState ||
			nativeState.GetCommand(signal.Id) is not { } command)
		{
			return;
		}

		if (signal.ActionKey == command.BodyActionKey)
		{
			Activate(command.LaunchArgument, command.ProtocolUri);
			return;
		}
		if (command.Actions.FirstOrDefault(action => action.Key == signal.ActionKey) is { } action)
		{
			Activate(action.Argument, action.ProtocolUri);
		}
	}

	private void OnActivationToken(ConnectionContext context, Exception? exception, (uint Id, string ActivationToken) signal)
	{
		if (exception is not null)
		{
			InvalidateContext(context, deferDisposal: true);
			LogWarning($"Linux notification activation-token signal failed: {exception.Message}");
			return;
		}
		if (context.NativeState is { IsActive: true } nativeState &&
			nativeState.GetNotificationId(signal.Id) is not null &&
			signal.ActivationToken.Length > 0)
		{
			Environment.SetEnvironmentVariable("XDG_ACTIVATION_TOKEN", signal.ActivationToken);
		}
	}

	private static LinuxAppNotificationNativeStateSession GetNativeState(ConnectionContext context)
		=> context.NativeState is { IsActive: true } nativeState
			? nativeState
			: throw new InvalidOperationException("The Linux notification signal subscription is unavailable.");

	private static void Activate(string argument, string? protocolUri)
	{
		if (protocolUri is not null && Uri.TryCreate(protocolUri, UriKind.Absolute, out var uri))
		{
			_ = Windows.System.Launcher.LaunchUriPlatformAsync(uri);
		}
		else
		{
			AppNotificationActivationBroker.Publish(new AppNotificationActivation(argument, new Dictionary<string, string>()));
		}
	}

	private static string[] BuildActions(LinuxAppNotificationCommand command, IReadOnlySet<string> capabilities)
	{
		if (!capabilities.Contains("actions"))
		{
			return Array.Empty<string>();
		}
		var actions = new List<string>
		{
			command.BodyActionKey,
			"Open",
		};
		foreach (var action in command.Actions)
		{
			actions.Add(action.Key);
			actions.Add(action.Title);
		}
		return actions.ToArray();
	}

	private static Dictionary<string, VariantValue> BuildHints(LinuxAppNotificationCommand command, IReadOnlySet<string> capabilities)
	{
		var hints = new Dictionary<string, VariantValue>
		{
			["urgency"] = command.Urgency,
		};
		if (command.Category.Length > 0)
		{
			hints["category"] = command.Category;
		}
		if (command.ProgressPercentage is { } progress)
		{
			hints["value"] = (uint)progress;
		}
		if (command.MuteAudio && capabilities.Contains("sound"))
		{
			hints["suppress-sound"] = true;
		}
		return hints;
	}

	private static string FormatBody(string body, IReadOnlySet<string> capabilities)
		=> capabilities.Contains("body-markup")
			? body.Replace("&", "&amp;", StringComparison.Ordinal)
				.Replace("<", "&lt;", StringComparison.Ordinal)
				.Replace(">", "&gt;", StringComparison.Ordinal)
			: body;

	private static string GetApplicationName()
		=> string.IsNullOrEmpty(Package.Current.DisplayName) ? AppDomain.CurrentDomain.FriendlyName : Package.Current.DisplayName;

	private void ResetInitialization(Task<ConnectionContext?> initialization)
	{
		lock (_initializationGate)
		{
			if (ReferenceEquals(_initialization, initialization))
			{
				_initialization = null;
			}
		}
	}

	private void AbandonInitialization(Task<ConnectionContext?> initialization)
	{
		var abandoned = false;
		lock (_initializationGate)
		{
			if (ReferenceEquals(_initialization, initialization) && !initialization.IsCompleted)
			{
				_initialization = null;
				abandoned = true;
			}
		}
		if (abandoned)
		{
			_ = DisposeWhenCompletedAsync(initialization);
		}
	}

	private static async Task DisposeWhenCompletedAsync(Task<ConnectionContext?> initialization)
	{
		try
		{
			(await initialization.ConfigureAwait(false))?.Dispose();
		}
		catch
		{
		}
	}

	private void InvalidateContext(ConnectionContext context, bool deferDisposal = false)
	{
		lock (_initializationGate)
		{
			if (_initialization is { IsCompletedSuccessfully: true } initialization &&
				ReferenceEquals(initialization.Result, context))
			{
				_initialization = null;
			}
			context.Deactivate();
		}
		if (deferDisposal)
		{
			_ = Task.Run(context.Dispose);
		}
		else
		{
			context.Dispose();
		}
	}

	private static T Wait<T>(Task<T> task)
		=> task.Wait(OperationTimeout)
			? task.GetAwaiter().GetResult()
			: throw new TimeoutException("Timed out while communicating with the Linux notification service.");

	private static void Wait(Task task)
	{
		if (!task.Wait(OperationTimeout))
		{
			throw new TimeoutException("Timed out while communicating with the Linux notification service.");
		}
		task.GetAwaiter().GetResult();
	}

	private static void LogWarning(string message)
	{
		if (typeof(LinuxAppNotificationManagerBackend).Log().IsEnabled(LogLevel.Warning))
		{
			typeof(LinuxAppNotificationManagerBackend).Log().LogWarning(message);
		}
	}

	private sealed class ConnectionContext : IDisposable
	{
		private LinuxAppNotificationNativeStateSession? _nativeState;
		private int _isDisposed;

		public ConnectionContext(DBusConnection connection, DBusProxy bus, Notifications notifications, IReadOnlySet<string> capabilities, string serverOwner)
		{
			Connection = connection;
			Bus = bus;
			Notifications = notifications;
			Capabilities = capabilities;
			ServerOwner = serverOwner;
		}

		public DBusConnection Connection { get; }

		public DBusProxy Bus { get; }

		public Notifications Notifications { get; }

		public IReadOnlySet<string> Capabilities { get; }

		public string ServerOwner { get; }

		public LinuxAppNotificationNativeStateSession? NativeState => System.Threading.Volatile.Read(ref _nativeState);

		public bool IsDisposed => System.Threading.Volatile.Read(ref _isDisposed) != 0;

		public List<IDisposable> Subscriptions { get; } = new();

		public void Activate(LinuxAppNotificationNativeStateStore store)
		{
			if (IsDisposed)
			{
				throw new ObjectDisposedException(nameof(ConnectionContext));
			}
			System.Threading.Volatile.Write(ref _nativeState, store.StartSession(ServerOwner));
		}

		public bool Deactivate()
			=> System.Threading.Interlocked.Exchange(ref _nativeState, null)?.End() == true;

		public void Dispose()
		{
			if (System.Threading.Interlocked.Exchange(ref _isDisposed, 1) != 0)
			{
				return;
			}
			Deactivate();
			foreach (var subscription in Subscriptions)
			{
				subscription.Dispose();
			}
			Connection.Dispose();
		}
	}
}