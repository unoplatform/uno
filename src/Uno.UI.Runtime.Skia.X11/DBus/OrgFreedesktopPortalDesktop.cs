// Generated using `dotnet dbus codegen --bus session --service org.freedesktop.portal.Desktop`
// tmds.dbus.tool      0.16.0
#nullable disable
#pragma warning disable CA1805

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tmds.DBus;

[assembly: InternalsVisibleTo(Connection.DynamicAssemblyName)]
namespace Uno.WinUI.Runtime.Skia.X11.Dbus
{
	[DBusInterface("org.freedesktop.portal.Inhibit")]
	internal interface IInhibit : IDBusObject
	{
		Task<ObjectPath> InhibitAsync(string Window, uint Flags, IDictionary<string, object> Options);
		Task<ObjectPath> CreateMonitorAsync(string Window, IDictionary<string, object> Options);
		Task QueryEndResponseAsync(ObjectPath SessionHandle);
		Task<IDisposable> WatchStateChangedAsync(Action<(ObjectPath sessionHandle, IDictionary<string, object> state)> handler, Action<Exception> onError = null);
		Task<T> GetAsync<T>(string prop);
		Task<InhibitProperties> GetAllAsync();
		Task SetAsync(string prop, object val);
		Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
	}

	[Dictionary]
	internal class InhibitProperties
	{
		private uint _version = default(uint);
		public uint Version
		{
			get
			{
				return _version;
			}

			set
			{
				_version = (value);
			}
		}
	}

	internal static class InhibitExtensions
	{
		public static Task<uint> GetVersionAsync(this IInhibit o) => o.GetAsync<uint>("version");
	}

	[DBusInterface("org.freedesktop.portal.Location")]
	internal interface ILocation : IDBusObject
	{
		Task<ObjectPath> CreateSessionAsync(IDictionary<string, object> Options);
		Task<ObjectPath> StartAsync(ObjectPath SessionHandle, string ParentWindow, IDictionary<string, object> Options);
		Task<IDisposable> WatchLocationUpdatedAsync(Action<(ObjectPath sessionHandle, IDictionary<string, object> location)> handler, Action<Exception> onError = null);
		Task<T> GetAsync<T>(string prop);
		Task<LocationProperties> GetAllAsync();
		Task SetAsync(string prop, object val);
		Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
	}

	[Dictionary]
	internal class LocationProperties
	{
		private uint _version = default(uint);
		public uint Version
		{
			get
			{
				return _version;
			}

			set
			{
				_version = (value);
			}
		}
	}

	internal static class LocationExtensions
	{
		public static Task<uint> GetVersionAsync(this ILocation o) => o.GetAsync<uint>("version");
	}

	[DBusInterface("org.freedesktop.portal.Account")]
	internal interface IAccount : IDBusObject
	{
		Task<ObjectPath> GetUserInformationAsync(string Window, IDictionary<string, object> Options);
		Task<T> GetAsync<T>(string prop);
		Task<AccountProperties> GetAllAsync();
		Task SetAsync(string prop, object val);
		Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
	}

	[Dictionary]
	internal class AccountProperties
	{
		private uint _version = default(uint);
		public uint Version
		{
			get
			{
				return _version;
			}

			set
			{
				_version = (value);
			}
		}
	}

	internal static class AccountExtensions
	{
		public static Task<uint> GetVersionAsync(this IAccount o) => o.GetAsync<uint>("version");
	}

	[DBusInterface("org.freedesktop.portal.Notification")]
	interface INotification : IDBusObject
	{
		Task AddNotificationAsync(string Id, IDictionary<string, object> Notification);
		Task RemoveNotificationAsync(string Id);
		Task<IDisposable> WatchActionInvokedAsync(Action<(string id, string action, object[] parameter)> handler, Action<Exception> onError = null);
		Task<T> GetAsync<T>(string prop);
		Task<NotificationProperties> GetAllAsync();
		Task SetAsync(string prop, object val);
		Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
	}

