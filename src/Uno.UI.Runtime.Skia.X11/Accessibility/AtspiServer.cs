#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using Uno.Foundation.Logging;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// Serves an AT-SPI accessibility tree over the desktop accessibility D-Bus.
/// </summary>
internal sealed class AtspiServer
{
	private readonly DBusConnection _connection;
	private readonly string _uniqueName;
	private AtspiReference _rootParent;
	private IReadOnlyDictionary<string, AtspiNode> _nodesByPath = new Dictionary<string, AtspiNode>();

	private AtspiServer(DBusConnection connection, string uniqueName)
	{
		_connection = connection;
		_uniqueName = uniqueName;
		_rootParent = AtspiReference.Null;
	}

	public static async Task<AtspiServer?> TryStartAsync(string applicationName)
	{
		DBusConnection? connection = null;

		try
		{
			var sessionBusAddress = DBusAddress.Session;
			if (sessionBusAddress is null)
			{
				if (typeof(AtspiServer).Log().IsEnabled(LogLevel.Debug))
				{
					typeof(AtspiServer).Log().Debug("No D-Bus session bus available; AT-SPI bridge will be disabled.");
				}

				return null;
			}

			var a11yBusAddress = await GetA11yBusAddressAsync(sessionBusAddress);
			if (string.IsNullOrEmpty(a11yBusAddress))
			{
				if (typeof(AtspiServer).Log().IsEnabled(LogLevel.Debug))
				{
					typeof(AtspiServer).Log().Debug("No AT-SPI accessibility bus address available; AT-SPI bridge will be disabled.");
				}

				return null;
			}

			connection = new DBusConnection(a11yBusAddress);
			await connection.ConnectAsync();

			var uniqueName = connection.UniqueName ?? "";
			var server = new AtspiServer(connection, uniqueName);
			server.SetRoot(new AtspiNode
			{
				Path = AtspiDbus.RootPath,
				Name = applicationName,
				Role = AtspiDbus.ApplicationRole,
				RoleName = AtspiDbus.ApplicationRoleName
			});
			connection.AddMethodHandler(new NodeHandler(server));
			server._rootParent = await server.EmbedAsync();

			if (server.Log().IsEnabled(LogLevel.Debug))
			{
				server.Log().Debug($"AT-SPI bridge started for '{applicationName}' on {uniqueName}.");
			}

			return server;
		}
		catch (Exception ex)
		{
			connection?.Dispose();

			if (typeof(AtspiServer).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(AtspiServer).Log().Debug($"Unable to start AT-SPI bridge: {ex.Message}");
			}

			return null;
		}
	}

	public void SetRoot(AtspiNode root)
	{
		if (root.Path != AtspiDbus.RootPath)
		{
			throw new ArgumentException($"The AT-SPI root node path must be '{AtspiDbus.RootPath}'.", nameof(root));
		}

		var nodesByPath = new Dictionary<string, AtspiNode>(StringComparer.Ordinal);
		AddNode(root, nodesByPath);
		Volatile.Write(ref _nodesByPath, nodesByPath);
	}

	public ValueTask StopAsync()
	{
		_connection.Dispose();
		return default;
	}

	private static async Task<string?> GetA11yBusAddressAsync(string sessionBusAddress)
	{
		using var sessionConnection = new DBusConnection(sessionBusAddress);
		await sessionConnection.ConnectAsync();

		MessageBuffer request;
		using (var writer = sessionConnection.GetMessageWriter())
		{
			writer.WriteMethodCallHeader(
				AtspiDbus.BusService,
				AtspiDbus.BusPath,
				AtspiDbus.BusInterface,
				AtspiDbus.GetAddressMethod,
				AtspiDbus.EmptySignature,
				MessageFlags.None);
			request = writer.CreateMessage();
		}

		return await sessionConnection.CallMethodAsync<string>(
			request,
			static (message, _) => message.GetBodyReader().ReadString(),
			null);
	}

