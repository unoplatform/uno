// Generated using `dotnet dbus codegen /usr/share/dbus-1/interfaces/org.freedesktop.portal.Request.xml`
#nullable disable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tmds.DBus;

[assembly: InternalsVisibleTo(Connection.DynamicAssemblyName)]
namespace Uno.WinUI.Runtime.Skia.X11.Dbus
{
	[DBusInterface("org.freedesktop.portal.Request")]
	internal interface IRequest : IDBusObject
	{
		Task CloseAsync();
		Task<IDisposable> WatchResponseAsync(Action<(Response Response, IDictionary<string, object> results)> handler, Action<Exception> onError = null);
	}

	// From https://flatpak.github.io/xdg-desktop-portal/docs/doc-org.freedesktop.portal.Request.html#signals
	internal enum Response
	{
		Success = 0,
		UserCancelled = 1,
		Other = 2
	}
}