	[Dictionary]
	internal class NotificationProperties
	{
		private uint _version = default(uint);
		public uint Version
		{
			get
			{
				return _version;
			}

			set
			{
				_version = (value);
			}
		}
	}

	internal static class NotificationExtensions
	{
		public static Task<uint> GetVersionAsync(this INotification o) => o.GetAsync<uint>("version");
	}

	[DBusInterface("org.freedesktop.portal.NetworkMonitor")]
	internal interface INetworkMonitor : IDBusObject
	{
		Task<bool> GetAvailableAsync();
		Task<bool> GetMeteredAsync();
		Task<uint> GetConnectivityAsync();
		Task<IDictionary<string, object>> GetStatusAsync();
		Task<bool> CanReachAsync(string Hostname, uint Port);
		Task<IDisposable> WatchchangedAsync(Action handler, Action<Exception> onError = null);
		Task<T> GetAsync<T>(string prop);
		Task<NetworkMonitorProperties> GetAllAsync();
		Task SetAsync(string prop, object val);
		Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
	}

	[Dictionary]
	internal class NetworkMonitorProperties
	{
		private uint _version = default(uint);
		public uint Version
		{
			get
			{
				return _version;
			}

			set
			{
				_version = (value);
			}
		}
	}

	internal static class NetworkMonitorExtensions
	{
		public static Task<uint> GetVersionAsync(this INetworkMonitor o) => o.GetAsync<uint>("version");
	}

	[DBusInterface("org.freedesktop.portal.Print")]
	internal interface IPrint : IDBusObject
	{
		Task<ObjectPath> PrintAsync(string ParentWindow, string Title, CloseSafeHandle Fd, IDictionary<string, object> Options);
		Task<ObjectPath> PreparePrintAsync(string ParentWindow, string Title, IDictionary<string, object> Settings, IDictionary<string, object> PageSetup, IDictionary<string, object> Options);
		Task<T> GetAsync<T>(string prop);
		Task<PrintProperties> GetAllAsync();
		Task SetAsync(string prop, object val);
		Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
	}

	[Dictionary]
	internal class PrintProperties
	{
		private uint _version = default(uint);
		public uint Version
		{
			get
			{
				return _version;
			}

			set
			{
				_version = (value);
			}
		}
	}

	internal static class PrintExtensions
	{
		public static Task<uint> GetVersionAsync(this IPrint o) => o.GetAsync<uint>("version");
	}

	[DBusInterface("org.freedesktop.portal.GameMode")]
	internal interface IGameMode : IDBusObject
	{
		Task<int> QueryStatusAsync(int Pid);
		Task<int> RegisterGameAsync(int Pid);
		Task<int> UnregisterGameAsync(int Pid);
		Task<int> QueryStatusByPidAsync(int Target, int Requester);
		Task<int> RegisterGameByPidAsync(int Target, int Requester);
		Task<int> UnregisterGameByPidAsync(int Target, int Requester);
		Task<int> QueryStatusByPIDFdAsync(CloseSafeHandle Target, CloseSafeHandle Requester);
		Task<int> RegisterGameByPIDFdAsync(CloseSafeHandle Target, CloseSafeHandle Requester);
		Task<int> UnregisterGameByPIDFdAsync(CloseSafeHandle Target, CloseSafeHandle Requester);
		Task<T> GetAsync<T>(string prop);
		Task<GameModeProperties> GetAllAsync();
		Task SetAsync(string prop, object val);
		Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
	}

	[Dictionary]
	internal class GameModeProperties
	{
		private bool _Active = default(bool);
		public bool Active
		{
			get
			{
				return _Active;
			}

			set
			{
				_Active = (value);
			}
		}

		private uint _version = default(uint);
		public uint Version
		{
			get
			{
				return _version;
			}

			set
			{
				_version = (value);
			}
		}
	}

	internal static class GameModeExtensions
	{
		public static Task<bool> GetActiveAsync(this IGameMode o) => o.GetAsync<bool>("Active");
		public static Task<uint> GetVersionAsync(this IGameMode o) => o.GetAsync<uint>("version");
	}