	private async Task<AtspiReference> EmbedAsync()
	{
		MessageBuffer request;
		using (var writer = _connection.GetMessageWriter())
		{
			writer.WriteMethodCallHeader(
				AtspiDbus.RegistryService,
				AtspiDbus.RootPath,
				AtspiDbus.SocketInterface,
				AtspiDbus.EmbedMethod,
				AtspiDbus.ReferenceSignature,
				MessageFlags.None);
			WriteReference(writer, new AtspiReference(_uniqueName, AtspiDbus.RootPath));
			request = writer.CreateMessage();
		}

		return await _connection.CallMethodAsync<AtspiReference>(
			request,
			static (message, _) =>
			{
				var reader = message.GetBodyReader();
				reader.AlignStruct();
				return new AtspiReference(reader.ReadString(), reader.ReadObjectPathAsString());
			},
			null);
	}

	private AtspiNode? FindNode(string path)
	{
		var nodesByPath = Volatile.Read(ref _nodesByPath);
		return nodesByPath.TryGetValue(path, out var node) ? node : null;
	}

	private AtspiReference GetReference(AtspiNode? node)
		=> node is null
			? new AtspiReference(_uniqueName, AtspiDbus.NullPath)
			: new AtspiReference(_uniqueName, node.Path);

	private AtspiReference GetParentReference(AtspiNode node)
		=> node.Parent is null ? _rootParent : GetReference(node.Parent);

	private static void AddNode(AtspiNode node, Dictionary<string, AtspiNode> nodesByPath)
	{
		nodesByPath[node.Path] = node;

		foreach (var child in node.Children)
		{
			AddNode(child, nodesByPath);
		}
	}

	private static int ToInt32(double value) => (int)Math.Round(value);

	private static bool Contains(AtspiNode node, int x, int y)
		=> x >= ToInt32(node.X)
			&& y >= ToInt32(node.Y)
			&& x < ToInt32(node.X + node.W)
			&& y < ToInt32(node.Y + node.H);

	private static AtspiNode? FindAccessibleAtPoint(AtspiNode node, int x, int y)
	{
		for (var i = node.Children.Count - 1; i >= 0; i--)
		{
			var child = node.Children[i];
			if (Contains(child, x, y))
			{
				return FindAccessibleAtPoint(child, x, y) ?? child;
			}
		}

		return Contains(node, x, y) ? node : null;
	}

	private static void WriteReference(MessageWriter writer, AtspiReference reference)
	{
		writer.WriteStructureStart();
		writer.WriteString(reference.Service);
		writer.WriteObjectPath(reference.Path);
	}

	private readonly record struct AtspiReference(string Service, string Path)
	{
		public static AtspiReference Null { get; } = new(AtspiDbus.RegistryService, AtspiDbus.NullPath);
	}

	/// <summary>
	/// Dispatches AT-SPI method calls for the currently published node snapshot.
	/// </summary>
	private sealed class NodeHandler : IPathMethodHandler
	{
		private readonly AtspiServer _server;

		public NodeHandler(AtspiServer server)
		{
			_server = server;
		}

		public string Path => AtspiDbus.AccessibleBasePath;

		public bool HandlesChildPaths => true;

		public ValueTask HandleMethodAsync(MethodContext context)
		{
			var node = context.Request.PathAsString is { } requestPath ? _server.FindNode(requestPath) : null;
			if (node is null)
			{
				context.ReplyUnknownMethodError();
				return default;
			}

			var iface = context.Request.InterfaceAsString ?? AtspiDbus.EmptyString;
			var member = context.Request.MemberAsString ?? AtspiDbus.EmptyString;

			switch (iface)
			{
				case AtspiDbus.AccessibleInterface:
					Accessible(context, node, member);
					break;
				case AtspiDbus.ComponentInterface:
					Component(context, node, member);
					break;
				case AtspiDbus.ApplicationInterface when node.Parent is null:
					Application(context, member);
					break;
				case AtspiDbus.PropertiesInterface:
					Properties(context, node, member);
					break;
				case AtspiDbus.IntrospectableInterface when member == AtspiDbus.IntrospectMethod:
					ReplyString(context, AtspiDbus.StringSignature, AtspiDbus.IntrospectionXml);
					break;
				default:
					context.ReplyUnknownMethodError();
					break;
			}

			return default;
		}

