// Generated using `dotnet dbus codegen --protocol-api --bus session --service org.freedesktop.portal.Desktop`
// and `dotnet dbus codegen --protocol-api /usr/share/dbus-1/interfaces/org.freedesktop.portal.Request.xml`
// then copying the relevant parts of the latter into the former.
// Arch Linux's xdg-desktop-portal package v1.18.4-1
// tmds.dbus.tool 0.21.2

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace Uno.WinUI.Runtime.Skia.X11.DBus
{
	// From https://flatpak.github.io/xdg-desktop-portal/docs/doc-org.freedesktop.portal.Request.html#signals
	enum Response : uint
	{
		Success = 0,
		UserCancelled = 1,
		Other = 2
	}

	record InhibitProperties
	{
		public uint Version { get; set; } = default!;
	}
	partial class Inhibit : DesktopObject
	{
		private const string __Interface = "org.freedesktop.portal.Inhibit";
		public Inhibit(DesktopService service, ObjectPath path) : base(service, path)
		{ }
		public Task<ObjectPath> InhibitAsync(string window, uint flags, Dictionary<string, VariantValue> options)
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "sua{sv}",
					member: "Inhibit");
				writer.WriteString(window);
				writer.WriteUInt32(flags);
				writer.WriteDictionary(options);
				return writer.CreateMessage();
			}
		}
		public Task<ObjectPath> CreateMonitorAsync(string window, Dictionary<string, VariantValue> options)
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "sa{sv}",
					member: "CreateMonitor");
				writer.WriteString(window);
				writer.WriteDictionary(options);
				return writer.CreateMessage();
			}
		}
		public Task QueryEndResponseAsync(ObjectPath sessionHandle)
		{
			return this.Connection.CallMethodAsync(CreateMessage());
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "o",
					member: "QueryEndResponse");
				writer.WriteObjectPath(sessionHandle);
				return writer.CreateMessage();
			}
		}
		public ValueTask<IDisposable> WatchStateChangedAsync(Action<Exception?, (ObjectPath SessionHandle, Dictionary<string, VariantValue> State)> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
			=> base.WatchSignalAsync(Service.Destination, __Interface, Path, "StateChanged", (Message m, object? s) => ReadMessage_oaesv(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
		public Task<uint> GetVersionAsync()
			=> this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
		public Task<InhibitProperties> GetPropertiesAsync()
		{
			return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
			static InhibitProperties ReadMessage(Message message, DesktopObject _)
			{
				var reader = message.GetBodyReader();
				return ReadProperties(ref reader);
			}
		}
		public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<InhibitProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
		{
			return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
			static PropertyChanges<InhibitProperties> ReadMessage(Message message, DesktopObject _)
			{
				var reader = message.GetBodyReader();
				reader.ReadString(); // interface
				List<string> changed = new(), invalidated = new();
				return new PropertyChanges<InhibitProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
			}
			static string[] ReadInvalidated(ref Reader reader)
			{
				List<string>? invalidated = null;
				ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.String);
				while (reader.HasNext(arrayEnd))
				{
					invalidated ??= new();
					var property = reader.ReadString();
					switch (property)
					{
						case "version": invalidated.Add("Version"); break;
					}
				}
				return invalidated?.ToArray() ?? Array.Empty<string>();
			}
		}
		private static InhibitProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
		{
			var props = new InhibitProperties();
			ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
			while (reader.HasNext(arrayEnd))
			{
				var property = reader.ReadString();
				switch (property)
				{
					case "version":
						reader.ReadSignature("u"u8);
						props.Version = reader.ReadUInt32();
						changedList?.Add("Version");
						break;
					default:
						reader.ReadVariantValue();
						break;
				}
			}
			return props;
		}
	}
	record LocationProperties
	{
		public uint Version { get; set; } = default!;
	}
	partial class Location : DesktopObject
	{
		private const string __Interface = "org.freedesktop.portal.Location";
		public Location(DesktopService service, ObjectPath path) : base(service, path)
		{ }
		public Task<ObjectPath> CreateSessionAsync(Dictionary<string, VariantValue> options)
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "a{sv}",
					member: "CreateSession");
				writer.WriteDictionary(options);
				return writer.CreateMessage();
			}
		}
		public Task<ObjectPath> StartAsync(ObjectPath sessionHandle, string parentWindow, Dictionary<string, VariantValue> options)
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "osa{sv}",
					member: "Start");
				writer.WriteObjectPath(sessionHandle);
				writer.WriteString(parentWindow);
				writer.WriteDictionary(options);
				return writer.CreateMessage();
			}
		}
		public ValueTask<IDisposable> WatchLocationUpdatedAsync(Action<Exception?, (ObjectPath SessionHandle, Dictionary<string, VariantValue> Location)> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
			=> base.WatchSignalAsync(Service.Destination, __Interface, Path, "LocationUpdated", (Message m, object? s) => ReadMessage_oaesv(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
		public Task<uint> GetVersionAsync()
			=> this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
		public Task<LocationProperties> GetPropertiesAsync()
		{
			return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
			static LocationProperties ReadMessage(Message message, DesktopObject _)
			{
				var reader = message.GetBodyReader();
				return ReadProperties(ref reader);
			}
		}
		public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<LocationProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
		{
			return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
			static PropertyChanges<LocationProperties> ReadMessage(Message message, DesktopObject _)
			{
				var reader = message.GetBodyReader();
				reader.ReadString(); // interface
				List<string> changed = new(), invalidated = new();
				return new PropertyChanges<LocationProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
			}
			static string[] ReadInvalidated(ref Reader reader)
			{
				List<string>? invalidated = null;
				ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.String);
				while (reader.HasNext(arrayEnd))
				{
					invalidated ??= new();
					var property = reader.ReadString();
					switch (property)
					{
						case "version": invalidated.Add("Version"); break;
					}
				}
				return invalidated?.ToArray() ?? Array.Empty<string>();
			}
		}
		private static LocationProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
		{
			var props = new LocationProperties();
			ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
			while (reader.HasNext(arrayEnd))
			{
				var property = reader.ReadString();
				switch (property)
				{
					case "version":
						reader.ReadSignature("u"u8);
						props.Version = reader.ReadUInt32();
						changedList?.Add("Version");
						break;
					default:
						reader.ReadVariantValue();
						break;
				}
			}
			return props;
		}
	}
	record NotificationProperties
	{
		public uint Version { get; set; } = default!;
	}
	partial class Notification : DesktopObject
	{
		private const string __Interface = "org.freedesktop.portal.Notification";
		public Notification(DesktopService service, ObjectPath path) : base(service, path)
		{ }
		public Task AddNotificationAsync(string id, Dictionary<string, VariantValue> notification)
		{
			return this.Connection.CallMethodAsync(CreateMessage());
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "sa{sv}",
					member: "AddNotification");
				writer.WriteString(id);
				writer.WriteDictionary(notification);
				return writer.CreateMessage();
			}
		}
		public Task RemoveNotificationAsync(string id)
		{
			return this.Connection.CallMethodAsync(CreateMessage());
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "s",
					member: "RemoveNotification");
				writer.WriteString(id);
				return writer.CreateMessage();
			}
		}
		public ValueTask<IDisposable> WatchActionInvokedAsync(Action<Exception?, (string Id, string Action, VariantValue[] Parameter)> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
			=> base.WatchSignalAsync(Service.Destination, __Interface, Path, "ActionInvoked", (Message m, object? s) => ReadMessage_ssav(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
		public Task<uint> GetVersionAsync()
			=> this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
		public Task<NotificationProperties> GetPropertiesAsync()
		{
			return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
			static NotificationProperties ReadMessage(Message message, DesktopObject _)
			{
				var reader = message.GetBodyReader();
				return ReadProperties(ref reader);
			}
		}
		public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<NotificationProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
		{
			return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
			static PropertyChanges<NotificationProperties> ReadMessage(Message message, DesktopObject _)
			{
				var reader = message.GetBodyReader();
				reader.ReadString(); // interface
				List<string> changed = new(), invalidated = new();
				return new PropertyChanges<NotificationProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
			}
			static string[] ReadInvalidated(ref Reader reader)
			{
				List<string>? invalidated = null;
				ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.String);
				while (reader.HasNext(arrayEnd))
				{
					invalidated ??= new();
					var property = reader.ReadString();
					switch (property)
					{
						case "version": invalidated.Add("Version"); break;
					}
				}
				return invalidated?.ToArray() ?? Array.Empty<string>();
			}
		}
		private static NotificationProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
		{
			var props = new NotificationProperties();
			ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
			while (reader.HasNext(arrayEnd))
			{
				var property = reader.ReadString();
				switch (property)
				{
					case "version":
						reader.ReadSignature("u"u8);
						props.Version = reader.ReadUInt32();
						changedList?.Add("Version");
						break;
					default:
						reader.ReadVariantValue();
						break;
				}
			}
			return props;
		}
	}
	record AccountProperties
	{
		public uint Version { get; set; } = default!;
	}
	partial class Account : DesktopObject
	{
		private const string __Interface = "org.freedesktop.portal.Account";
		public Account(DesktopService service, ObjectPath path) : base(service, path)
		{ }
		public Task<ObjectPath> GetUserInformationAsync(string window, Dictionary<string, VariantValue> options)
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "sa{sv}",
					member: "GetUserInformation");
				writer.WriteString(window);
				writer.WriteDictionary(options);
				return writer.CreateMessage();
			}
		}
		public Task<uint> GetVersionAsync()
			=> this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
		public Task<AccountProperties> GetPropertiesAsync()
		{
			return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
			static AccountProperties ReadMessage(Message message, DesktopObject _)
			{
				var reader = message.GetBodyReader();
				return ReadProperties(ref reader);
			}
		}
		public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<AccountProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
		{
			return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
			static PropertyChanges<AccountProperties> ReadMessage(Message message, DesktopObject _)
			{
				var reader = message.GetBodyReader();
				reader.ReadString(); // interface
				List<string> changed = new(), invalidated = new();
				return new PropertyChanges<AccountProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
			}
			static string[] ReadInvalidated(ref Reader reader)
			{
				List<string>? invalidated = null;
				ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.String);
				while (reader.HasNext(arrayEnd))
				{
					invalidated ??= new();
					var property = reader.ReadString();
					switch (property)
					{
						case "version": invalidated.Add("Version"); break;
					}
				}
				return invalidated?.ToArray() ?? Array.Empty<string>();
			}
		}
		private static AccountProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
		{
			var props = new AccountProperties();
			ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
			while (reader.HasNext(arrayEnd))
			{
				var property = reader.ReadString();
				switch (property)
				{
					case "version":
						reader.ReadSignature("u"u8);
						props.Version = reader.ReadUInt32();
						changedList?.Add("Version");
						break;
					default:
						reader.ReadVariantValue();
						break;
				}
			}
			return props;
		}
	}
	record NetworkMonitorProperties
	{
		public uint Version { get; set; } = default!;
	}
	partial class NetworkMonitor : DesktopObject
	{
		private const string __Interface = "org.freedesktop.portal.NetworkMonitor";
		public NetworkMonitor(DesktopService service, ObjectPath path) : base(service, path)
		{ }
		public Task<bool> GetAvailableAsync()
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_b(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					member: "GetAvailable");
				return writer.CreateMessage();
			}
		}
		public Task<bool> GetMeteredAsync()
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_b(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					member: "GetMetered");
				return writer.CreateMessage();
			}
		}
		public Task<uint> GetConnectivityAsync()
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_u(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					member: "GetConnectivity");
				return writer.CreateMessage();
			}
		}
		public Task<Dictionary<string, VariantValue>> GetStatusAsync()
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_aesv(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					member: "GetStatus");
				return writer.CreateMessage();
			}
		}
		public Task<bool> CanReachAsync(string hostname, uint port)
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_b(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "su",
					member: "CanReach");
				writer.WriteString(hostname);
				writer.WriteUInt32(port);
				return writer.CreateMessage();
			}
		}
		public ValueTask<IDisposable> WatchChangedAsync(Action<Exception?> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
			=> base.WatchSignalAsync(Service.Destination, __Interface, Path, "changed", handler, emitOnCapturedContext, flags);
		public Task<uint> GetVersionAsync()
			=> this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
		public Task<NetworkMonitorProperties> GetPropertiesAsync()
		{
			return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
			static NetworkMonitorProperties ReadMessage(Message message, DesktopObject _)
			{
				var reader = message.GetBodyReader();
				return ReadProperties(ref reader);
			}
		}
		public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<NetworkMonitorProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
		{
			return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
			static PropertyChanges<NetworkMonitorProperties> ReadMessage(Message message, DesktopObject _)
			{
				var reader = message.GetBodyReader();
				reader.ReadString(); // interface
				List<string> changed = new(), invalidated = new();
				return new PropertyChanges<NetworkMonitorProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
			}
			static string[] ReadInvalidated(ref Reader reader)
			{
				List<string>? invalidated = null;
				ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.String);
				while (reader.HasNext(arrayEnd))
				{
					invalidated ??= new();
					var property = reader.ReadString();
					switch (property)
					{
						case "version": invalidated.Add("Version"); break;
					}
				}
				return invalidated?.ToArray() ?? Array.Empty<string>();
			}
		}
		private static NetworkMonitorProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
		{
			var props = new NetworkMonitorProperties();
			ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
			while (reader.HasNext(arrayEnd))
			{
				var property = reader.ReadString();
				switch (property)
				{
					case "version":
						reader.ReadSignature("u"u8);
						props.Version = reader.ReadUInt32();
						changedList?.Add("Version");
						break;
					default:
						reader.ReadVariantValue();
						break;
				}
			}
			return props;
		}
	}
	record PrintProperties
	{
		public uint Version { get; set; } = default!;
	}
	partial class Print : DesktopObject
	{
		private const string __Interface = "org.freedesktop.portal.Print";
		public Print(DesktopService service, ObjectPath path) : base(service, path)
		{ }
		public Task<ObjectPath> PrintAsync(string parentWindow, string title, System.Runtime.InteropServices.SafeHandle fd, Dictionary<string, VariantValue> options)
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "ssha{sv}",
					member: "Print");
				writer.WriteString(parentWindow);
				writer.WriteString(title);
				writer.WriteHandle(fd);
				writer.WriteDictionary(options);
				return writer.CreateMessage();
			}
		}
		public Task<ObjectPath> PreparePrintAsync(string parentWindow, string title, Dictionary<string, VariantValue> settings, Dictionary<string, VariantValue> pageSetup, Dictionary<string, VariantValue> options)
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "ssa{sv}a{sv}a{sv}",
					member: "PreparePrint");
				writer.WriteString(parentWindow);
				writer.WriteString(title);
				writer.WriteDictionary(settings);
				writer.WriteDictionary(pageSetup);
				writer.WriteDictionary(options);
				return writer.CreateMessage();
			}
		}
		public Task<uint> GetVersionAsync()
			=> this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
		public Task<PrintProperties> GetPropertiesAsync()
		{
			return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
			static PrintProperties ReadMessage(Message message, DesktopObject _)
			{
				var reader = message.GetBodyReader();
				return ReadProperties(ref reader);
			}
		}
		public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<PrintProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
		{
			return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
			static PropertyChanges<PrintProperties> ReadMessage(Message message, DesktopObject _)
			{
				var reader = message.GetBodyReader();
				reader.ReadString(); // interface
				List<string> changed = new(), invalidated = new();
				return new PropertyChanges<PrintProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
			}
			static string[] ReadInvalidated(ref Reader reader)
			{
				List<string>? invalidated = null;
				ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.String);
				while (reader.HasNext(arrayEnd))
				{
					invalidated ??= new();
					var property = reader.ReadString();
					switch (property)
					{
						case "version": invalidated.Add("Version"); break;
					}
				}
				return invalidated?.ToArray() ?? Array.Empty<string>();
			}
		}
		private static PrintProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
		{
			var props = new PrintProperties();
			ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
			while (reader.HasNext(arrayEnd))
			{
				var property = reader.ReadString();
				switch (property)
				{
					case "version":
						reader.ReadSignature("u"u8);
						props.Version = reader.ReadUInt32();
						changedList?.Add("Version");
						break;
					default:
						reader.ReadVariantValue();
						break;
				}
			}
			return props;
		}
	}
	record SettingsProperties
	{
		public uint Version { get; set; } = default!;
	}
	partial class Settings : DesktopObject
	{
		private const string __Interface = "org.freedesktop.portal.Settings";
		public Settings(DesktopService service, ObjectPath path) : base(service, path)
		{ }
		public Task<Dictionary<string, Dictionary<string, VariantValue>>> ReadAllAsync(string[] namespaces)
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_aesaesv(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "as",
					member: "ReadAll");
				writer.WriteArray(namespaces);
				return writer.CreateMessage();
			}
		}
		public Task<VariantValue> ReadAsync(string @namespace, string key)
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_v(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "ss",
					member: "Read");
				writer.WriteString(@namespace);
				writer.WriteString(key);
				return writer.CreateMessage();
			}
		}
		public Task<VariantValue> ReadOneAsync(string @namespace, string key)
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_v(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "ss",
					member: "ReadOne");
				writer.WriteString(@namespace);
				writer.WriteString(key);
				return writer.CreateMessage();
			}
		}
		public ValueTask<IDisposable> WatchSettingChangedAsync(Action<Exception?, (string Namespace, string Key, VariantValue Value)> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
			=> base.WatchSignalAsync(Service.Destination, __Interface, Path, "SettingChanged", (Message m, object? s) => ReadMessage_ssv(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
		public Task<uint> GetVersionAsync()
			=> this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
		public Task<SettingsProperties> GetPropertiesAsync()
		{
			return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
			static SettingsProperties ReadMessage(Message message, DesktopObject _)
			{
				var reader = message.GetBodyReader();
				return ReadProperties(ref reader);
			}
		}
		public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<SettingsProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
		{
			return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
			static PropertyChanges<SettingsProperties> ReadMessage(Message message, DesktopObject _)
			{
				var reader = message.GetBodyReader();
				reader.ReadString(); // interface
				List<string> changed = new(), invalidated = new();
				return new PropertyChanges<SettingsProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
			}
			static string[] ReadInvalidated(ref Reader reader)
			{
				List<string>? invalidated = null;
				ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.String);
				while (reader.HasNext(arrayEnd))
				{
					invalidated ??= new();
					var property = reader.ReadString();
					switch (property)
					{
						case "version": invalidated.Add("Version"); break;
					}
				}
				return invalidated?.ToArray() ?? Array.Empty<string>();
			}
		}
		private static SettingsProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
		{
			var props = new SettingsProperties();
			ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
			while (reader.HasNext(arrayEnd))
			{
				var property = reader.ReadString();
				switch (property)
				{
					case "version":
						reader.ReadSignature("u"u8);
						props.Version = reader.ReadUInt32();
						changedList?.Add("Version");
						break;
					default:
						reader.ReadVariantValue();
						break;
				}
			}
			return props;
		}
	}
	record GameModeProperties
	{
		public bool Active { get; set; } = default!;
		public uint Version { get; set; } = default!;
	}
	partial class GameMode : DesktopObject
	{
		private const string __Interface = "org.freedesktop.portal.GameMode";
		public GameMode(DesktopService service, ObjectPath path) : base(service, path)
		{ }
		public Task<int> QueryStatusAsync(int pid)
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_i(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "i",
					member: "QueryStatus");
				writer.WriteInt32(pid);
				return writer.CreateMessage();
			}
		}
		public Task<int> RegisterGameAsync(int pid)
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_i(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "i",
					member: "RegisterGame");
				writer.WriteInt32(pid);
				return writer.CreateMessage();
			}
		}
		public Task<int> UnregisterGameAsync(int pid)
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_i(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "i",
					member: "UnregisterGame");
				writer.WriteInt32(pid);
				return writer.CreateMessage();
			}
		}
		public Task<int> QueryStatusByPidAsync(int target, int requester)
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_i(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "ii",
					member: "QueryStatusByPid");
				writer.WriteInt32(target);
				writer.WriteInt32(requester);
				return writer.CreateMessage();
			}
		}
		public Task<int> RegisterGameByPidAsync(int target, int requester)
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_i(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "ii",
					member: "RegisterGameByPid");
				writer.WriteInt32(target);
				writer.WriteInt32(requester);
				return writer.CreateMessage();
			}
		}
		public Task<int> UnregisterGameByPidAsync(int target, int requester)
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_i(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "ii",
					member: "UnregisterGameByPid");
				writer.WriteInt32(target);
				writer.WriteInt32(requester);
				return writer.CreateMessage();
			}
		}
		public Task<int> QueryStatusByPIDFdAsync(System.Runtime.InteropServices.SafeHandle target, System.Runtime.InteropServices.SafeHandle requester)
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_i(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "hh",
					member: "QueryStatusByPIDFd");
				writer.WriteHandle(target);
				writer.WriteHandle(requester);
				return writer.CreateMessage();
			}
		}
		public Task<int> RegisterGameByPIDFdAsync(System.Runtime.InteropServices.SafeHandle target, System.Runtime.InteropServices.SafeHandle requester)
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_i(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "hh",
					member: "RegisterGameByPIDFd");
				writer.WriteHandle(target);
				writer.WriteHandle(requester);
				return writer.CreateMessage();
			}
		}
		public Task<int> UnregisterGameByPIDFdAsync(System.Runtime.InteropServices.SafeHandle target, System.Runtime.InteropServices.SafeHandle requester)
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_i(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "hh",
					member: "UnregisterGameByPIDFd");
				writer.WriteHandle(target);
				writer.WriteHandle(requester);
				return writer.CreateMessage();
			}
		}
		public Task<bool> GetActiveAsync()
			=> this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Active"), (Message m, object? s) => ReadMessage_v_b(m, (DesktopObject)s!), this);
		public Task<uint> GetVersionAsync()
			=> this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
		public Task<GameModeProperties> GetPropertiesAsync()
		{
			return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
			static GameModeProperties ReadMessage(Message message, DesktopObject _)
			{
				var reader = message.GetBodyReader();
				return ReadProperties(ref reader);
			}
		}
		public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<GameModeProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
		{
			return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
			static PropertyChanges<GameModeProperties> ReadMessage(Message message, DesktopObject _)
			{
				var reader = message.GetBodyReader();
				reader.ReadString(); // interface
				List<string> changed = new(), invalidated = new();
				return new PropertyChanges<GameModeProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
			}
			static string[] ReadInvalidated(ref Reader reader)
			{
				List<string>? invalidated = null;
				ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.String);
				while (reader.HasNext(arrayEnd))
				{
					invalidated ??= new();
					var property = reader.ReadString();
					switch (property)
					{
						case "Active": invalidated.Add("Active"); break;
						case "version": invalidated.Add("Version"); break;
					}
				}
				return invalidated?.ToArray() ?? Array.Empty<string>();
			}
		}
		private static GameModeProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
		{
			var props = new GameModeProperties();
			ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
			while (reader.HasNext(arrayEnd))
			{
				var property = reader.ReadString();
				switch (property)
				{
					case "Active":
						reader.ReadSignature("b"u8);
						props.Active = reader.ReadBool();
						changedList?.Add("Active");
						break;
					case "version":
						reader.ReadSignature("u"u8);
						props.Version = reader.ReadUInt32();
						changedList?.Add("Version");
						break;
					default:
						reader.ReadVariantValue();
						break;
				}
			}
			return props;
		}
	}
	record MemoryMonitorProperties
	{
		public uint Version { get; set; } = default!;
	}
	partial class MemoryMonitor : DesktopObject
	{
		private const string __Interface = "org.freedesktop.portal.MemoryMonitor";
		public MemoryMonitor(DesktopService service, ObjectPath path) : base(service, path)
		{ }
		public ValueTask<IDisposable> WatchLowMemoryWarningAsync(Action<Exception?, byte> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
			=> base.WatchSignalAsync(Service.Destination, __Interface, Path, "LowMemoryWarning", (Message m, object? s) => ReadMessage_y(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
		public Task<uint> GetVersionAsync()
			=> this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
		public Task<MemoryMonitorProperties> GetPropertiesAsync()
		{
			return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
			static MemoryMonitorProperties ReadMessage(Message message, DesktopObject _)
			{
				var reader = message.GetBodyReader();
				return ReadProperties(ref reader);
			}
		}
		public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<MemoryMonitorProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
		{
			return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
			static PropertyChanges<MemoryMonitorProperties> ReadMessage(Message message, DesktopObject _)
			{
				var reader = message.GetBodyReader();
				reader.ReadString(); // interface
				List<string> changed = new(), invalidated = new();
				return new PropertyChanges<MemoryMonitorProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
			}
			static string[] ReadInvalidated(ref Reader reader)
			{
				List<string>? invalidated = null;
				ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.String);
				while (reader.HasNext(arrayEnd))
				{
					invalidated ??= new();
					var property = reader.ReadString();
					switch (property)
					{
						case "version": invalidated.Add("Version"); break;
					}
				}
				return invalidated?.ToArray() ?? Array.Empty<string>();
			}
		}
		private static MemoryMonitorProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
		{
			var props = new MemoryMonitorProperties();
			ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
			while (reader.HasNext(arrayEnd))
			{
				var property = reader.ReadString();
				switch (property)
				{
					case "version":
						reader.ReadSignature("u"u8);
						props.Version = reader.ReadUInt32();
						changedList?.Add("Version");
						break;
					default:
						reader.ReadVariantValue();
						break;
				}
			}
			return props;
		}
	}
	record OpenURIProperties
	{
		public uint Version { get; set; } = default!;
	}
	partial class OpenURI : DesktopObject
	{
		private const string __Interface = "org.freedesktop.portal.OpenURI";
		public OpenURI(DesktopService service, ObjectPath path) : base(service, path)
		{ }
		public Task<ObjectPath> OpenURIAsync(string parentWindow, string uri, Dictionary<string, VariantValue> options)
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "ssa{sv}",
					member: "OpenURI");
				writer.WriteString(parentWindow);
				writer.WriteString(uri);
				writer.WriteDictionary(options);
				return writer.CreateMessage();
			}
		}
		public Task<ObjectPath> OpenFileAsync(string parentWindow, System.Runtime.InteropServices.SafeHandle fd, Dictionary<string, VariantValue> options)
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "sha{sv}",
					member: "OpenFile");
				writer.WriteString(parentWindow);
				writer.WriteHandle(fd);
				writer.WriteDictionary(options);
				return writer.CreateMessage();
			}
		}
		public Task<ObjectPath> OpenDirectoryAsync(string parentWindow, System.Runtime.InteropServices.SafeHandle fd, Dictionary<string, VariantValue> options)
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "sha{sv}",
					member: "OpenDirectory");
				writer.WriteString(parentWindow);
				writer.WriteHandle(fd);
				writer.WriteDictionary(options);
				return writer.CreateMessage();
			}
		}
		public Task<uint> GetVersionAsync()
			=> this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
		public Task<OpenURIProperties> GetPropertiesAsync()
		{
			return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
			static OpenURIProperties ReadMessage(Message message, DesktopObject _)
			{
				var reader = message.GetBodyReader();
				return ReadProperties(ref reader);
			}
		}
		public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<OpenURIProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
		{
			return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
			static PropertyChanges<OpenURIProperties> ReadMessage(Message message, DesktopObject _)
			{
				var reader = message.GetBodyReader();
				reader.ReadString(); // interface
				List<string> changed = new(), invalidated = new();
				return new PropertyChanges<OpenURIProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
			}
			static string[] ReadInvalidated(ref Reader reader)
			{
				List<string>? invalidated = null;
				ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.String);
				while (reader.HasNext(arrayEnd))
				{
					invalidated ??= new();
					var property = reader.ReadString();
					switch (property)
					{
						case "version": invalidated.Add("Version"); break;
					}
				}
				return invalidated?.ToArray() ?? Array.Empty<string>();
			}
		}
		private static OpenURIProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
		{
			var props = new OpenURIProperties();
			ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
			while (reader.HasNext(arrayEnd))
			{
				var property = reader.ReadString();
				switch (property)
				{
					case "version":
						reader.ReadSignature("u"u8);
						props.Version = reader.ReadUInt32();
						changedList?.Add("Version");
						break;
					default:
						reader.ReadVariantValue();
						break;
				}
			}
			return props;
		}
	}
	record RealtimeProperties
	{
		public int MaxRealtimePriority { get; set; } = default!;
		public int MinNiceLevel { get; set; } = default!;
		public long RTTimeUSecMax { get; set; } = default!;
		public uint Version { get; set; } = default!;
	}
	partial class Realtime : DesktopObject
	{
		private const string __Interface = "org.freedesktop.portal.Realtime";
		public Realtime(DesktopService service, ObjectPath path) : base(service, path)
		{ }
		public Task MakeThreadRealtimeWithPIDAsync(ulong process, ulong thread, uint priority)
		{
			return this.Connection.CallMethodAsync(CreateMessage());
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "ttu",
					member: "MakeThreadRealtimeWithPID");
				writer.WriteUInt64(process);
				writer.WriteUInt64(thread);
				writer.WriteUInt32(priority);
				return writer.CreateMessage();
			}
		}
		public Task MakeThreadHighPriorityWithPIDAsync(ulong process, ulong thread, int priority)
		{
			return this.Connection.CallMethodAsync(CreateMessage());
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "tti",
					member: "MakeThreadHighPriorityWithPID");
				writer.WriteUInt64(process);
				writer.WriteUInt64(thread);
				writer.WriteInt32(priority);
				return writer.CreateMessage();
			}
		}
		public Task<int> GetMaxRealtimePriorityAsync()
			=> this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "MaxRealtimePriority"), (Message m, object? s) => ReadMessage_v_i(m, (DesktopObject)s!), this);
		public Task<int> GetMinNiceLevelAsync()
			=> this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "MinNiceLevel"), (Message m, object? s) => ReadMessage_v_i(m, (DesktopObject)s!), this);
		public Task<long> GetRTTimeUSecMaxAsync()
			=> this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "RTTimeUSecMax"), (Message m, object? s) => ReadMessage_v_x(m, (DesktopObject)s!), this);
		public Task<uint> GetVersionAsync()
			=> this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
		public Task<RealtimeProperties> GetPropertiesAsync()
		{
			return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
			static RealtimeProperties ReadMessage(Message message, DesktopObject _)
			{
				var reader = message.GetBodyReader();
				return ReadProperties(ref reader);
			}
		}
		public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<RealtimeProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
		{
			return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
			static PropertyChanges<RealtimeProperties> ReadMessage(Message message, DesktopObject _)
			{
				var reader = message.GetBodyReader();
				reader.ReadString(); // interface
				List<string> changed = new(), invalidated = new();
				return new PropertyChanges<RealtimeProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
			}
			static string[] ReadInvalidated(ref Reader reader)
			{
				List<string>? invalidated = null;
				ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.String);
				while (reader.HasNext(arrayEnd))
				{
					invalidated ??= new();
					var property = reader.ReadString();
					switch (property)
					{
						case "MaxRealtimePriority": invalidated.Add("MaxRealtimePriority"); break;
						case "MinNiceLevel": invalidated.Add("MinNiceLevel"); break;
						case "RTTimeUSecMax": invalidated.Add("RTTimeUSecMax"); break;
						case "version": invalidated.Add("Version"); break;
					}
				}
				return invalidated?.ToArray() ?? Array.Empty<string>();
			}
		}
		private static RealtimeProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
		{
			var props = new RealtimeProperties();
			ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
			while (reader.HasNext(arrayEnd))
			{
				var property = reader.ReadString();
				switch (property)
				{
					case "MaxRealtimePriority":
						reader.ReadSignature("i"u8);
						props.MaxRealtimePriority = reader.ReadInt32();
						changedList?.Add("MaxRealtimePriority");
						break;
					case "MinNiceLevel":
						reader.ReadSignature("i"u8);
						props.MinNiceLevel = reader.ReadInt32();
						changedList?.Add("MinNiceLevel");
						break;
					case "RTTimeUSecMax":
						reader.ReadSignature("x"u8);
						props.RTTimeUSecMax = reader.ReadInt64();
						changedList?.Add("RTTimeUSecMax");
						break;
					case "version":
						reader.ReadSignature("u"u8);
						props.Version = reader.ReadUInt32();
						changedList?.Add("Version");
						break;
					default:
						reader.ReadVariantValue();
						break;
				}
			}
			return props;
		}
	}
	record DynamicLauncherProperties
	{
		public uint SupportedLauncherTypes { get; set; } = default!;
		public uint Version { get; set; } = default!;
	}
	partial class DynamicLauncher : DesktopObject
	{
		private const string __Interface = "org.freedesktop.portal.DynamicLauncher";
		public DynamicLauncher(DesktopService service, ObjectPath path) : base(service, path)
		{ }
		public Task InstallAsync(string token, string desktopFileId, string desktopEntry, Dictionary<string, VariantValue> options)
		{
			return this.Connection.CallMethodAsync(CreateMessage());
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "sssa{sv}",
					member: "Install");
				writer.WriteString(token);
				writer.WriteString(desktopFileId);
				writer.WriteString(desktopEntry);
				writer.WriteDictionary(options);
				return writer.CreateMessage();
			}
		}
		public Task<ObjectPath> PrepareInstallAsync(string parentWindow, string name, VariantValue iconV, Dictionary<string, VariantValue> options)
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "ssva{sv}",
					member: "PrepareInstall");
				writer.WriteString(parentWindow);
				writer.WriteString(name);
				writer.WriteVariant(iconV);
				writer.WriteDictionary(options);
				return writer.CreateMessage();
			}
		}
		public Task<string> RequestInstallTokenAsync(string name, VariantValue iconV, Dictionary<string, VariantValue> options)
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_s(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "sva{sv}",
					member: "RequestInstallToken");
				writer.WriteString(name);
				writer.WriteVariant(iconV);
				writer.WriteDictionary(options);
				return writer.CreateMessage();
			}
		}
		public Task UninstallAsync(string desktopFileId, Dictionary<string, VariantValue> options)
		{
			return this.Connection.CallMethodAsync(CreateMessage());
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "sa{sv}",
					member: "Uninstall");
				writer.WriteString(desktopFileId);
				writer.WriteDictionary(options);
				return writer.CreateMessage();
			}
		}
		public Task<string> GetDesktopEntryAsync(string desktopFileId)
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_s(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "s",
					member: "GetDesktopEntry");
				writer.WriteString(desktopFileId);
				return writer.CreateMessage();
			}
		}
		public Task<(VariantValue IconV, string IconFormat, uint IconSize)> GetIconAsync(string desktopFileId)
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_vsu(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "s",
					member: "GetIcon");
				writer.WriteString(desktopFileId);
				return writer.CreateMessage();
			}
		}
		public Task LaunchAsync(string desktopFileId, Dictionary<string, VariantValue> options)
		{
			return this.Connection.CallMethodAsync(CreateMessage());
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "sa{sv}",
					member: "Launch");
				writer.WriteString(desktopFileId);
				writer.WriteDictionary(options);
				return writer.CreateMessage();
			}
		}
		public Task<uint> GetSupportedLauncherTypesAsync()
			=> this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "SupportedLauncherTypes"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
		public Task<uint> GetVersionAsync()
			=> this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
		public Task<DynamicLauncherProperties> GetPropertiesAsync()
		{
			return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
			static DynamicLauncherProperties ReadMessage(Message message, DesktopObject _)
			{
				var reader = message.GetBodyReader();
				return ReadProperties(ref reader);
			}
		}
		public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<DynamicLauncherProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
		{
			return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
			static PropertyChanges<DynamicLauncherProperties> ReadMessage(Message message, DesktopObject _)
			{
				var reader = message.GetBodyReader();
				reader.ReadString(); // interface
				List<string> changed = new(), invalidated = new();
				return new PropertyChanges<DynamicLauncherProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
			}
			static string[] ReadInvalidated(ref Reader reader)
			{
				List<string>? invalidated = null;
				ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.String);
				while (reader.HasNext(arrayEnd))
				{
					invalidated ??= new();
					var property = reader.ReadString();
					switch (property)
					{
						case "SupportedLauncherTypes": invalidated.Add("SupportedLauncherTypes"); break;
						case "version": invalidated.Add("Version"); break;
					}
				}
				return invalidated?.ToArray() ?? Array.Empty<string>();
			}
		}
		private static DynamicLauncherProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
		{
			var props = new DynamicLauncherProperties();
			ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
			while (reader.HasNext(arrayEnd))
			{
				var property = reader.ReadString();
				switch (property)
				{
					case "SupportedLauncherTypes":
						reader.ReadSignature("u"u8);
						props.SupportedLauncherTypes = reader.ReadUInt32();
						changedList?.Add("SupportedLauncherTypes");
						break;
					case "version":
						reader.ReadSignature("u"u8);
						props.Version = reader.ReadUInt32();
						changedList?.Add("Version");
						break;
					default:
						reader.ReadVariantValue();
						break;
				}
			}
			return props;
		}
	}
	record CameraProperties
	{
		public bool IsCameraPresent { get; set; } = default!;
		public uint Version { get; set; } = default!;
	}
	partial class Camera : DesktopObject
	{
		private const string __Interface = "org.freedesktop.portal.Camera";
		public Camera(DesktopService service, ObjectPath path) : base(service, path)
		{ }
		public Task<ObjectPath> AccessCameraAsync(Dictionary<string, VariantValue> options)
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "a{sv}",
					member: "AccessCamera");
				writer.WriteDictionary(options);
				return writer.CreateMessage();
			}
		}
		public Task<System.Runtime.InteropServices.SafeHandle> OpenPipeWireRemoteAsync(Dictionary<string, VariantValue> options)
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_h(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "a{sv}",
					member: "OpenPipeWireRemote");
				writer.WriteDictionary(options);
				return writer.CreateMessage();
			}
		}
		public Task<bool> GetIsCameraPresentAsync()
			=> this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "IsCameraPresent"), (Message m, object? s) => ReadMessage_v_b(m, (DesktopObject)s!), this);
		public Task<uint> GetVersionAsync()
			=> this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
		public Task<CameraProperties> GetPropertiesAsync()
		{
			return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
			static CameraProperties ReadMessage(Message message, DesktopObject _)
			{
				var reader = message.GetBodyReader();
				return ReadProperties(ref reader);
			}
		}
		public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<CameraProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
		{
			return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
			static PropertyChanges<CameraProperties> ReadMessage(Message message, DesktopObject _)
			{
				var reader = message.GetBodyReader();
				reader.ReadString(); // interface
				List<string> changed = new(), invalidated = new();
				return new PropertyChanges<CameraProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
			}
			static string[] ReadInvalidated(ref Reader reader)
			{
				List<string>? invalidated = null;
				ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.String);
				while (reader.HasNext(arrayEnd))
				{
					invalidated ??= new();
					var property = reader.ReadString();
					switch (property)
					{
						case "IsCameraPresent": invalidated.Add("IsCameraPresent"); break;
						case "version": invalidated.Add("Version"); break;
					}
				}
				return invalidated?.ToArray() ?? Array.Empty<string>();
			}
		}
		private static CameraProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
		{
			var props = new CameraProperties();
			ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
			while (reader.HasNext(arrayEnd))
			{
				var property = reader.ReadString();
				switch (property)
				{
					case "IsCameraPresent":
						reader.ReadSignature("b"u8);
						props.IsCameraPresent = reader.ReadBool();
						changedList?.Add("IsCameraPresent");
						break;
					case "version":
						reader.ReadSignature("u"u8);
						props.Version = reader.ReadUInt32();
						changedList?.Add("Version");
						break;
					default:
						reader.ReadVariantValue();
						break;
				}
			}
			return props;
		}
	}
	record DeviceProperties
	{
		public uint Version { get; set; } = default!;
	}
	partial class Device : DesktopObject
	{
		private const string __Interface = "org.freedesktop.portal.Device";
		public Device(DesktopService service, ObjectPath path) : base(service, path)
		{ }
		public Task<ObjectPath> AccessDeviceAsync(uint pid, string[] devices, Dictionary<string, VariantValue> options)
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "uasa{sv}",
					member: "AccessDevice");
				writer.WriteUInt32(pid);
				writer.WriteArray(devices);
				writer.WriteDictionary(options);
				return writer.CreateMessage();
			}
		}
		public Task<uint> GetVersionAsync()
			=> this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
		public Task<DeviceProperties> GetPropertiesAsync()
		{
			return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
			static DeviceProperties ReadMessage(Message message, DesktopObject _)
			{
				var reader = message.GetBodyReader();
				return ReadProperties(ref reader);
			}
		}
		public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<DeviceProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
		{
			return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
			static PropertyChanges<DeviceProperties> ReadMessage(Message message, DesktopObject _)
			{
				var reader = message.GetBodyReader();
				reader.ReadString(); // interface
				List<string> changed = new(), invalidated = new();
				return new PropertyChanges<DeviceProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
			}
			static string[] ReadInvalidated(ref Reader reader)
			{
				List<string>? invalidated = null;
				ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.String);
				while (reader.HasNext(arrayEnd))
				{
					invalidated ??= new();
					var property = reader.ReadString();
					switch (property)
					{
						case "version": invalidated.Add("Version"); break;
					}
				}
				return invalidated?.ToArray() ?? Array.Empty<string>();
			}
		}
		private static DeviceProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
		{
			var props = new DeviceProperties();
			ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
			while (reader.HasNext(arrayEnd))
			{
				var property = reader.ReadString();
				switch (property)
				{
					case "version":
						reader.ReadSignature("u"u8);
						props.Version = reader.ReadUInt32();
						changedList?.Add("Version");
						break;
					default:
						reader.ReadVariantValue();
						break;
				}
			}
			return props;
		}
	}
	record PowerProfileMonitorProperties
	{
		public bool PowerSaverEnabled { get; set; } = default!;
		public uint Version { get; set; } = default!;
	}
	partial class PowerProfileMonitor : DesktopObject
	{
		private const string __Interface = "org.freedesktop.portal.PowerProfileMonitor";
		public PowerProfileMonitor(DesktopService service, ObjectPath path) : base(service, path)
		{ }
		public Task<bool> GetPowerSaverEnabledAsync()
			=> this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "power-saver-enabled"), (Message m, object? s) => ReadMessage_v_b(m, (DesktopObject)s!), this);
		public Task<uint> GetVersionAsync()
			=> this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
		public Task<PowerProfileMonitorProperties> GetPropertiesAsync()
		{
			return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
			static PowerProfileMonitorProperties ReadMessage(Message message, DesktopObject _)
			{
				var reader = message.GetBodyReader();
				return ReadProperties(ref reader);
			}
		}
		public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<PowerProfileMonitorProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
		{
			return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
			static PropertyChanges<PowerProfileMonitorProperties> ReadMessage(Message message, DesktopObject _)
			{
				var reader = message.GetBodyReader();
				reader.ReadString(); // interface
				List<string> changed = new(), invalidated = new();
				return new PropertyChanges<PowerProfileMonitorProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
			}
			static string[] ReadInvalidated(ref Reader reader)
			{
				List<string>? invalidated = null;
				ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.String);
				while (reader.HasNext(arrayEnd))
				{
					invalidated ??= new();
					var property = reader.ReadString();
					switch (property)
					{
						case "power-saver-enabled": invalidated.Add("PowerSaverEnabled"); break;
						case "version": invalidated.Add("Version"); break;
					}
				}
				return invalidated?.ToArray() ?? Array.Empty<string>();
			}
		}
		private static PowerProfileMonitorProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
		{
			var props = new PowerProfileMonitorProperties();
			ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
			while (reader.HasNext(arrayEnd))
			{
				var property = reader.ReadString();
				switch (property)
				{
					case "power-saver-enabled":
						reader.ReadSignature("b"u8);
						props.PowerSaverEnabled = reader.ReadBool();
						changedList?.Add("PowerSaverEnabled");
						break;
					case "version":
						reader.ReadSignature("u"u8);
						props.Version = reader.ReadUInt32();
						changedList?.Add("Version");
						break;
					default:
						reader.ReadVariantValue();
						break;
				}
			}
			return props;
		}
	}
	record EmailProperties
	{
		public uint Version { get; set; } = default!;
	}
	partial class Email : DesktopObject
	{
		private const string __Interface = "org.freedesktop.portal.Email";
		public Email(DesktopService service, ObjectPath path) : base(service, path)
		{ }
		public Task<ObjectPath> ComposeEmailAsync(string parentWindow, Dictionary<string, VariantValue> options)
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "sa{sv}",
					member: "ComposeEmail");
				writer.WriteString(parentWindow);
				writer.WriteDictionary(options);
				return writer.CreateMessage();
			}
		}
		public Task<uint> GetVersionAsync()
			=> this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
		public Task<EmailProperties> GetPropertiesAsync()
		{
			return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
			static EmailProperties ReadMessage(Message message, DesktopObject _)
			{
				var reader = message.GetBodyReader();
				return ReadProperties(ref reader);
			}
		}
		public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<EmailProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
		{
			return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
			static PropertyChanges<EmailProperties> ReadMessage(Message message, DesktopObject _)
			{
				var reader = message.GetBodyReader();
				reader.ReadString(); // interface
				List<string> changed = new(), invalidated = new();
				return new PropertyChanges<EmailProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
			}
			static string[] ReadInvalidated(ref Reader reader)
			{
				List<string>? invalidated = null;
				ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.String);
				while (reader.HasNext(arrayEnd))
				{
					invalidated ??= new();
					var property = reader.ReadString();
					switch (property)
					{
						case "version": invalidated.Add("Version"); break;
					}
				}
				return invalidated?.ToArray() ?? Array.Empty<string>();
			}
		}
		private static EmailProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
		{
			var props = new EmailProperties();
			ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
			while (reader.HasNext(arrayEnd))
			{
				var property = reader.ReadString();
				switch (property)
				{
					case "version":
						reader.ReadSignature("u"u8);
						props.Version = reader.ReadUInt32();
						changedList?.Add("Version");
						break;
					default:
						reader.ReadVariantValue();
						break;
				}
			}
			return props;
		}
	}
	record TrashProperties
	{
		public uint Version { get; set; } = default!;
	}
	partial class Trash : DesktopObject
	{
		private const string __Interface = "org.freedesktop.portal.Trash";
		public Trash(DesktopService service, ObjectPath path) : base(service, path)
		{ }
		public Task<uint> TrashFileAsync(System.Runtime.InteropServices.SafeHandle fd)
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_u(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "h",
					member: "TrashFile");
				writer.WriteHandle(fd);
				return writer.CreateMessage();
			}
		}
		public Task<uint> GetVersionAsync()
			=> this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
		public Task<TrashProperties> GetPropertiesAsync()
		{
			return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
			static TrashProperties ReadMessage(Message message, DesktopObject _)
			{
				var reader = message.GetBodyReader();
				return ReadProperties(ref reader);
			}
		}
		public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<TrashProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
		{
			return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
			static PropertyChanges<TrashProperties> ReadMessage(Message message, DesktopObject _)
			{
				var reader = message.GetBodyReader();
				reader.ReadString(); // interface
				List<string> changed = new(), invalidated = new();
				return new PropertyChanges<TrashProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
			}
			static string[] ReadInvalidated(ref Reader reader)
			{
				List<string>? invalidated = null;
				ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.String);
				while (reader.HasNext(arrayEnd))
				{
					invalidated ??= new();
					var property = reader.ReadString();
					switch (property)
					{
						case "version": invalidated.Add("Version"); break;
					}
				}
				return invalidated?.ToArray() ?? Array.Empty<string>();
			}
		}
		private static TrashProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
		{
			var props = new TrashProperties();
			ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
			while (reader.HasNext(arrayEnd))
			{
				var property = reader.ReadString();
				switch (property)
				{
					case "version":
						reader.ReadSignature("u"u8);
						props.Version = reader.ReadUInt32();
						changedList?.Add("Version");
						break;
					default:
						reader.ReadVariantValue();
						break;
				}
			}
			return props;
		}
	}
	record ProxyResolverProperties
	{
		public uint Version { get; set; } = default!;
	}
	partial class ProxyResolver : DesktopObject
	{
		private const string __Interface = "org.freedesktop.portal.ProxyResolver";
		public ProxyResolver(DesktopService service, ObjectPath path) : base(service, path)
		{ }
		public Task<string[]> LookupAsync(string uri)
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_as(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "s",
					member: "Lookup");
				writer.WriteString(uri);
				return writer.CreateMessage();
			}
		}
		public Task<uint> GetVersionAsync()
			=> this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
		public Task<ProxyResolverProperties> GetPropertiesAsync()
		{
			return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
			static ProxyResolverProperties ReadMessage(Message message, DesktopObject _)
			{
				var reader = message.GetBodyReader();
				return ReadProperties(ref reader);
			}
		}
		public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<ProxyResolverProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
		{
			return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
			static PropertyChanges<ProxyResolverProperties> ReadMessage(Message message, DesktopObject _)
			{
				var reader = message.GetBodyReader();
				reader.ReadString(); // interface
				List<string> changed = new(), invalidated = new();
				return new PropertyChanges<ProxyResolverProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
			}
			static string[] ReadInvalidated(ref Reader reader)
			{
				List<string>? invalidated = null;
				ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.String);
				while (reader.HasNext(arrayEnd))
				{
					invalidated ??= new();
					var property = reader.ReadString();
					switch (property)
					{
						case "version": invalidated.Add("Version"); break;
					}
				}
				return invalidated?.ToArray() ?? Array.Empty<string>();
			}
		}
		private static ProxyResolverProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
		{
			var props = new ProxyResolverProperties();
			ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
			while (reader.HasNext(arrayEnd))
			{
				var property = reader.ReadString();
				switch (property)
				{
					case "version":
						reader.ReadSignature("u"u8);
						props.Version = reader.ReadUInt32();
						changedList?.Add("Version");
						break;
					default:
						reader.ReadVariantValue();
						break;
				}
			}
			return props;
		}
	}
	record FileChooserProperties
	{
		public uint Version { get; set; } = default!;
	}
	partial class FileChooser : DesktopObject
	{
		private const string __Interface = "org.freedesktop.portal.FileChooser";
		public FileChooser(DesktopService service, ObjectPath path) : base(service, path)
		{ }
		public Task<ObjectPath> OpenFileAsync(string parentWindow, string title, Dictionary<string, VariantValue> options)
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "ssa{sv}",
					member: "OpenFile");
				writer.WriteString(parentWindow);
				writer.WriteString(title);
				writer.WriteDictionary(options);
				return writer.CreateMessage();
			}
		}
		public Task<ObjectPath> SaveFileAsync(string parentWindow, string title, Dictionary<string, VariantValue> options)
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "ssa{sv}",
					member: "SaveFile");
				writer.WriteString(parentWindow);
				writer.WriteString(title);
				writer.WriteDictionary(options);
				return writer.CreateMessage();
			}
		}
		public Task<ObjectPath> SaveFilesAsync(string parentWindow, string title, Dictionary<string, VariantValue> options)
		{
			return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					signature: "ssa{sv}",
					member: "SaveFiles");
				writer.WriteString(parentWindow);
				writer.WriteString(title);
				writer.WriteDictionary(options);
				return writer.CreateMessage();
			}
		}
		public Task<uint> GetVersionAsync()
			=> this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
		public Task<FileChooserProperties> GetPropertiesAsync()
		{
			return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
			static FileChooserProperties ReadMessage(Message message, DesktopObject _)
			{
				var reader = message.GetBodyReader();
				return ReadProperties(ref reader);
			}
		}
		public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<FileChooserProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
		{
			return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
			static PropertyChanges<FileChooserProperties> ReadMessage(Message message, DesktopObject _)
			{
				var reader = message.GetBodyReader();
				reader.ReadString(); // interface
				List<string> changed = new(), invalidated = new();
				return new PropertyChanges<FileChooserProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
			}
			static string[] ReadInvalidated(ref Reader reader)
			{
				List<string>? invalidated = null;
				ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.String);
				while (reader.HasNext(arrayEnd))
				{
					invalidated ??= new();
					var property = reader.ReadString();
					switch (property)
					{
						case "version": invalidated.Add("Version"); break;
					}
				}
				return invalidated?.ToArray() ?? Array.Empty<string>();
			}
		}
		private static FileChooserProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
		{
			var props = new FileChooserProperties();
			ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
			while (reader.HasNext(arrayEnd))
			{
				var property = reader.ReadString();
				switch (property)
				{
					case "version":
						reader.ReadSignature("u"u8);
						props.Version = reader.ReadUInt32();
						changedList?.Add("Version");
						break;
					default:
						reader.ReadVariantValue();
						break;
				}
			}
			return props;
		}
	}
	partial class DesktopService
	{
		public Tmds.DBus.Protocol.Connection Connection { get; }
		public string Destination { get; }
		public DesktopService(Tmds.DBus.Protocol.Connection connection, string destination)
			=> (Connection, Destination) = (connection, destination);
		public Inhibit CreateInhibit(ObjectPath path) => new Inhibit(this, path);
		public Location CreateLocation(ObjectPath path) => new Location(this, path);
		public Notification CreateNotification(ObjectPath path) => new Notification(this, path);
		public Account CreateAccount(ObjectPath path) => new Account(this, path);
		public NetworkMonitor CreateNetworkMonitor(ObjectPath path) => new NetworkMonitor(this, path);
		public Print CreatePrint(ObjectPath path) => new Print(this, path);
		public Settings CreateSettings(ObjectPath path) => new Settings(this, path);
		public GameMode CreateGameMode(ObjectPath path) => new GameMode(this, path);
		public MemoryMonitor CreateMemoryMonitor(ObjectPath path) => new MemoryMonitor(this, path);
		public OpenURI CreateOpenURI(ObjectPath path) => new OpenURI(this, path);
		public Realtime CreateRealtime(ObjectPath path) => new Realtime(this, path);
		public DynamicLauncher CreateDynamicLauncher(ObjectPath path) => new DynamicLauncher(this, path);
		public Camera CreateCamera(ObjectPath path) => new Camera(this, path);
		public Device CreateDevice(ObjectPath path) => new Device(this, path);
		public PowerProfileMonitor CreatePowerProfileMonitor(ObjectPath path) => new PowerProfileMonitor(this, path);
		public Email CreateEmail(ObjectPath path) => new Email(this, path);
		public Trash CreateTrash(ObjectPath path) => new Trash(this, path);
		public ProxyResolver CreateProxyResolver(ObjectPath path) => new ProxyResolver(this, path);
		public FileChooser CreateFileChooser(ObjectPath path) => new FileChooser(this, path);

		public Request CreateRequest(ObjectPath path) => new Request(this, path);
	}
	partial class Request : DesktopObject
	{
		private const string __Interface = "org.freedesktop.portal.Request";
		public Request(DesktopService service, ObjectPath path) : base(service, path)
		{ }
		public Task CloseAsync()
		{
			return this.Connection.CallMethodAsync(CreateMessage());
			MessageBuffer CreateMessage()
			{
				var writer = this.Connection.GetMessageWriter();
				writer.WriteMethodCallHeader(
					destination: Service.Destination,
					path: Path,
					@interface: __Interface,
					member: "Close");
				return writer.CreateMessage();
			}
		}
		public ValueTask<IDisposable> WatchResponseAsync(Action<Exception?, (uint Response, Dictionary<string, VariantValue> Results)> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
			=> base.WatchSignalAsync(Service.Destination, __Interface, Path, "Response", (Message m, object? s) => ReadMessage_uaesv(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
	}
	class DesktopObject
	{
		public DesktopService Service { get; }
		public ObjectPath Path { get; }
		protected Tmds.DBus.Protocol.Connection Connection => Service.Connection;
		protected DesktopObject(DesktopService service, ObjectPath path)
			=> (Service, Path) = (service, path);
		protected MessageBuffer CreateGetPropertyMessage(string @interface, string property)
		{
			var writer = this.Connection.GetMessageWriter();
			writer.WriteMethodCallHeader(
				destination: Service.Destination,
				path: Path,
				@interface: "org.freedesktop.DBus.Properties",
				signature: "ss",
				member: "Get");
			writer.WriteString(@interface);
			writer.WriteString(property);
			return writer.CreateMessage();
		}
		protected MessageBuffer CreateGetAllPropertiesMessage(string @interface)
		{
			var writer = this.Connection.GetMessageWriter();
			writer.WriteMethodCallHeader(
				destination: Service.Destination,
				path: Path,
				@interface: "org.freedesktop.DBus.Properties",
				signature: "s",
				member: "GetAll");
			writer.WriteString(@interface);
			return writer.CreateMessage();
		}
		protected ValueTask<IDisposable> WatchPropertiesChangedAsync<TProperties>(string @interface, MessageValueReader<PropertyChanges<TProperties>> reader, Action<Exception?, PropertyChanges<TProperties>> handler, bool emitOnCapturedContext, ObserverFlags flags)
		{
			var rule = new MatchRule
			{
				Type = MessageType.Signal,
				Sender = Service.Destination,
				Path = Path,
				Interface = "org.freedesktop.DBus.Properties",
				Member = "PropertiesChanged",
				Arg0 = @interface
			};
			return this.Connection.AddMatchAsync(rule, reader,
													(Exception? ex, PropertyChanges<TProperties> changes, object? rs, object? hs) => ((Action<Exception?, PropertyChanges<TProperties>>)hs!).Invoke(ex, changes),
													this, handler, emitOnCapturedContext, flags);
		}
		public ValueTask<IDisposable> WatchSignalAsync<TArg>(string sender, string @interface, ObjectPath path, string signal, MessageValueReader<TArg> reader, Action<Exception?, TArg> handler, bool emitOnCapturedContext, ObserverFlags flags)
		{
			var rule = new MatchRule
			{
				Type = MessageType.Signal,
				Sender = sender,
				Path = path,
				Member = signal,
				Interface = @interface
			};
			return this.Connection.AddMatchAsync(rule, reader,
													(Exception? ex, TArg arg, object? rs, object? hs) => ((Action<Exception?, TArg>)hs!).Invoke(ex, arg),
													this, handler, emitOnCapturedContext, flags);
		}
		public ValueTask<IDisposable> WatchSignalAsync(string sender, string @interface, ObjectPath path, string signal, Action<Exception?> handler, bool emitOnCapturedContext, ObserverFlags flags)
		{
			var rule = new MatchRule
			{
				Type = MessageType.Signal,
				Sender = sender,
				Path = path,
				Member = signal,
				Interface = @interface
			};
			return this.Connection.AddMatchAsync<object>(rule, (Message message, object? state) => null!,
															(Exception? ex, object v, object? rs, object? hs) => ((Action<Exception?>)hs!).Invoke(ex), this, handler, emitOnCapturedContext, flags);
		}
		protected static ObjectPath ReadMessage_o(Message message, DesktopObject _)
		{
			var reader = message.GetBodyReader();
			return reader.ReadObjectPath();
		}
		protected static (ObjectPath, Dictionary<string, VariantValue>) ReadMessage_oaesv(Message message, DesktopObject _)
		{
			var reader = message.GetBodyReader();
			var arg0 = reader.ReadObjectPath();
			var arg1 = reader.ReadDictionaryOfStringToVariantValue();
			return (arg0, arg1);
		}
		protected static uint ReadMessage_v_u(Message message, DesktopObject _)
		{
			var reader = message.GetBodyReader();
			reader.ReadSignature("u"u8);
			return reader.ReadUInt32();
		}
		protected static (string, string, VariantValue[]) ReadMessage_ssav(Message message, DesktopObject _)
		{
			var reader = message.GetBodyReader();
			var arg0 = reader.ReadString();
			var arg1 = reader.ReadString();
			var arg2 = reader.ReadArrayOfVariantValue();
			return (arg0, arg1, arg2);
		}
		protected static bool ReadMessage_b(Message message, DesktopObject _)
		{
			var reader = message.GetBodyReader();
			return reader.ReadBool();
		}
		protected static uint ReadMessage_u(Message message, DesktopObject _)
		{
			var reader = message.GetBodyReader();
			return reader.ReadUInt32();
		}
		protected static Dictionary<string, VariantValue> ReadMessage_aesv(Message message, DesktopObject _)
		{
			var reader = message.GetBodyReader();
			return reader.ReadDictionaryOfStringToVariantValue();
		}
		protected static Dictionary<string, Dictionary<string, VariantValue>> ReadMessage_aesaesv(Message message, DesktopObject _)
		{
			var reader = message.GetBodyReader();
			return ReadType_aesaesv(ref reader);
		}
		protected static VariantValue ReadMessage_v(Message message, DesktopObject _)
		{
			var reader = message.GetBodyReader();
			return reader.ReadVariantValue();
		}
		protected static (string, string, VariantValue) ReadMessage_ssv(Message message, DesktopObject _)
		{
			var reader = message.GetBodyReader();
			var arg0 = reader.ReadString();
			var arg1 = reader.ReadString();
			var arg2 = reader.ReadVariantValue();
			return (arg0, arg1, arg2);
		}
		protected static int ReadMessage_i(Message message, DesktopObject _)
		{
			var reader = message.GetBodyReader();
			return reader.ReadInt32();
		}
		protected static bool ReadMessage_v_b(Message message, DesktopObject _)
		{
			var reader = message.GetBodyReader();
			reader.ReadSignature("b"u8);
			return reader.ReadBool();
		}
		protected static byte ReadMessage_y(Message message, DesktopObject _)
		{
			var reader = message.GetBodyReader();
			return reader.ReadByte();
		}
		protected static int ReadMessage_v_i(Message message, DesktopObject _)
		{
			var reader = message.GetBodyReader();
			reader.ReadSignature("i"u8);
			return reader.ReadInt32();
		}
		protected static long ReadMessage_v_x(Message message, DesktopObject _)
		{
			var reader = message.GetBodyReader();
			reader.ReadSignature("x"u8);
			return reader.ReadInt64();
		}
		protected static string ReadMessage_s(Message message, DesktopObject _)
		{
			var reader = message.GetBodyReader();
			return reader.ReadString();
		}
		protected static (VariantValue, string, uint) ReadMessage_vsu(Message message, DesktopObject _)
		{
			var reader = message.GetBodyReader();
			var arg0 = reader.ReadVariantValue();
			var arg1 = reader.ReadString();
			var arg2 = reader.ReadUInt32();
			return (arg0, arg1, arg2);
		}
		protected static System.Runtime.InteropServices.SafeHandle ReadMessage_h(Message message, DesktopObject _)
		{
			var reader = message.GetBodyReader();
			return reader.ReadHandle<Microsoft.Win32.SafeHandles.SafeFileHandle>()!;
		}
		protected static string[] ReadMessage_as(Message message, DesktopObject _)
		{
			var reader = message.GetBodyReader();
			return reader.ReadArrayOfString();
		}
		protected static (uint, Dictionary<string, VariantValue>) ReadMessage_uaesv(Message message, DesktopObject _)
		{
			var reader = message.GetBodyReader();
			var arg0 = reader.ReadUInt32();
			var arg1 = reader.ReadDictionaryOfStringToVariantValue();
			return (arg0, arg1);
		}
		protected static Dictionary<string, Dictionary<string, VariantValue>> ReadType_aesaesv(ref Reader reader)
		{
			Dictionary<string, Dictionary<string, VariantValue>> dictionary = new();
			ArrayEnd dictEnd = reader.ReadDictionaryStart();
			while (reader.HasNext(dictEnd))
			{
				var key = reader.ReadString();
				var value = reader.ReadDictionaryOfStringToVariantValue();
				dictionary[key] = value;
			}
			return dictionary;
		}
	}
	class PropertyChanges<TProperties>
	{
		public PropertyChanges(TProperties properties, string[] invalidated, string[] changed)
			=> (Properties, Invalidated, Changed) = (properties, invalidated, changed);
		public TProperties Properties { get; }
		public string[] Invalidated { get; }
		public string[] Changed { get; }
		public bool HasChanged(string property) => Array.IndexOf(Changed, property) != -1;
		public bool IsInvalidated(string property) => Array.IndexOf(Invalidated, property) != -1;
	}
}