	[DBusInterface("org.freedesktop.portal.MemoryMonitor")]
	internal interface IMemoryMonitor : IDBusObject
	{
		Task<IDisposable> WatchLowMemoryWarningAsync(Action<byte> handler, Action<Exception> onError = null);
		Task<T> GetAsync<T>(string prop);
		Task<MemoryMonitorProperties> GetAllAsync();
		Task SetAsync(string prop, object val);
		Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
	}

	[Dictionary]
	internal class MemoryMonitorProperties
	{
		private uint _version = default(uint);
		public uint Version
		{
			get
			{
				return _version;
			}

			set
			{
				_version = (value);
			}
		}
	}

	internal static class MemoryMonitorExtensions
	{
		public static Task<uint> GetVersionAsync(this IMemoryMonitor o) => o.GetAsync<uint>("version");
	}

	[DBusInterface("org.freedesktop.portal.OpenURI")]
	internal interface IOpenURI : IDBusObject
	{
		Task<ObjectPath> OpenURIAsync(string ParentWindow, string Uri, IDictionary<string, object> Options);
		Task<ObjectPath> OpenFileAsync(string ParentWindow, CloseSafeHandle Fd, IDictionary<string, object> Options);
		Task<ObjectPath> OpenDirectoryAsync(string ParentWindow, CloseSafeHandle Fd, IDictionary<string, object> Options);
		Task<T> GetAsync<T>(string prop);
		Task<OpenURIProperties> GetAllAsync();
		Task SetAsync(string prop, object val);
		Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
	}

	[Dictionary]
	internal class OpenURIProperties
	{
		private uint _version = default(uint);
		public uint Version
		{
			get
			{
				return _version;
			}

			set
			{
				_version = (value);
			}
		}
	}

	internal static class OpenURIExtensions
	{
		public static Task<uint> GetVersionAsync(this IOpenURI o) => o.GetAsync<uint>("version");
	}

	[DBusInterface("org.freedesktop.portal.Realtime")]
	internal interface IRealtime : IDBusObject
	{
		Task MakeThreadRealtimeWithPIDAsync(ulong Process, ulong Thread, uint Priority);
		Task MakeThreadHighPriorityWithPIDAsync(ulong Process, ulong Thread, int Priority);
		Task<T> GetAsync<T>(string prop);
		Task<RealtimeProperties> GetAllAsync();
		Task SetAsync(string prop, object val);
		Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
	}

	[Dictionary]
	internal class RealtimeProperties
	{
		private int _MaxRealtimePriority = default(int);
		public int MaxRealtimePriority
		{
			get
			{
				return _MaxRealtimePriority;
			}

			set
			{
				_MaxRealtimePriority = (value);
			}
		}

		private int _MinNiceLevel = default(int);
		public int MinNiceLevel
		{
			get
			{
				return _MinNiceLevel;
			}

			set
			{
				_MinNiceLevel = (value);
			}
		}

		private long _RTTimeUSecMax = default(long);
		public long RTTimeUSecMax
		{
			get
			{
				return _RTTimeUSecMax;
			}

			set
			{
				_RTTimeUSecMax = (value);
			}
		}

		private uint _version = default(uint);
		public uint Version
		{
			get
			{
				return _version;
			}

			set
			{
				_version = (value);
			}
		}
	}

	internal static class RealtimeExtensions
	{
		public static Task<int> GetMaxRealtimePriorityAsync(this IRealtime o) => o.GetAsync<int>("MaxRealtimePriority");
		public static Task<int> GetMinNiceLevelAsync(this IRealtime o) => o.GetAsync<int>("MinNiceLevel");
		public static Task<long> GetRTTimeUSecMaxAsync(this IRealtime o) => o.GetAsync<long>("RTTimeUSecMax");
		public static Task<uint> GetVersionAsync(this IRealtime o) => o.GetAsync<uint>("version");
	}