		private void Accessible(MethodContext context, AtspiNode node, string member)
		{
			switch (member)
			{
				case AtspiDbus.GetRoleMethod:
					ReplyUInt32(context, node.Role);
					break;
				case AtspiDbus.GetRoleNameMethod:
				case AtspiDbus.GetLocalizedRoleNameMethod:
					ReplyString(context, AtspiDbus.StringSignature, node.RoleName);
					break;
				case AtspiDbus.GetStateMethod:
					ReplyStates(context, node);
					break;
				case AtspiDbus.GetInterfacesMethod:
					ReplyInterfaces(context, node);
					break;
				case AtspiDbus.GetChildAtIndexMethod:
					ReplyChildAtIndex(context, node);
					break;
				case AtspiDbus.GetChildrenMethod:
					ReplyChildren(context, node);
					break;
				case AtspiDbus.GetIndexInParentMethod:
					ReplyInt32(context, node.Parent?.Children.IndexOf(node) ?? -1);
					break;
				case AtspiDbus.GetApplicationMethod:
					ReplyReference(context, new AtspiReference(_server._uniqueName, AtspiDbus.RootPath));
					break;
				case AtspiDbus.GetParentMethod:
					ReplyReference(context, _server.GetParentReference(node));
					break;
				case AtspiDbus.GetAttributesMethod:
					ReplyEmptyDictionary(context, AtspiDbus.StringDictionarySignature);
					break;
				case AtspiDbus.GetRelationSetMethod:
					ReplyEmptyStructArray(context, AtspiDbus.RelationSetSignature);
					break;
				default:
					context.ReplyUnknownMethodError();
					break;
			}
		}

		private void Component(MethodContext context, AtspiNode node, string member)
		{
			switch (member)
			{
				case AtspiDbus.GetExtentsMethod:
					ReplyExtents(context, node);
					break;
				case AtspiDbus.GetPositionMethod:
					ReplyPosition(context, node);
					break;
				case AtspiDbus.GetSizeMethod:
					ReplySize(context, node);
					break;
				case AtspiDbus.GetLayerMethod:
					ReplyUInt32(context, AtspiDbus.WindowLayer);
					break;
				case AtspiDbus.ContainsMethod:
					ReplyContains(context, node);
					break;
				case AtspiDbus.GetAccessibleAtPointMethod:
					ReplyAccessibleAtPoint(context, node);
					break;
				case AtspiDbus.GrabFocusMethod:
					ReplyBool(context, false);
					break;
				default:
					context.ReplyUnknownMethodError();
					break;
			}
		}

		private static void Application(MethodContext context, string member)
		{
			switch (member)
			{
				case AtspiDbus.GetLocaleMethod:
					ReplyString(context, AtspiDbus.StringSignature, AtspiDbus.DefaultLocale);
					break;
				default:
					context.ReplyUnknownMethodError();
					break;
			}
		}

		private void Properties(MethodContext context, AtspiNode node, string member)
		{
			switch (member)
			{
				case AtspiDbus.GetPropertyMethod:
				{
					var reader = context.Request.GetBodyReader();
					var propertyInterface = reader.ReadString();
					var property = reader.ReadString();
					ReplyVariant(context, node, propertyInterface, property);
					break;
				}
				case AtspiDbus.SetPropertyMethod:
					ReplyVoid(context);
					break;
				case AtspiDbus.GetAllPropertiesMethod:
					ReplyEmptyVariantDictionary(context);
					break;
				default:
					context.ReplyUnknownMethodError();
					break;
			}
		}

