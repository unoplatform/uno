#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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
	/// <summary>
	/// Drives the real control behind an <see cref="AtspiNode"/>. Implemented by the
	/// accessibility host, which marshals each call to the UI thread and drives the
	/// control through its automation provider. Called from the D-Bus reader thread.
	/// </summary>
	public interface IWriteTarget
	{
		bool Invoke(AtspiNode node);
		bool SetRangeValue(AtspiNode node, double value);
		bool SetText(AtspiNode node, string text);
		bool SelectChild(AtspiNode node, int index);
		// start and end are UTF-16 indices; start == end places the caret.
		bool SetTextSelection(AtspiNode node, int start, int end);
	}

	private readonly DBusConnection _connection;
	private readonly string _uniqueName;
	private readonly IWriteTarget _writeTarget;
	private AtspiReference _rootParent;
	private IReadOnlyDictionary<string, AtspiNode> _nodesByPath = new Dictionary<string, AtspiNode>();
	private volatile AtspiNode? _focusedNode;

	private AtspiServer(DBusConnection connection, string uniqueName, IWriteTarget writeTarget)
	{
		_connection = connection;
		_uniqueName = uniqueName;
		_writeTarget = writeTarget;
		_rootParent = AtspiReference.Null;
	}

	public static async Task<AtspiServer?> TryStartAsync(string applicationName, IWriteTarget writeTarget)
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
			var server = new AtspiServer(connection, uniqueName, writeTarget);
			server.SetRoot(new AtspiNode
			{
				Path = AtspiDbus.RootPath,
				Name = applicationName,
				Role = AtspiDbus.ApplicationRole,
				RoleName = AtspiDbus.ApplicationRoleName
			});
			connection.AddMethodHandler(new NodeHandler(server));

			// The app root's parent is the AT-SPI registry desktop; the Embed reply
			// echoes it back but we do not depend on it.
			server._rootParent = new AtspiReference(AtspiDbus.RegistryService, AtspiDbus.RootPath);
			await server.EmbedAsync();

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
				typeof(AtspiServer).Log().Debug($"Unable to start AT-SPI bridge: {ex}");
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

	/// <summary>
	/// Records the currently focused node so <see cref="GetState"/> can report the
	/// AT-SPI focused state and focus-change signals target the right node.
	/// </summary>
	public void SetFocus(AtspiNode? node)
	{
		// _focusedNode is declared volatile; a plain assignment is already a volatile write.
		_focusedNode = node;
	}

	// ──────────────────────────────────────────────────────────────
	//  Live event emission — org.a11y.atspi.Event.* signals.
	//  MessageWriter is a ref struct in Tmds.DBus.Protocol 0.92; it is
	//  always written inline here, never via a helper that would receive
	//  the writer by value (struct copy ⇒ empty body).
	// ──────────────────────────────────────────────────────────────

	public void EmitStateChanged(AtspiNode node, string detail, int value)
		=> Emit(AtspiDbus.EventObjectInterface, node, AtspiDbus.StateChangedMember, detail, value, 0, 0);

	public void EmitPropertyChange(AtspiNode node, string prop, double value)
		=> Emit(AtspiDbus.EventObjectInterface, node, AtspiDbus.PropertyChangeMember, prop, 0, 0, value);

	public void EmitPropertyChange(AtspiNode node, string prop, string value)
		=> Emit(AtspiDbus.EventObjectInterface, node, AtspiDbus.PropertyChangeMember, prop, 0, 0, value);

	public void EmitSelectionChanged(AtspiNode node)
		=> Emit(AtspiDbus.EventObjectInterface, node, AtspiDbus.SelectionChangedMember, "", 0, 0, 0);

	public void EmitChildrenChanged(AtspiNode parent, bool added, int index, AtspiNode child)
		=> Emit(AtspiDbus.EventObjectInterface, parent, AtspiDbus.ChildrenChangedMember, added ? "add" : "remove", index, 0, GetReference(child));

	// Offsets and lengths are in characters (code points), as AT-SPI Text counts them.
	public void EmitTextChanged(AtspiNode node, bool inserted, int offset, int length, string text)
		=> Emit(AtspiDbus.EventObjectInterface, node, AtspiDbus.TextChangedMember, inserted ? "insert" : "delete", offset, length, text);

	public void EmitTextCaretMoved(AtspiNode node, int offset)
		=> Emit(AtspiDbus.EventObjectInterface, node, AtspiDbus.TextCaretMovedMember, "", offset, 0, 0);

	public void EmitTextSelectionChanged(AtspiNode node)
		=> Emit(AtspiDbus.EventObjectInterface, node, AtspiDbus.TextSelectionChangedMember, "", 0, 0, 0);

	// object:announcement — speech without moving focus; detail1 carries the politeness
	// (ATSPI_LIVE_POLITE = 1, ATSPI_LIVE_ASSERTIVE = 2) and the value the text to present.
	public void EmitAnnouncement(AtspiNode node, string text, bool assertive)
		=> Emit(AtspiDbus.EventObjectInterface, node, AtspiDbus.AnnouncementMember, "", assertive ? 2 : 1, 0, text);

	// window:activate / window:deactivate — screen readers use these to find the active window.
	public void EmitWindowActivation(AtspiNode frame, bool active)
		=> Emit(AtspiDbus.EventWindowInterface, frame, active ? AtspiDbus.ActivateMember : AtspiDbus.DeactivateMember, "", 0, 0, frame.Name);

	private void Emit(string eventInterface, AtspiNode node, string member, string detail, int detail1, int detail2, object value)
	{
		try
		{
			var writer = _connection.GetMessageWriter();
			writer.WriteSignalHeader(null, node.Path, eventInterface, member, AtspiDbus.EventSignature);
			writer.WriteString(detail);
			writer.WriteInt32(detail1);
			writer.WriteInt32(detail2);
			switch (value)
			{
				case int i:
					writer.WriteVariantInt32(i);
					break;
				case double d:
					writer.WriteVariantDouble(d);
					break;
				case string s:
					writer.WriteVariantString(s);
					break;
				case AtspiReference reference:
					writer.WriteSignature(AtspiDbus.ReferenceSignature);
					writer.WriteStructureStart();
					writer.WriteString(reference.Service);
					writer.WriteObjectPath(reference.Path);
					break;
				default:
					throw new ArgumentException($"Unsupported AT-SPI event value {value.GetType()}.", nameof(value));
			}
			// Trailing a{sv} of cached properties (Event.xml); none are pushed, clients query on demand.
			var properties = writer.WriteDictionaryStart();
			writer.WriteDictionaryEnd(properties);
			_connection.TrySendMessage(writer.CreateMessage());
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"AT-SPI {member} emit failed on '{node.Path}': {ex}");
			}
		}
	}

	private static async Task<string?> GetA11yBusAddressAsync(string sessionBusAddress)
	{
		using var sessionConnection = new DBusConnection(sessionBusAddress);
		await sessionConnection.ConnectAsync();

		// MessageWriter is a ref struct in Tmds.DBus.Protocol 0.92 and must not be
		// disposed before the call is sent, or the message's pooled buffer is
		// reclaimed while still in flight.
		var request = BuildGetAddressMessage(sessionConnection);

		return await sessionConnection.CallMethodAsync<string>(
			request,
			static (message, _) => message.GetBodyReader().ReadString(),
			null);
	}

	private static MessageBuffer BuildGetAddressMessage(DBusConnection sessionConnection)
	{
		var writer = sessionConnection.GetMessageWriter();
		writer.WriteMethodCallHeader(
			AtspiDbus.BusService,
			AtspiDbus.BusPath,
			AtspiDbus.BusInterface,
			AtspiDbus.GetAddressMethod,
			AtspiDbus.EmptySignature,
			MessageFlags.None);
		return writer.CreateMessage();
	}

	private async Task EmbedAsync()
	{
		var request = BuildEmbedMessage();
		await _connection.CallMethodAsync(request);
	}

	private MessageBuffer BuildEmbedMessage()
	{
		var writer = _connection.GetMessageWriter();
		writer.WriteMethodCallHeader(
			AtspiDbus.RegistryService,
			AtspiDbus.RootPath,
			AtspiDbus.SocketInterface,
			AtspiDbus.EmbedMethod,
			AtspiDbus.ReferenceSignature,
			MessageFlags.None);
		WriteReference(ref writer, new AtspiReference(_uniqueName, AtspiDbus.RootPath));
		return writer.CreateMessage();
	}

	private AtspiNode? FindNode(string path)
	{
		var nodesByPath = Volatile.Read(ref _nodesByPath);
		return nodesByPath.TryGetValue(path, out var node) ? node : null;
	}

	private AtspiReference GetReference(AtspiNode? node)
		=> node is null
			? AtspiReference.Null
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

	private static void WriteReference(ref MessageWriter writer, AtspiReference reference)
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

		// Registered at the AT-SPI root so every incoming call under /org/a11y/atspi
		// (accessible nodes, but also cache/registry callbacks the daemon makes after
		// Embed) reaches this handler and gets a well-formed reply. Tmds.DBus.Protocol
		// 0.92 tears down the whole connection if an incoming call hits no handler, so
		// leaving sibling paths (e.g. .../cache) uncovered drops the a11y connection.
		public string Path => AtspiDbus.RootObjectPath;

		public bool HandlesChildPaths => true;

		public ValueTask HandleMethodAsync(MethodContext context)
		{
			// A handler exception would tear down the whole AT-SPI connection with it,
			// so a malformed or unexpected client request must never propagate.
			try
			{
				HandleMethodCore(context);
			}
			catch (Exception ex)
			{
				if (_server.Log().IsEnabled(LogLevel.Debug))
				{
					_server.Log().Debug($"AT-SPI request {context.Request.InterfaceAsString}.{context.Request.MemberAsString} on '{context.Request.PathAsString}' failed: {ex}");
				}

				if (!context.ReplySent && !context.NoReplyExpected)
				{
					context.ReplyUnknownMethodError();
				}
			}

			return default;
		}

		private void HandleMethodCore(MethodContext context)
		{
			var iface = context.Request.InterfaceAsString ?? AtspiDbus.EmptyString;
			var member = context.Request.MemberAsString ?? AtspiDbus.EmptyString;

			if (_server.Log().IsEnabled(LogLevel.Trace))
			{
				_server.Log().Trace($"AT-SPI request: {iface}.{member} on '{context.Request.PathAsString}'");
			}

			var node = context.Request.PathAsString is { } requestPath ? _server.FindNode(requestPath) : null;
			if (node is null)
			{
				context.ReplyUnknownMethodError();
				return;
			}

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
				case AtspiDbus.ActionInterface:
					Action(context, node, member);
					break;
				case AtspiDbus.TextInterface:
					Text(context, node, member);
					break;
				case AtspiDbus.EditableTextInterface:
					EditableText(context, node, member);
					break;
				case AtspiDbus.SelectionInterface:
					Selection(context, node, member);
					break;
				case AtspiDbus.PropertiesInterface:
					Properties(context, node, member);
					break;
				case AtspiDbus.IntrospectableInterface when member == AtspiDbus.IntrospectMethod:
					ReplyIntrospection(context, node);
					break;
				default:
					context.ReplyUnknownMethodError();
					break;
			}
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
					ReplyAttributes(context, node);
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
					ReplyUInt32(context, AtspiDbus.WidgetLayer);
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

		private void Action(MethodContext context, AtspiNode node, string member)
		{
			switch (member)
			{
				case AtspiDbus.GetNActionsMethod:
					ReplyInt32(context, Actionable(node) ? 1 : 0);
					break;
				case AtspiDbus.GetActionNameMethod:
				case AtspiDbus.GetLocalizedActionNameMethod:
					_ = context.Request.GetBodyReader().ReadInt32();
					ReplyString(context, AtspiDbus.StringSignature, ActionName(node));
					break;
				case AtspiDbus.GetActionDescriptionMethod:
				case AtspiDbus.GetActionKeyBindingMethod:
					_ = context.Request.GetBodyReader().ReadInt32();
					ReplyString(context, AtspiDbus.StringSignature, AtspiDbus.EmptyString);
					break;
				case AtspiDbus.GetActionsMethod:
					ReplyActions(context, node);
					break;
				case AtspiDbus.DoActionMethod:
					{
						_ = context.Request.GetBodyReader().ReadInt32();
						ReplyBool(context, _server._writeTarget.Invoke(node));
						break;
					}
				default:
					context.ReplyUnknownMethodError();
					break;
			}
		}

		private void Text(MethodContext context, AtspiNode node, string member)
		{
			var text = node.Text;
			var reader = context.Request.GetBodyReader();
			switch (member)
			{
				case AtspiDbus.GetTextMethod:
					{
						var start = reader.ReadInt32();
						var end = reader.ReadInt32();
						var start16 = Utf16Index(text, Math.Max(0, start));
						var end16 = end < 0 ? text.Length : Math.Max(start16, Utf16Index(text, end));
						ReplyString(context, AtspiDbus.StringSignature, text.Substring(start16, end16 - start16));
						break;
					}
				case AtspiDbus.GetCharacterAtOffsetMethod:
					{
						var offset16 = Utf16Index(text, Math.Max(0, reader.ReadInt32()));
						ReplyInt32(context, offset16 < text.Length ? char.ConvertToUtf32(text, offset16) : 0);
						break;
					}
				case AtspiDbus.GetStringAtOffsetMethod:
					{
						var offset = reader.ReadInt32();
						var (start, end) = AtspiTextSegmentation.GetStringAtOffset(text, offset, reader.ReadUInt32());
						ReplyTextRange(context, text, start, end);
						break;
					}
				case AtspiDbus.GetTextAtOffsetMethod:
				case AtspiDbus.GetTextBeforeOffsetMethod:
				case AtspiDbus.GetTextAfterOffsetMethod:
					{
						var offset = reader.ReadInt32();
						var boundary = reader.ReadUInt32();
						var (start, end) = member switch
						{
							AtspiDbus.GetTextBeforeOffsetMethod => AtspiTextSegmentation.GetTextBeforeOffset(text, offset, boundary),
							AtspiDbus.GetTextAfterOffsetMethod => AtspiTextSegmentation.GetTextAfterOffset(text, offset, boundary),
							_ => AtspiTextSegmentation.GetTextAtOffset(text, offset, boundary),
						};
						ReplyTextRange(context, text, start, end);
						break;
					}
				case AtspiDbus.SetCaretOffsetMethod:
					{
						var offset16 = Utf16Index(text, Math.Max(0, reader.ReadInt32()));
						ReplyBool(context, node.SelectionStart >= 0 && _server._writeTarget.SetTextSelection(node, offset16, offset16));
						break;
					}
				case AtspiDbus.GetNSelectionsMethod:
					ReplyInt32(context, HasTextSelection(node) ? 1 : 0);
					break;
				case AtspiDbus.GetSelectionMethod:
					{
						var index = reader.ReadInt32();
						using var writer = context.CreateReplyWriter(AtspiDbus.Int32PairSignature);
						var selected = index == 0 && HasTextSelection(node);
						writer.WriteInt32(selected ? CharacterCount(text, node.SelectionStart) : 0);
						writer.WriteInt32(selected ? CharacterCount(text, node.SelectionEnd) : 0);
						context.Reply(writer.CreateMessage());
						break;
					}
				case AtspiDbus.AddSelectionMethod:
					{
						var start16 = Utf16Index(text, Math.Max(0, reader.ReadInt32()));
						var end16 = Utf16Index(text, Math.Max(0, reader.ReadInt32()));
						ReplyBool(context, node.SelectionStart >= 0 && !HasTextSelection(node) && _server._writeTarget.SetTextSelection(node, start16, end16));
						break;
					}
				case AtspiDbus.SetSelectionMethod:
					{
						var index = reader.ReadInt32();
						var start16 = Utf16Index(text, Math.Max(0, reader.ReadInt32()));
						var end16 = Utf16Index(text, Math.Max(0, reader.ReadInt32()));
						ReplyBool(context, index == 0 && node.SelectionStart >= 0 && _server._writeTarget.SetTextSelection(node, start16, end16));
						break;
					}
				case AtspiDbus.RemoveSelectionMethod:
					{
						var index = reader.ReadInt32();
						ReplyBool(context, index == 0 && HasTextSelection(node) && _server._writeTarget.SetTextSelection(node, node.SelectionEnd, node.SelectionEnd));
						break;
					}
				case AtspiDbus.GetCharacterExtentsMethod:
				case AtspiDbus.GetRangeExtentsMethod:
					{
						// No per-glyph layout is exposed; report the whole text box so magnifiers
						// and mouse review still land on the right control.
						using var writer = context.CreateReplyWriter(AtspiDbus.Int32QuadSignature);
						writer.WriteInt32(ToInt32(node.X));
						writer.WriteInt32(ToInt32(node.Y));
						writer.WriteInt32(ToInt32(node.W));
						writer.WriteInt32(ToInt32(node.H));
						context.Reply(writer.CreateMessage());
						break;
					}
				case AtspiDbus.GetOffsetAtPointMethod:
					ReplyInt32(context, -1);
					break;
				case AtspiDbus.GetAttributesMethod:
				case AtspiDbus.GetAttributeRunMethod:
					{
						// No text attributes are exposed: one empty run spanning the whole text.
						using var writer = context.CreateReplyWriter(AtspiDbus.TextAttributeRunSignature);
						var dictionary = writer.WriteDictionaryStart();
						writer.WriteDictionaryEnd(dictionary);
						writer.WriteInt32(0);
						writer.WriteInt32(CharacterCount(text));
						context.Reply(writer.CreateMessage());
						break;
					}
				case AtspiDbus.GetDefaultAttributesMethod:
				case AtspiDbus.GetDefaultAttributeSetMethod:
					{
						using var writer = context.CreateReplyWriter(AtspiDbus.StringDictionarySignature);
						var dictionary = writer.WriteDictionaryStart();
						writer.WriteDictionaryEnd(dictionary);
						context.Reply(writer.CreateMessage());
						break;
					}
				case AtspiDbus.GetAttributeValueMethod:
					ReplyString(context, AtspiDbus.StringSignature, AtspiDbus.EmptyString);
					break;
				default:
					context.ReplyUnknownMethodError();
					break;
			}
		}

		private static bool HasTextSelection(AtspiNode node)
			=> node.SelectionStart >= 0 && node.SelectionEnd > node.SelectionStart;

		// Replies (s text, i startOffset, i endOffset) for a character range.
		private static void ReplyTextRange(MethodContext context, string text, int start, int end)
		{
			using var writer = context.CreateReplyWriter(AtspiDbus.TextRangeSignature);
			var start16 = Utf16Index(text, start);
			var end16 = Utf16Index(text, end);
			writer.WriteString(text.Substring(start16, end16 - start16));
			writer.WriteInt32(start);
			writer.WriteInt32(end);
			context.Reply(writer.CreateMessage());
		}

		private void EditableText(MethodContext context, AtspiNode node, string member)
		{
			if (!node.Editable)
			{
				ReplyBool(context, false);
				return;
			}

			var reader = context.Request.GetBodyReader();
			switch (member)
			{
				case AtspiDbus.SetTextContentsMethod:
					ReplyBool(context, _server._writeTarget.SetText(node, reader.ReadString()));
					break;
				case AtspiDbus.InsertTextMethod:
					{
						var pos = reader.ReadInt32();
						var s = reader.ReadString();
						var len = reader.ReadInt32();
						var current = node.Text;
						var pos16 = Utf16Index(current, Math.Max(0, pos));
						var insert = len >= 0 && len < CharacterCount(s) ? s.Substring(0, Utf16Index(s, len)) : s;
						ReplyBool(context, _server._writeTarget.SetText(node, current.Insert(pos16, insert)));
						break;
					}
				case AtspiDbus.DeleteTextMethod:
					{
						var start = reader.ReadInt32();
						var end = reader.ReadInt32();
						var current = node.Text;
						var start16 = Utf16Index(current, Math.Max(0, start));
						var end16 = Math.Max(start16, Utf16Index(current, Math.Max(0, end)));
						if (start16 >= end16)
						{
							ReplyBool(context, false);
							break;
						}
						ReplyBool(context, _server._writeTarget.SetText(node, current.Remove(start16, end16 - start16)));
						break;
					}
				case AtspiDbus.CopyTextMethod:
					ReplyVoid(context);
					break;
				case AtspiDbus.CutTextMethod:
				case AtspiDbus.PasteTextMethod:
					ReplyBool(context, false);
					break;
				default:
					context.ReplyUnknownMethodError();
					break;
			}
		}

		private void Selection(MethodContext context, AtspiNode node, string member)
		{
			switch (member)
			{
				case AtspiDbus.GetSelectedChildMethod:
					{
						var index = context.Request.GetBodyReader().ReadInt32();
						var selected = index >= 0 ? node.Children.FindAll(c => c.Selected) : null;
						ReplyReference(context, _server.GetReference(selected is not null && index < selected.Count ? selected[index] : null));
						break;
					}
				case AtspiDbus.SelectChildMethod:
					{
						var index = context.Request.GetBodyReader().ReadInt32();
						ReplyBool(context, _server._writeTarget.SelectChild(node, index));
						break;
					}
				case AtspiDbus.IsChildSelectedMethod:
					{
						var index = context.Request.GetBodyReader().ReadInt32();
						ReplyBool(context, index >= 0 && index < node.Children.Count && node.Children[index].Selected);
						break;
					}
				case AtspiDbus.GetNSelectedChildrenMethod:
					ReplyInt32(context, node.Children.Count(c => c.Selected));
					break;
				case AtspiDbus.DeselectSelectedChildMethod:
				case AtspiDbus.DeselectChildMethod:
				case AtspiDbus.SelectAllMethod:
				case AtspiDbus.ClearSelectionMethod:
					ReplyBool(context, false);
					break;
				default:
					context.ReplyUnknownMethodError();
					break;
			}
		}

		private static bool Actionable(AtspiNode node)
			=> node.HasToggle || node.Selectable || node.RoleName is "push button" or "combo box";

		private static string ActionName(AtspiNode node)
			=> node.Selectable ? "select" : node.RoleName switch
			{
				"push button" => "click",
				"check box" => "toggle",
				"radio button" => "toggle",
				"combo box" => "expand or collapse",
				_ => "activate",
			};

		private static bool HasSelectableChildren(AtspiNode node)
			=> node.Children.Exists(c => c.Selectable);

		private static void ReplyActions(MethodContext context, AtspiNode node)
		{
			using var writer = context.CreateReplyWriter(AtspiDbus.ActionsSignature);
			var array = writer.WriteArrayStart(DBusType.Struct);
			if (Actionable(node))
			{
				writer.WriteStructureStart();
				writer.WriteString(ActionName(node));
				writer.WriteString(AtspiDbus.EmptyString);
				writer.WriteString(AtspiDbus.EmptyString);
			}
			writer.WriteArrayEnd(array);
			context.Reply(writer.CreateMessage());
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
					{
						var reader = context.Request.GetBodyReader();
						var propertyInterface = reader.ReadString();
						var property = reader.ReadString();
						if (propertyInterface == AtspiDbus.ValueInterface &&
							property == AtspiDbus.CurrentValueProperty &&
							node.HasRange)
						{
							_server._writeTarget.SetRangeValue(node, reader.ReadVariantValue().GetDouble());
							ReplyVoid(context);
						}
						else if (Array.IndexOf(PropertiesOf(node, propertyInterface), property) >= 0)
						{
							context.ReplyError(AtspiDbus.PropertyReadOnlyError, $"Property '{propertyInterface}.{property}' is read-only.");
						}
						else
						{
							context.ReplyError(AtspiDbus.UnknownPropertyError, $"Unknown property '{propertyInterface}.{property}'.");
						}
						break;
					}
				case AtspiDbus.GetAllPropertiesMethod:
					{
						var propertyInterface = context.Request.GetBodyReader().ReadString();
						ReplyAllProperties(context, node, propertyInterface);
						break;
					}
				default:
					context.ReplyUnknownMethodError();
					break;
			}
		}

		private void ReplyVariant(MethodContext context, AtspiNode node, string propertyInterface, string property)
		{
			// A wrongly-typed placeholder makes libatspi fail the read ("expected i, got s").
			if (Array.IndexOf(PropertiesOf(node, propertyInterface), property) < 0)
			{
				context.ReplyError(AtspiDbus.UnknownPropertyError, $"Unknown property '{propertyInterface}.{property}'.");
				return;
			}

			var writer = context.CreateReplyWriter(AtspiDbus.VariantSignature);
			try
			{
				WritePropertyValue(ref writer, node, propertyInterface, property);
				context.Reply(writer.CreateMessage());
			}
			finally
			{
				writer.Dispose();
			}
		}

		private static readonly string[] _accessibleProperties =
			[AtspiDbus.NameProperty, AtspiDbus.DescriptionProperty, AtspiDbus.AccessibleIdProperty, AtspiDbus.ChildCountProperty, AtspiDbus.ParentProperty, AtspiDbus.LocaleProperty, AtspiDbus.HelpTextProperty];
		private static readonly string[] _applicationProperties =
			[AtspiDbus.ToolkitNameProperty, AtspiDbus.VersionProperty, AtspiDbus.ToolkitVersionProperty, AtspiDbus.AtspiVersionProperty];
		private static readonly string[] _valueProperties =
			[AtspiDbus.CurrentValueProperty, AtspiDbus.MinimumValueProperty, AtspiDbus.MaximumValueProperty, AtspiDbus.MinimumIncrementProperty];
		private static readonly string[] _textProperties =
			[AtspiDbus.CharacterCountProperty, AtspiDbus.CaretOffsetProperty];
		private static readonly string[] _actionProperties = [AtspiDbus.NActionsProperty];

		// The properties each interface serves for this node; drives Get, Set and GetAll alike.
		private static string[] PropertiesOf(AtspiNode node, string propertyInterface) => propertyInterface switch
		{
			AtspiDbus.AccessibleInterface => _accessibleProperties,
			AtspiDbus.ApplicationInterface when node.Parent is null => _applicationProperties,
			AtspiDbus.ValueInterface when node.HasRange => _valueProperties,
			AtspiDbus.TextInterface when node.HasText => _textProperties,
			AtspiDbus.ActionInterface when Actionable(node) => _actionProperties,
			_ => Array.Empty<string>(),
		};

		private void ReplyAllProperties(MethodContext context, AtspiNode node, string propertyInterface)
		{
			var properties = PropertiesOf(node, propertyInterface);

			var writer = context.CreateReplyWriter(AtspiDbus.VariantDictionarySignature);
			try
			{
				var dictionary = writer.WriteDictionaryStart();
				foreach (var property in properties)
				{
					writer.WriteDictionaryEntryStart();
					writer.WriteString(property);
					WritePropertyValue(ref writer, node, propertyInterface, property);
				}
				writer.WriteDictionaryEnd(dictionary);
				context.Reply(writer.CreateMessage());
			}
			finally
			{
				writer.Dispose();
			}
		}

		private void WritePropertyValue(ref MessageWriter writer, AtspiNode node, string propertyInterface, string property)
		{
			switch (propertyInterface, property)
			{
				case (AtspiDbus.AccessibleInterface, AtspiDbus.NameProperty):
					writer.WriteVariantString(node.Name);
					break;
				case (AtspiDbus.AccessibleInterface, AtspiDbus.DescriptionProperty):
					writer.WriteVariantString(node.Description ?? AtspiDbus.EmptyString);
					break;
				case (AtspiDbus.AccessibleInterface, AtspiDbus.AccessibleIdProperty):
					writer.WriteVariantString(AtspiDbus.EmptyString);
					break;
				case (AtspiDbus.AccessibleInterface, AtspiDbus.ChildCountProperty):
					writer.WriteVariantInt32(node.Children.Count);
					break;
				case (AtspiDbus.AccessibleInterface, AtspiDbus.ParentProperty):
					writer.WriteSignature(AtspiDbus.ReferenceSignature);
					var parentRef = _server.GetParentReference(node);
					writer.WriteStructureStart();
					writer.WriteString(parentRef.Service);
					writer.WriteObjectPath(parentRef.Path);
					break;
				case (AtspiDbus.AccessibleInterface, AtspiDbus.LocaleProperty):
					writer.WriteVariantString(AtspiDbus.DefaultLocale);
					break;
				case (AtspiDbus.AccessibleInterface, AtspiDbus.HelpTextProperty):
					writer.WriteVariantString(AtspiDbus.EmptyString);
					break;
				case (AtspiDbus.ApplicationInterface, AtspiDbus.ToolkitNameProperty):
					writer.WriteVariantString(AtspiDbus.ToolkitName);
					break;
				case (AtspiDbus.ApplicationInterface, AtspiDbus.VersionProperty):
				case (AtspiDbus.ApplicationInterface, AtspiDbus.ToolkitVersionProperty):
					writer.WriteVariantString(AtspiDbus.ToolkitVersion);
					break;
				case (AtspiDbus.ApplicationInterface, AtspiDbus.AtspiVersionProperty):
					writer.WriteVariantString(AtspiDbus.AtspiVersion);
					break;
				case (AtspiDbus.ValueInterface, AtspiDbus.CurrentValueProperty) when node.HasRange:
					writer.WriteVariantDouble(node.Val);
					break;
				case (AtspiDbus.ValueInterface, AtspiDbus.MinimumValueProperty) when node.HasRange:
					writer.WriteVariantDouble(node.Min);
					break;
				case (AtspiDbus.ValueInterface, AtspiDbus.MaximumValueProperty) when node.HasRange:
					writer.WriteVariantDouble(node.Max);
					break;
				case (AtspiDbus.ValueInterface, AtspiDbus.MinimumIncrementProperty) when node.HasRange:
					writer.WriteVariantDouble(0);
					break;
				case (AtspiDbus.TextInterface, AtspiDbus.CharacterCountProperty) when node.HasText:
					writer.WriteVariantInt32(CharacterCount(node.Text));
					break;
				case (AtspiDbus.TextInterface, AtspiDbus.CaretOffsetProperty) when node.HasText:
					// AT-SPI reports -1 for text without a caret (e.g. a read-only label).
					writer.WriteVariantInt32(node.SelectionEnd >= 0 ? CharacterCount(node.Text, node.SelectionEnd) : -1);
					break;
				case (AtspiDbus.ActionInterface, AtspiDbus.NActionsProperty):
					// libatspi reads NActions as a property (int), not only via GetNActions.
					writer.WriteVariantInt32(Actionable(node) ? 1 : 0);
					break;
				default:
					writer.WriteVariantString(AtspiDbus.EmptyString);
					break;
			}
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
				var childRef = _server.GetReference(child);
				writer.WriteStructureStart();
				writer.WriteString(childRef.Service);
				writer.WriteObjectPath(childRef.Path);
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

		private static int Utf16Index(string s, int characterOffset) => AtspiTextSegmentation.Utf16Index(s, characterOffset);

		private static int CharacterCount(string s) => AtspiTextSegmentation.CharacterCount(s, s.Length);

		private static int CharacterCount(string s, int utf16Length) => AtspiTextSegmentation.CharacterCount(s, utf16Length);

		private static void ReplyPosition(MethodContext context, AtspiNode node)
		{
			// Component.xml declares GetPosition as two separate out arguments (x, y) —
			// only GetExtents replies with a struct.
			using var writer = context.CreateReplyWriter(AtspiDbus.Int32PairSignature);
			writer.WriteInt32(ToInt32(node.X));
			writer.WriteInt32(ToInt32(node.Y));
			context.Reply(writer.CreateMessage());
		}

		private static void ReplySize(MethodContext context, AtspiNode node)
		{
			using var writer = context.CreateReplyWriter(AtspiDbus.Int32PairSignature);
			writer.WriteInt32(ToInt32(node.W));
			writer.WriteInt32(ToInt32(node.H));
			context.Reply(writer.CreateMessage());
		}

		private static void ReplyContains(MethodContext context, AtspiNode node)
		{
			var reader = context.Request.GetBodyReader();
			var x = reader.ReadInt32();
			var y = reader.ReadInt32();
			var coordType = reader.ReadUInt32();
			(x, y) = TranslateToScreen(node, x, y, coordType);
			ReplyBool(context, Contains(node, x, y));
		}

		private void ReplyAccessibleAtPoint(MethodContext context, AtspiNode node)
		{
			var reader = context.Request.GetBodyReader();
			var x = reader.ReadInt32();
			var y = reader.ReadInt32();
			var coordType = reader.ReadUInt32();
			(x, y) = TranslateToScreen(node, x, y, coordType);
			ReplyReference(context, _server.GetReference(FindAccessibleAtPoint(node, x, y)));
		}

		// Published bounds are absolute screen coordinates, so window- and
		// parent-relative input points shift by that ancestor's own position.
		private static (int X, int Y) TranslateToScreen(AtspiNode node, int x, int y, uint coordType)
		{
			switch (coordType)
			{
				case AtspiDbus.WindowCoordType:
					var root = node;
					while (root.Parent is { } rootParent)
					{
						root = rootParent;
					}
					return (x + (int)root.X, y + (int)root.Y);
				case AtspiDbus.ParentCoordType:
					var origin = node.Parent ?? node;
					return (x + (int)origin.X, y + (int)origin.Y);
				default:
					return (x, y);
			}
		}

		private void ReplyStates(MethodContext context, AtspiNode node)
		{
			uint state0 = 0;
			void SetState(int bit) => state0 |= 1u << bit;

			if (node.Enabled)
			{
				SetState(AtspiDbus.EnabledState);
				SetState(AtspiDbus.SensitiveState);
			}

			if (!node.Offscreen)
			{
				SetState(AtspiDbus.ShowingState);
				SetState(AtspiDbus.VisibleState);
			}

			if (node.Focusable)
			{
				SetState(AtspiDbus.FocusableState);
			}

			if (node.Checked)
			{
				SetState(AtspiDbus.CheckedState);
			}

			if (node.Editable)
			{
				SetState(AtspiDbus.EditableState);
			}

			if (node.Expandable)
			{
				SetState(AtspiDbus.ExpandableState);
				if (node.Expanded)
				{
					SetState(AtspiDbus.ExpandedState);
				}
			}

			if (node.Selectable)
			{
				SetState(AtspiDbus.SelectableState);
				if (node.Selected)
				{
					SetState(AtspiDbus.SelectedState);
				}
			}

			if (ReferenceEquals(_server._focusedNode, node))
			{
				SetState(AtspiDbus.FocusedState);
			}

			if (node.Active)
			{
				SetState(AtspiDbus.ActiveState);
			}

			if (node.Modal)
			{
				SetState(AtspiDbus.ModalState);
			}

			if (node.SelectionStart >= 0)
			{
				SetState(node.MultiLine ? AtspiDbus.MultiLineState : AtspiDbus.SingleLineState);
			}

			// The AT-SPI state set is two 32-bit words; states with index >= 32 live in
			// the second word.
			uint state1 = 0;
			void SetState1(int bit) => state1 |= 1u << (bit - 32);

			if (node.Required)
			{
				SetState1(AtspiDbus.RequiredState);
			}

			if (node.ReadOnly)
			{
				SetState1(AtspiDbus.ReadOnlyState);
			}

			if (node.SelectionStart >= 0)
			{
				SetState1(AtspiDbus.SelectableTextState);
			}

			if (node.HasToggle)
			{
				SetState1(AtspiDbus.CheckableState);
			}

			if (node.Indeterminate)
			{
				SetState1(AtspiDbus.IndeterminateState);
			}

			using var writer = context.CreateReplyWriter(AtspiDbus.UInt32ArraySignature);
			var array = writer.WriteArrayStart(DBusType.UInt32);
			writer.WriteUInt32(state0);
			writer.WriteUInt32(state1);
			writer.WriteArrayEnd(array);
			context.Reply(writer.CreateMessage());
		}

		// Single source of truth for a node's AT-SPI interfaces, so GetInterfaces and
		// Introspect can never disagree.
		private static List<string> InterfaceNames(AtspiNode node)
		{
			var names = new List<string> { AtspiDbus.AccessibleInterface, AtspiDbus.ComponentInterface };
			if (node.Parent is null)
			{
				names.Add(AtspiDbus.ApplicationInterface);
			}
			if (Actionable(node))
			{
				names.Add(AtspiDbus.ActionInterface);
			}
			if (node.HasRange)
			{
				names.Add(AtspiDbus.ValueInterface);
			}
			if (node.HasText)
			{
				names.Add(AtspiDbus.TextInterface);
			}
			if (node.Editable)
			{
				names.Add(AtspiDbus.EditableTextInterface);
			}
			if (HasSelectableChildren(node))
			{
				names.Add(AtspiDbus.SelectionInterface);
			}
			return names;
		}

		private static void ReplyInterfaces(MethodContext context, AtspiNode node)
		{
			using var writer = context.CreateReplyWriter(AtspiDbus.StringArraySignature);
			var array = writer.WriteArrayStart(DBusType.String);
			foreach (var name in InterfaceNames(node))
			{
				writer.WriteString(name);
			}
			writer.WriteArrayEnd(array);
			context.Reply(writer.CreateMessage());
		}

		private static void ReplyIntrospection(MethodContext context, AtspiNode node)
		{
			var xml = new StringBuilder("<node>");
			foreach (var name in InterfaceNames(node))
			{
				xml.Append("<interface name=\"").Append(name).Append("\"/>");
			}
			xml.Append("<interface name=\"").Append(AtspiDbus.PropertiesInterface).Append("\"/>");
			xml.Append("<interface name=\"").Append(AtspiDbus.IntrospectableInterface).Append("\"/>");
			xml.Append("</node>");
			ReplyString(context, AtspiDbus.StringSignature, xml.ToString());
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
			writer.WriteStructureStart();
			writer.WriteString(reference.Service);
			writer.WriteObjectPath(reference.Path);
			context.Reply(writer.CreateMessage());
		}

		private static void ReplyAttributes(MethodContext context, AtspiNode node)
		{
			var attributes = new List<(string Key, string Value)>();
			if (node.PositionInSet > 0)
			{
				attributes.Add(("posinset", node.PositionInSet.ToString(CultureInfo.InvariantCulture)));
				attributes.Add(("setsize", node.SizeOfSet.ToString(CultureInfo.InvariantCulture)));
			}
			if (node.HeadingLevel > 0)
			{
				attributes.Add(("level", node.HeadingLevel.ToString(CultureInfo.InvariantCulture)));
			}
			if (!string.IsNullOrEmpty(node.Landmark))
			{
				attributes.Add(("xml-roles", node.Landmark));
			}
			if (!string.IsNullOrEmpty(node.Placeholder))
			{
				attributes.Add(("placeholder-text", node.Placeholder));
			}

			using var writer = context.CreateReplyWriter(AtspiDbus.StringDictionarySignature);
			var dictionary = writer.WriteDictionaryStart();
			foreach (var (key, value) in attributes)
			{
				writer.WriteDictionaryEntryStart();
				writer.WriteString(key);
				writer.WriteString(value);
			}
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
		public const string RootObjectPath = "/org/a11y/atspi";
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
		public const string ActionInterface = "org.a11y.atspi.Action";
		public const string ValueInterface = "org.a11y.atspi.Value";
		public const string TextInterface = "org.a11y.atspi.Text";
		public const string EditableTextInterface = "org.a11y.atspi.EditableText";
		public const string SelectionInterface = "org.a11y.atspi.Selection";
		public const string EventObjectInterface = "org.a11y.atspi.Event.Object";
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
		public const uint WindowCoordType = 1;
		public const uint ParentCoordType = 2;
		public const string GetAccessibleAtPointMethod = "GetAccessibleAtPoint";
		public const string GrabFocusMethod = "GrabFocus";
		public const string GetNActionsMethod = "GetNActions";
		public const string GetActionNameMethod = "GetName";
		public const string GetLocalizedActionNameMethod = "GetLocalizedName";
		public const string GetActionDescriptionMethod = "GetDescription";
		public const string GetActionKeyBindingMethod = "GetKeyBinding";
		public const string GetActionsMethod = "GetActions";
		public const string DoActionMethod = "DoAction";
		public const string GetTextMethod = "GetText";
		public const string GetCharacterAtOffsetMethod = "GetCharacterAtOffset";
		public const string GetStringAtOffsetMethod = "GetStringAtOffset";
		public const string GetTextAtOffsetMethod = "GetTextAtOffset";
		public const string GetTextBeforeOffsetMethod = "GetTextBeforeOffset";
		public const string GetTextAfterOffsetMethod = "GetTextAfterOffset";
		public const string SetCaretOffsetMethod = "SetCaretOffset";
		public const string GetNSelectionsMethod = "GetNSelections";
		public const string GetSelectionMethod = "GetSelection";
		public const string AddSelectionMethod = "AddSelection";
		public const string RemoveSelectionMethod = "RemoveSelection";
		public const string SetSelectionMethod = "SetSelection";
		public const string GetCharacterExtentsMethod = "GetCharacterExtents";
		public const string GetRangeExtentsMethod = "GetRangeExtents";
		public const string GetOffsetAtPointMethod = "GetOffsetAtPoint";
		public const string GetAttributeRunMethod = "GetAttributeRun";
		public const string GetDefaultAttributesMethod = "GetDefaultAttributes";
		public const string GetDefaultAttributeSetMethod = "GetDefaultAttributeSet";
		public const string GetAttributeValueMethod = "GetAttributeValue";
		public const string SetTextContentsMethod = "SetTextContents";
		public const string InsertTextMethod = "InsertText";
		public const string DeleteTextMethod = "DeleteText";
		public const string CopyTextMethod = "CopyText";
		public const string CutTextMethod = "CutText";
		public const string PasteTextMethod = "PasteText";
		public const string GetSelectedChildMethod = "GetSelectedChild";
		public const string SelectChildMethod = "SelectChild";
		public const string IsChildSelectedMethod = "IsChildSelected";
		public const string GetNSelectedChildrenMethod = "GetNSelectedChildren";
		public const string DeselectSelectedChildMethod = "DeselectSelectedChild";
		public const string DeselectChildMethod = "DeselectChild";
		public const string SelectAllMethod = "SelectAll";
		public const string ClearSelectionMethod = "ClearSelection";
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
		public const string CurrentValueProperty = "CurrentValue";
		public const string MinimumValueProperty = "MinimumValue";
		public const string MaximumValueProperty = "MaximumValue";
		public const string MinimumIncrementProperty = "MinimumIncrement";
		public const string CharacterCountProperty = "CharacterCount";
		public const string CaretOffsetProperty = "CaretOffset";
		public const string HelpTextProperty = "HelpText";
		public const string ToolkitVersionProperty = "ToolkitVersion";
		public const string UnknownPropertyError = "org.freedesktop.DBus.Error.UnknownProperty";
		public const string PropertyReadOnlyError = "org.freedesktop.DBus.Error.PropertyReadOnly";
		public const string NActionsProperty = "NActions";
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
		public const string Int32PairSignature = "ii";
		public const string UInt32ArraySignature = "au";
		public const string StringArraySignature = "as";
		public const string StringDictionarySignature = "a{ss}";
		public const string VariantDictionarySignature = "a{sv}";
		public const string RelationSetSignature = "a(ua(so))";
		public const string ActionsSignature = "a(sss)";
		public const string Int32QuadSignature = "iiii";
		public const string TextRangeSignature = "sii";
		public const string TextAttributeRunSignature = "a{ss}ii";
		public const string EventSignature = "siiva{sv}";
		public const string EventWindowInterface = "org.a11y.atspi.Event.Window";
		public const string StateChangedMember = "StateChanged";
		public const string PropertyChangeMember = "PropertyChange";
		public const string SelectionChangedMember = "SelectionChanged";
		public const string ChildrenChangedMember = "ChildrenChanged";
		public const string TextChangedMember = "TextChanged";
		public const string TextCaretMovedMember = "TextCaretMoved";
		public const string TextSelectionChangedMember = "TextSelectionChanged";
		public const string AnnouncementMember = "Announcement";
		public const string ActivateMember = "Activate";
		public const string DeactivateMember = "Deactivate";
		public const string ApplicationRoleName = "application";
		public const uint WidgetLayer = 3; // ATSPI_LAYER_WIDGET
		public const uint ApplicationRole = 75;
		public const int EnabledState = 8;
		public const int FocusableState = 11;
		public const int SensitiveState = 24;
		public const int ShowingState = 25;
		public const int VisibleState = 30;
		public const int CheckedState = 4;
		public const int EditableState = 7;
		public const int ExpandableState = 9;
		public const int ExpandedState = 10;
		public const int FocusedState = 12;
		public const int SelectableState = 22;
		public const int SelectedState = 23;
		public const int ActiveState = 1;
		public const int ModalState = 16;
		public const int MultiLineState = 17;
		public const int SingleLineState = 26;
		public const int IndeterminateState = 32;
		public const int RequiredState = 33;
		public const int SelectableTextState = 38;
		public const int CheckableState = 41;
		public const int ReadOnlyState = 43;
	}
}