	[DBusInterface("org.freedesktop.portal.DynamicLauncher")]
	internal interface IDynamicLauncher : IDBusObject
	{
		Task InstallAsync(string Token, string DesktopFileId, string DesktopEntry, IDictionary<string, object> Options);
		Task<ObjectPath> PrepareInstallAsync(string ParentWindow, string Name, object IconV, IDictionary<string, object> Options);
		Task<string> RequestInstallTokenAsync(string Name, object IconV, IDictionary<string, object> Options);
		Task UninstallAsync(string DesktopFileId, IDictionary<string, object> Options);
		Task<string> GetDesktopEntryAsync(string DesktopFileId);
		Task<(object iconV, string iconFormat, uint iconSize)> GetIconAsync(string DesktopFileId);
		Task LaunchAsync(string DesktopFileId, IDictionary<string, object> Options);
		Task<T> GetAsync<T>(string prop);
		Task<DynamicLauncherProperties> GetAllAsync();
		Task SetAsync(string prop, object val);
		Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
	}

	[Dictionary]
	internal class DynamicLauncherProperties
	{
		private uint _SupportedLauncherTypes = default(uint);
		public uint SupportedLauncherTypes
		{
			get
			{
				return _SupportedLauncherTypes;
			}

			set
			{
				_SupportedLauncherTypes = (value);
			}
		}

		private uint _version = default(uint);
		public uint Version
		{
			get
			{
				return _version;
			}

			set
			{
				_version = (value);
			}
		}
	}

	internal static class DynamicLauncherExtensions
	{
		public static Task<uint> GetSupportedLauncherTypesAsync(this IDynamicLauncher o) => o.GetAsync<uint>("SupportedLauncherTypes");
		public static Task<uint> GetVersionAsync(this IDynamicLauncher o) => o.GetAsync<uint>("version");
	}

	[DBusInterface("org.freedesktop.portal.Camera")]
	internal interface ICamera : IDBusObject
	{
		Task<ObjectPath> AccessCameraAsync(IDictionary<string, object> Options);
		Task<CloseSafeHandle> OpenPipeWireRemoteAsync(IDictionary<string, object> Options);
		Task<T> GetAsync<T>(string prop);
		Task<CameraProperties> GetAllAsync();
		Task SetAsync(string prop, object val);
		Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
	}

	[Dictionary]
	internal class CameraProperties
	{
		private bool _IsCameraPresent = default(bool);
		public bool IsCameraPresent
		{
			get
			{
				return _IsCameraPresent;
			}

			set
			{
				_IsCameraPresent = (value);
			}
		}

		private uint _version = default(uint);
		public uint Version
		{
			get
			{
				return _version;
			}

			set
			{
				_version = (value);
			}
		}
	}

	internal static class CameraExtensions
	{
		public static Task<bool> GetIsCameraPresentAsync(this ICamera o) => o.GetAsync<bool>("IsCameraPresent");
		public static Task<uint> GetVersionAsync(this ICamera o) => o.GetAsync<uint>("version");
	}

	[DBusInterface("org.freedesktop.portal.Device")]
	internal interface IDevice : IDBusObject
	{
		Task<ObjectPath> AccessDeviceAsync(uint Pid, string[] Devices, IDictionary<string, object> Options);
		Task<T> GetAsync<T>(string prop);
		Task<DeviceProperties> GetAllAsync();
		Task SetAsync(string prop, object val);
		Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
	}

	[Dictionary]
	internal class DeviceProperties
	{
		private uint _version = default(uint);
		public uint Version
		{
			get
			{
				return _version;
			}

			set
			{
				_version = (value);
			}
		}
	}

	internal static class DeviceExtensions
	{
		public static Task<uint> GetVersionAsync(this IDevice o) => o.GetAsync<uint>("version");
	}

	[DBusInterface("org.freedesktop.portal.PowerProfileMonitor")]
	internal interface IPowerProfileMonitor : IDBusObject
	{
		Task<T> GetAsync<T>(string prop);
		Task<PowerProfileMonitorProperties> GetAllAsync();
		Task SetAsync(string prop, object val);
		Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
	}