		private void ReplyVariant(MethodContext context, AtspiNode node, string propertyInterface, string property)
		{
			using var writer = context.CreateReplyWriter(AtspiDbus.VariantSignature);

			switch (propertyInterface, property)
			{
				case (AtspiDbus.AccessibleInterface, AtspiDbus.NameProperty):
					writer.WriteVariantString(node.Name);
					break;
				case (AtspiDbus.AccessibleInterface, AtspiDbus.DescriptionProperty):
				case (AtspiDbus.AccessibleInterface, AtspiDbus.AccessibleIdProperty):
					writer.WriteVariantString(AtspiDbus.EmptyString);
					break;
				case (AtspiDbus.AccessibleInterface, AtspiDbus.ChildCountProperty):
					writer.WriteVariantInt32(node.Children.Count);
					break;
				case (AtspiDbus.AccessibleInterface, AtspiDbus.ParentProperty):
					writer.WriteSignature(AtspiDbus.ReferenceSignature);
					WriteReference(writer, _server.GetParentReference(node));
					break;
				case (AtspiDbus.ApplicationInterface, AtspiDbus.LocaleProperty):
					writer.WriteVariantString(AtspiDbus.DefaultLocale);
					break;
				case (AtspiDbus.ApplicationInterface, AtspiDbus.ToolkitNameProperty):
					writer.WriteVariantString(AtspiDbus.ToolkitName);
					break;
				case (AtspiDbus.ApplicationInterface, AtspiDbus.VersionProperty):
					writer.WriteVariantString(AtspiDbus.ToolkitVersion);
					break;
				case (AtspiDbus.ApplicationInterface, AtspiDbus.AtspiVersionProperty):
					writer.WriteVariantString(AtspiDbus.AtspiVersion);
					break;
				default:
					writer.WriteVariantString(AtspiDbus.EmptyString);
					break;
			}

			context.Reply(writer.CreateMessage());
		}

		private void ReplyChildAtIndex(MethodContext context, AtspiNode node)
		{
			var index = context.Request.GetBodyReader().ReadInt32();
			var child = index >= 0 && index < node.Children.Count ? node.Children[index] : null;
			ReplyReference(context, _server.GetReference(child));
		}

		private void ReplyChildren(MethodContext context, AtspiNode node)
		{
			using var writer = context.CreateReplyWriter(AtspiDbus.ReferenceArraySignature);
			var array = writer.WriteArrayStart(DBusType.Struct);
			foreach (var child in node.Children)
			{
				WriteReference(writer, _server.GetReference(child));
			}
			writer.WriteArrayEnd(array);
			context.Reply(writer.CreateMessage());
		}

		private static void ReplyExtents(MethodContext context, AtspiNode node)
		{
			using var writer = context.CreateReplyWriter(AtspiDbus.ExtentsSignature);
			writer.WriteStructureStart();
			writer.WriteInt32(ToInt32(node.X));
			writer.WriteInt32(ToInt32(node.Y));
			writer.WriteInt32(ToInt32(node.W));
			writer.WriteInt32(ToInt32(node.H));
			context.Reply(writer.CreateMessage());
		}

		private static void ReplyPosition(MethodContext context, AtspiNode node)
		{
			using var writer = context.CreateReplyWriter(AtspiDbus.PositionSignature);
			writer.WriteStructureStart();
			writer.WriteInt32(ToInt32(node.X));
			writer.WriteInt32(ToInt32(node.Y));
			context.Reply(writer.CreateMessage());
		}

		private static void ReplySize(MethodContext context, AtspiNode node)
		{
			using var writer = context.CreateReplyWriter(AtspiDbus.PositionSignature);
			writer.WriteStructureStart();
			writer.WriteInt32(ToInt32(node.W));
			writer.WriteInt32(ToInt32(node.H));
			context.Reply(writer.CreateMessage());
		}

		private static void ReplyContains(MethodContext context, AtspiNode node)
		{
			var reader = context.Request.GetBodyReader();
			var x = reader.ReadInt32();
			var y = reader.ReadInt32();
			_ = reader.ReadUInt32();
			ReplyBool(context, Contains(node, x, y));
		}

		private void ReplyAccessibleAtPoint(MethodContext context, AtspiNode node)
		{
			var reader = context.Request.GetBodyReader();
			var x = reader.ReadInt32();
			var y = reader.ReadInt32();
			_ = reader.ReadUInt32();
			ReplyReference(context, _server.GetReference(FindAccessibleAtPoint(node, x, y)));
		}

