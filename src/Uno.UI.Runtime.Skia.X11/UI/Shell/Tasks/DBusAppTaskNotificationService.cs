#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using Uno.WinUI.Runtime.Skia.X11.DBus;

namespace Uno.WinUI.Runtime.Skia.X11;

internal sealed class DBusAppTaskNotificationService : IAppTaskNotificationService
{
	private const string Service = "org.freedesktop.Notifications";
	private static readonly ObjectPath ObjectPath = new("/org/freedesktop/Notifications");
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
	private DBusConnection? _publicationConnection;

	public async Task<AppTaskNotificationSupport> ProbeAsync()
	{
		if (DBusAddress.Session is not { } sessionAddress)
		{
			return default;
		}

		using var connection = new DBusConnection(sessionAddress);
		await connection.ConnectAsync().AsTask().WaitAsync(Timeout).ConfigureAwait(false);
		var dbus = new DBusService(connection, "org.freedesktop.DBus").CreateDBus("/org/freedesktop/DBus");
		if (await dbus.NameHasOwnerAsync(Service).WaitAsync(Timeout).ConfigureAwait(false))
		{
			var owner = await dbus.GetNameOwnerAsync(Service).WaitAsync(Timeout).ConfigureAwait(false);
			return new(true, owner);
		}

		var activatable = await dbus.ListActivatableNamesAsync().WaitAsync(Timeout).ConfigureAwait(false);
		return new(activatable.Contains(Service, StringComparer.Ordinal), null);
	}

	public async Task<string> GetOwnerAsync()
	{
		var connection = await GetPublicationConnectionAsync().ConfigureAwait(false);
		var dbus = new DBusService(connection, "org.freedesktop.DBus").CreateDBus("/org/freedesktop/DBus");
		if (!await dbus.NameHasOwnerAsync(Service).WaitAsync(Timeout).ConfigureAwait(false))
		{
			await dbus.StartServiceByNameAsync(Service, 0).WaitAsync(Timeout).ConfigureAwait(false);
		}

		return await dbus.GetNameOwnerAsync(Service).WaitAsync(Timeout).ConfigureAwait(false);
	}

	public async Task<uint> NotifyAsync(string owner, uint replacesId, string icon, string summary, string body)
	{
		var connection = await GetPublicationConnectionAsync().ConfigureAwait(false);
		var notifications = new DBusService(connection, owner).CreateNotifications(ObjectPath);
		return await notifications.NotifyAsync(
			"Uno Platform",
			replacesId,
			icon,
			summary,
			body,
			Array.Empty<string>(),
			new Dictionary<string, VariantValue>(),
			expireTimeout: 0)
			.WaitAsync(Timeout)
			.ConfigureAwait(false);
	}

	public async Task CloseAsync(string owner, uint notificationId)
	{
		var connection = await GetPublicationConnectionAsync().ConfigureAwait(false);
		var notifications = new DBusService(connection, owner).CreateNotifications(ObjectPath);
		await notifications.CloseNotificationAsync(notificationId).WaitAsync(Timeout).ConfigureAwait(false);
	}

	private async Task<DBusConnection> GetPublicationConnectionAsync()
	{
		if (_publicationConnection is not null)
		{
			return _publicationConnection;
		}

		var sessionAddress = DBusAddress.Session ??
			throw new InvalidOperationException("The D-Bus session address is unavailable.");
		var connection = new DBusConnection(sessionAddress);
		try
		{
			await connection.ConnectAsync().AsTask().WaitAsync(Timeout).ConfigureAwait(false);
			return _publicationConnection = connection;
		}
		finally
		{
			if (!ReferenceEquals(_publicationConnection, connection))
			{
				connection.Dispose();
			}
		}
	}

	public void Reset()
	{
		_publicationConnection?.Dispose();
		_publicationConnection = null;
	}

	public void Dispose() => Reset();
}