	[Dictionary]
	internal class PowerProfileMonitorProperties
	{
		private bool _power_saver_enabled = default(bool);
		public bool PowerSaverEnabled
		{
			get
			{
				return _power_saver_enabled;
			}

			set
			{
				_power_saver_enabled = (value);
			}
		}

		private uint _version = default(uint);
		public uint Version
		{
			get
			{
				return _version;
			}

			set
			{
				_version = (value);
			}
		}
	}

	internal static class PowerProfileMonitorExtensions
	{
		public static Task<bool> GetPowerSaverEnabledAsync(this IPowerProfileMonitor o) => o.GetAsync<bool>("power-saver-enabled");
		public static Task<uint> GetVersionAsync(this IPowerProfileMonitor o) => o.GetAsync<uint>("version");
	}

	[DBusInterface("org.freedesktop.portal.Email")]
	internal interface IEmail : IDBusObject
	{
		Task<ObjectPath> ComposeEmailAsync(string ParentWindow, IDictionary<string, object> Options);
		Task<T> GetAsync<T>(string prop);
		Task<EmailProperties> GetAllAsync();
		Task SetAsync(string prop, object val);
		Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
	}

	[Dictionary]
	internal class EmailProperties
	{
		private uint _version = default(uint);
		public uint Version
		{
			get
			{
				return _version;
			}

			set
			{
				_version = (value);
			}
		}
	}

	internal static class EmailExtensions
	{
		public static Task<uint> GetVersionAsync(this IEmail o) => o.GetAsync<uint>("version");
	}

	[DBusInterface("org.freedesktop.portal.Trash")]
	internal interface ITrash : IDBusObject
	{
		Task<uint> TrashFileAsync(CloseSafeHandle Fd);
		Task<T> GetAsync<T>(string prop);
		Task<TrashProperties> GetAllAsync();
		Task SetAsync(string prop, object val);
		Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
	}

	[Dictionary]
	internal class TrashProperties
	{
		private uint _version = default(uint);
		public uint Version
		{
			get
			{
				return _version;
			}

			set
			{
				_version = (value);
			}
		}
	}

	internal static class TrashExtensions
	{
		public static Task<uint> GetVersionAsync(this ITrash o) => o.GetAsync<uint>("version");
	}

	[DBusInterface("org.freedesktop.portal.ProxyResolver")]
	internal interface IProxyResolver : IDBusObject
	{
		Task<string[]> LookupAsync(string Uri);
		Task<T> GetAsync<T>(string prop);
		Task<ProxyResolverProperties> GetAllAsync();
		Task SetAsync(string prop, object val);
		Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
	}

	[Dictionary]
	internal class ProxyResolverProperties
	{
		private uint _version = default(uint);
		public uint Version
		{
			get
			{
				return _version;
			}

			set
			{
				_version = (value);
			}
		}
	}

	internal static class ProxyResolverExtensions
	{
		public static Task<uint> GetVersionAsync(this IProxyResolver o) => o.GetAsync<uint>("version");
	}

	[DBusInterface("org.freedesktop.portal.FileChooser")]
	internal interface IFileChooser : IDBusObject
	{
		Task<ObjectPath> OpenFileAsync(string ParentWindow, string Title, IDictionary<string, object> Options);
		Task<ObjectPath> SaveFileAsync(string ParentWindow, string Title, IDictionary<string, object> Options);
		Task<ObjectPath> SaveFilesAsync(string ParentWindow, string Title, IDictionary<string, object> Options);
		Task<T> GetAsync<T>(string prop);
		Task<FileChooserProperties> GetAllAsync();
		Task SetAsync(string prop, object val);
		Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
	}

	[Dictionary]
	internal class FileChooserProperties
	{
		private uint _version = default(uint);
		public uint Version
		{
			get
			{
				return _version;
			}

			set
			{
				_version = (value);
			}
		}
	}

	internal static class FileChooserExtensions
	{
		public static Task<uint> GetVersionAsync(this IFileChooser o) => o.GetAsync<uint>("version");
	}
}