		private static void ReplyStates(MethodContext context, AtspiNode node)
		{
			uint state0 = 0;
			void SetState(int bit) => state0 |= 1u << bit;

			if (node.Enabled)
			{
				SetState(AtspiDbus.EnabledState);
				SetState(AtspiDbus.SensitiveState);
			}

			SetState(AtspiDbus.ShowingState);
			SetState(AtspiDbus.VisibleState);

			if (node.Focusable)
			{
				SetState(AtspiDbus.FocusableState);
			}

			using var writer = context.CreateReplyWriter(AtspiDbus.UInt32ArraySignature);
			var array = writer.WriteArrayStart(DBusType.UInt32);
			writer.WriteUInt32(state0);
			writer.WriteUInt32(0);
			writer.WriteArrayEnd(array);
			context.Reply(writer.CreateMessage());
		}

		private static void ReplyInterfaces(MethodContext context, AtspiNode node)
		{
			using var writer = context.CreateReplyWriter(AtspiDbus.StringArraySignature);
			var array = writer.WriteArrayStart(DBusType.String);
			writer.WriteString(AtspiDbus.AccessibleInterface);
			writer.WriteString(AtspiDbus.ComponentInterface);
			if (node.Parent is null)
			{
				writer.WriteString(AtspiDbus.ApplicationInterface);
			}
			writer.WriteArrayEnd(array);
			context.Reply(writer.CreateMessage());
		}

		private static void ReplyString(MethodContext context, string signature, string value)
		{
			using var writer = context.CreateReplyWriter(signature);
			writer.WriteString(value);
			context.Reply(writer.CreateMessage());
		}

		private static void ReplyUInt32(MethodContext context, uint value)
		{
			using var writer = context.CreateReplyWriter(AtspiDbus.UInt32Signature);
			writer.WriteUInt32(value);
			context.Reply(writer.CreateMessage());
		}

		private static void ReplyInt32(MethodContext context, int value)
		{
			using var writer = context.CreateReplyWriter(AtspiDbus.Int32Signature);
			writer.WriteInt32(value);
			context.Reply(writer.CreateMessage());
		}

		private static void ReplyBool(MethodContext context, bool value)
		{
			using var writer = context.CreateReplyWriter(AtspiDbus.BoolSignature);
			writer.WriteBool(value);
			context.Reply(writer.CreateMessage());
		}

		private static void ReplyVoid(MethodContext context)
		{
			using var writer = context.CreateReplyWriter(AtspiDbus.EmptySignature);
			context.Reply(writer.CreateMessage());
		}

		private static void ReplyReference(MethodContext context, AtspiReference reference)
		{
			using var writer = context.CreateReplyWriter(AtspiDbus.ReferenceSignature);
			WriteReference(writer, reference);
			context.Reply(writer.CreateMessage());
		}

		private static void ReplyEmptyDictionary(MethodContext context, string signature)
		{
			using var writer = context.CreateReplyWriter(signature);
			var dictionary = writer.WriteDictionaryStart();
			writer.WriteDictionaryEnd(dictionary);
			context.Reply(writer.CreateMessage());
		}

		private static void ReplyEmptyVariantDictionary(MethodContext context)
		{
			using var writer = context.CreateReplyWriter(AtspiDbus.VariantDictionarySignature);
			var dictionary = writer.WriteDictionaryStart();
			writer.WriteDictionaryEnd(dictionary);
			context.Reply(writer.CreateMessage());
		}

		private static void ReplyEmptyStructArray(MethodContext context, string signature)
		{
			using var writer = context.CreateReplyWriter(signature);
			var array = writer.WriteArrayStart(DBusType.Struct);
			writer.WriteArrayEnd(array);
			context.Reply(writer.CreateMessage());
		}
	}

