#nullable enable
#pragma warning disable CS8305

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.UI.Shell.Tasks;
using Uno.WinUI.Runtime.Skia.X11.DBus;
using Windows.UI.Shell.Tasks;

namespace Uno.WinUI.Runtime.Skia.X11;

internal sealed class X11AppTaskInfoExtension : AppTaskInfoExtensionBase
{
	private const string Service = "org.freedesktop.Notifications";
	private static readonly ObjectPath ObjectPath = new("/org/freedesktop/Notifications");
	private static readonly X11AppTaskInfoExtension Instance = new();

	private readonly Dictionary<string, string> _signatures = new(StringComparer.Ordinal);
	private readonly Dictionary<string, uint> _notificationIds = new(StringComparer.Ordinal);
	private readonly object _supportProbeGate = new();
	private DBusConnection? _publicationConnection;
	private Task? _supportProbe;
	private int _supportState;
	private long _nextSupportProbeAt;

	private X11AppTaskInfoExtension()
	{
		EnsureSupportProbe();
	}

	internal static void Register() =>
		ApiExtensibility.Register(typeof(IAppTaskInfoExtension), _ => Instance);

	public override bool IsSupported()
	{
		var isSupported = Volatile.Read(ref _supportState) == 1;
		EnsureSupportProbe();
		return isSupported;
	}

	protected override async Task OnSynchronizeAsync(AppTaskInfoSnapshot[] tasks)
	{
		try
		{
			await PublishAsync(tasks);
			Volatile.Write(ref _supportState, 1);
		}
		catch
		{
			ResetPublicationConnection();
			Volatile.Write(ref _supportState, 0);
			Interlocked.Exchange(ref _nextSupportProbeAt, Environment.TickCount64 + 5000);
			throw;
		}
	}

	private async Task PublishAsync(AppTaskInfoSnapshot[] tasks)
	{
		var sessionAddress = DBusAddress.Session;
		if (sessionAddress is null)
		{
			throw new InvalidOperationException("The D-Bus session address is unavailable.");
		}

		var connection = await GetPublicationConnectionAsync(sessionAddress);
		var service = new DBusService(connection, Service);
		var notifications = service.CreateNotifications(ObjectPath);
		var currentIds = tasks.Select(static task => task.Id).ToHashSet(StringComparer.Ordinal);
		var removedIds = _signatures.Keys.Where(id => !currentIds.Contains(id)).ToArray();

		foreach (var removedId in removedIds)
		{
			if (_notificationIds.TryGetValue(removedId, out var notificationId))
			{
				await notifications.CloseNotificationAsync(notificationId)
					.WaitAsync(TimeSpan.FromSeconds(5))
					.ConfigureAwait(false);
				_notificationIds.Remove(removedId);
			}

			_signatures.Remove(removedId);
		}

		foreach (var task in tasks)
		{
			var payload = CreatePayload(task);
			if (_signatures.TryGetValue(task.Id, out var previous) && previous == payload.Signature)
			{
				continue;
			}

			_notificationIds.TryGetValue(task.Id, out var replacesId);
			var notificationId = await notifications.NotifyAsync(
				"Uno Platform",
				replacesId,
				payload.Icon,
				payload.Summary,
				payload.Body,
				Array.Empty<string>(),
				new Dictionary<string, VariantValue>(),
				expireTimeout: -1)
				.WaitAsync(TimeSpan.FromSeconds(5))
				.ConfigureAwait(false);
			_notificationIds[task.Id] = notificationId;
			_signatures[task.Id] = payload.Signature;
		}
	}

	private async Task<DBusConnection> GetPublicationConnectionAsync(string sessionAddress)
	{
		if (_publicationConnection is not null)
		{
			return _publicationConnection;
		}

		var connection = new DBusConnection(sessionAddress);
		try
		{
			await connection.ConnectAsync()
				.AsTask()
				.WaitAsync(TimeSpan.FromSeconds(5))
				.ConfigureAwait(false);
			return _publicationConnection = connection;
		}
		catch
		{
			connection.Dispose();
			throw;
		}
	}

	private void ResetPublicationConnection()
	{
		_publicationConnection?.Dispose();
		_publicationConnection = null;
	}

	private static async Task<bool> ServiceHasOwnerAsync(string sessionAddress)
	{
		try
		{
			using var connection = new DBusConnection(sessionAddress);
			await connection.ConnectAsync()
				.AsTask()
				.WaitAsync(TimeSpan.FromSeconds(5))
				.ConfigureAwait(false);
			var service = new DBusService(connection, "org.freedesktop.DBus");
			var dbus = service.CreateDBus("/org/freedesktop/DBus");
			return await dbus.NameHasOwnerAsync(Service)
				.WaitAsync(TimeSpan.FromSeconds(5))
				.ConfigureAwait(false);
		}
		catch (Exception error)
		{
			if (typeof(X11AppTaskInfoExtension).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(X11AppTaskInfoExtension).Log().Debug(
					$"Unable to probe the '{Service}' D-Bus service: {error.Message}");
			}

			return false;
		}
	}

	private void EnsureSupportProbe()
	{
		if (Environment.TickCount64 < Interlocked.Read(ref _nextSupportProbeAt))
		{
			return;
		}

		lock (_supportProbeGate)
		{
			if (_supportProbe is { IsCompleted: false })
			{
				return;
			}

			Interlocked.Exchange(ref _nextSupportProbeAt, Environment.TickCount64 + 5000);
			_supportProbe = ProbeSupportAsync();
		}
	}

	private async Task ProbeSupportAsync()
	{
		var sessionAddress = DBusAddress.Session;
		var isSupported = sessionAddress is not null && await ServiceHasOwnerAsync(sessionAddress);
		var supportState = isSupported ? 1 : 2;
		var previousState = Interlocked.Exchange(ref _supportState, supportState);
		if (supportState == 1 && previousState != 1)
		{
			InvalidateSynchronization();
		}
	}

	private static NotificationPayload CreatePayload(AppTaskInfoSnapshot task) =>
		new(GetIcon(task.IconUri), EscapeMarkup(task.Title), EscapeMarkup(GetBody(task)));

	private static string GetBody(AppTaskInfoSnapshot task)
	{
		var content = GetContentText(task);
		return string.IsNullOrEmpty(task.Content.Question)
			? $"{task.State}: {content}"
			: $"{task.Content.Question}\n{task.State}: {content}";
	}

	private static string GetContentText(AppTaskInfoSnapshot task)
	{
		if (!string.IsNullOrEmpty(task.Content.ExecutingStep))
		{
			return task.Content.ExecutingStep;
		}

		if (!string.IsNullOrEmpty(task.Content.TextSummary))
		{
			return task.Content.TextSummary;
		}

		return task.Content.GeneratedAssets.Length > 0
			? string.Join(", ", task.Content.GeneratedAssets.Select(static asset => asset.Name))
			: task.Subtitle;
	}

	private static string GetIcon(Uri iconUri) =>
		iconUri.IsFile ? iconUri.LocalPath : string.Empty;

	private static string EscapeMarkup(string value) =>
		value
			.Replace("&", "&amp;", StringComparison.Ordinal)
			.Replace("<", "&lt;", StringComparison.Ordinal)
			.Replace(">", "&gt;", StringComparison.Ordinal);

	// Deriving the change signature from the published payload keeps the two in lockstep: content that
	// the notification does not render can never be missed, and can never force a spurious re-publish.
	private readonly record struct NotificationPayload(string Icon, string Summary, string Body)
	{
		internal string Signature => string.Join('\n', Icon, Summary, Body);
	}
}