	/// <summary>
	/// Contains D-Bus names, signatures, and AT-SPI numeric constants used by the bridge.
	/// </summary>
	private static class AtspiDbus
	{
		public const string AccessibleBasePath = "/org/a11y/atspi/accessible";
		public const string RootPath = "/org/a11y/atspi/accessible/root";
		public const string NullPath = "/org/a11y/atspi/null";
		public const string BusPath = "/org/a11y/bus";
		public const string BusService = "org.a11y.Bus";
		public const string BusInterface = "org.a11y.Bus";
		public const string RegistryService = "org.a11y.atspi.Registry";
		public const string AccessibleInterface = "org.a11y.atspi.Accessible";
		public const string ComponentInterface = "org.a11y.atspi.Component";
		public const string ApplicationInterface = "org.a11y.atspi.Application";
		public const string SocketInterface = "org.a11y.atspi.Socket";
		public const string PropertiesInterface = "org.freedesktop.DBus.Properties";
		public const string IntrospectableInterface = "org.freedesktop.DBus.Introspectable";
		public const string GetAddressMethod = "GetAddress";
		public const string EmbedMethod = "Embed";
		public const string GetRoleMethod = "GetRole";
		public const string GetRoleNameMethod = "GetRoleName";
		public const string GetLocalizedRoleNameMethod = "GetLocalizedRoleName";
		public const string GetStateMethod = "GetState";
		public const string GetInterfacesMethod = "GetInterfaces";
		public const string GetChildAtIndexMethod = "GetChildAtIndex";
		public const string GetChildrenMethod = "GetChildren";
		public const string GetIndexInParentMethod = "GetIndexInParent";
		public const string GetApplicationMethod = "GetApplication";
		public const string GetParentMethod = "GetParent";
		public const string GetAttributesMethod = "GetAttributes";
		public const string GetRelationSetMethod = "GetRelationSet";
		public const string GetExtentsMethod = "GetExtents";
		public const string GetPositionMethod = "GetPosition";
		public const string GetSizeMethod = "GetSize";
		public const string GetLayerMethod = "GetLayer";
		public const string ContainsMethod = "Contains";
		public const string GetAccessibleAtPointMethod = "GetAccessibleAtPoint";
		public const string GrabFocusMethod = "GrabFocus";
		public const string GetLocaleMethod = "GetLocale";
		public const string GetPropertyMethod = "Get";
		public const string SetPropertyMethod = "Set";
		public const string GetAllPropertiesMethod = "GetAll";
		public const string IntrospectMethod = "Introspect";
		public const string NameProperty = "Name";
		public const string DescriptionProperty = "Description";
		public const string AccessibleIdProperty = "AccessibleId";
		public const string ChildCountProperty = "ChildCount";
		public const string ParentProperty = "Parent";
		public const string LocaleProperty = "Locale";
		public const string ToolkitNameProperty = "ToolkitName";
		public const string VersionProperty = "Version";
		public const string AtspiVersionProperty = "AtspiVersion";
		public const string DefaultLocale = "C";
		public const string ToolkitName = "Uno";
		public const string ToolkitVersion = "1.0";
		public const string AtspiVersion = "2.1";
		public const string EmptyString = "";
		public const string EmptySignature = "";
		public const string ReferenceSignature = "(so)";
		public const string ReferenceArraySignature = "a(so)";
		public const string StringSignature = "s";
		public const string UInt32Signature = "u";
		public const string Int32Signature = "i";
		public const string BoolSignature = "b";
		public const string VariantSignature = "v";
		public const string ExtentsSignature = "(iiii)";
		public const string PositionSignature = "(ii)";
		public const string UInt32ArraySignature = "au";
		public const string StringArraySignature = "as";
		public const string StringDictionarySignature = "a{ss}";
		public const string VariantDictionarySignature = "a{sv}";
		public const string RelationSetSignature = "a(ua(so))";
		public const string IntrospectionXml = "<node/>";
		public const string ApplicationRoleName = "application";
		public const uint WindowLayer = 3;
		public const uint ApplicationRole = 75;
		public const int EnabledState = 8;
		public const int FocusableState = 11;
		public const int SensitiveState = 24;
		public const int ShowingState = 25;
		public const int VisibleState = 30;
	}
}
