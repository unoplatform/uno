#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Skia;
using CoreWindowActivationState = Windows.UI.Core.CoreWindowActivationState;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// Publishes the Skia-rendered X11 visual tree to the AT-SPI2 accessibility bus
/// (<c>org.a11y.atspi</c>) as a tree of <see cref="AtspiNode"/> objects served by an
/// <see cref="AtspiServer"/>: an application root holding one frame for the window.
/// Mirrors <c>MacOSAccessibility</c>, but the bridge only runs while the desktop reports
/// accessibility as enabled (<see cref="AtspiEnablement"/>).
/// </summary>
internal sealed class X11Accessibility : SkiaAccessibilityBase, AtspiServer.IWriteTarget
{
	private const string RootPath = "/org/a11y/atspi/accessible/root";
	private const string NodePathPrefix = "/org/a11y/atspi/accessible/";

	private readonly X11XamlRootHost _host;
	private readonly Window _window;

	// Guards _server assignment (on the D-Bus connect continuation thread) against
	// StopServer/DisposeCore (on the UI thread), so a server that starts as the window
	// closes or accessibility is switched off is never left running.
	private readonly object _serverGate = new();
	private AtspiServer? _server;
	private volatile bool _serverStarting;
	private bool _initialized;
	private AtspiNode? _root;
	private AtspiNode? _frame;
	private bool _isActive;
	private readonly Dictionary<nint, AtspiNode> _nodesByHandle = new();
	private readonly Dictionary<nint, UIElement> _elementsByHandle = new();
	// Immutable snapshot published for the D-Bus reader thread (write-path lookups).
	// The mutable _elementsByHandle above is only touched on the UI thread during a build.
	private volatile IReadOnlyDictionary<nint, UIElement> _elementsSnapshot = new Dictionary<nint, UIElement>();
	private AtspiNode? _focusedNode;
	private TextBox? _focusedTextBox;
	private bool _caretUpdateQueued;
	private int _announcedCaret;
	private bool _announcedSelection;
	private bool _treeInitialized;
	private bool _treeBuildQueued;
	private int _nextPath = 1;
	private double _scale = 1.0;

	// AT-SPI clients cache object references across tree changes; a rebuild must
	// republish the same element at the same path or a cached reference silently
	// resolves to a different control (including routing writes to the wrong one).
	// Keyed by element identity, populated lazily, never cleared on rebuild.
	private readonly Dictionary<nint, string> _pathsByHandle = new();
	private readonly Dictionary<(nint Combo, int Index), string> _itemPathsByComboIndex = new();
	// Screen position of the window's client area, in physical pixels; added to each
	// node's client-space bounds so AT-SPI extents are absolute screen coordinates.
	private double _originX;
	private double _originY;

	internal X11Accessibility(X11XamlRootHost host, Window window)
	{
		_host = host;
		_window = window;
	}

	public override bool IsAccessibilityEnabled => !IsDisposed && _server is not null;

	/// <summary>
	/// Starts following the desktop accessibility switch; the AT-SPI server runs (and
	/// the tree is published) only while it is on. Called by the host after the native
	/// X11 window exists.
	/// </summary>
	internal void Initialize()
	{
		if (_initialized || IsDisposed)
		{
			return;
		}
		_initialized = true;

		_isActive = _window.NativeWrapper?.ActivationState is { } state && state != CoreWindowActivationState.Deactivated;
		_window.Activated += OnWindowActivated;
		_window.AppWindow.Changed += OnAppWindowChanged;
		AtspiEnablement.Changed += OnEnablementChanged;

		_ = InitializeEnablementAsync();
	}

	private async Task InitializeEnablementAsync()
	{
		await AtspiEnablement.EnsureInitializedAsync();
		X11XamlRootHost.QueueAction(_host, UpdateServerState);
	}

	private void OnEnablementChanged(object? sender, EventArgs e)
		=> X11XamlRootHost.QueueAction(_host, UpdateServerState);

	private void UpdateServerState()
	{
		if (IsDisposed)
		{
			return;
		}

		if (AtspiEnablement.IsEnabled)
		{
			if (_server is null && !_serverStarting)
			{
				StartServerSafely();
			}
		}
		else if (_server is not null)
		{
			StopServer();
		}
	}

	private async void StartServerSafely()
	{
		_serverStarting = true;
		try
		{
			var server = await AtspiServer.TryStartAsync(ResolveApplicationName(), this);
			if (server is null)
			{
				return;
			}

			lock (_serverGate)
			{
				if (IsDisposed || !AtspiEnablement.IsEnabled)
				{
					// The window closed or accessibility was switched off while the
					// connection was being established; don't leak the D-Bus connection.
					StopServerSafely(server);
					return;
				}

				_server = server;
			}

			X11XamlRootHost.QueueAction(_host, () =>
			{
				if (IsDisposed || _server is null)
				{
					return;
				}

				if (_window.RootElement is { } rootElement)
				{
					QueueTreeBuild(rootElement);
				}
			});
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"[A11y] Failed to start AT-SPI server: {ex.Message}", ex);
			}
		}
		finally
		{
			_serverStarting = false;
		}
	}

	private void StopServer()
	{
		AtspiServer? server;
		lock (_serverGate)
		{
			server = _server;
			_server = null;
		}

		TrackFocusedTextBox(null);
		_root = null;
		_frame = null;
		_nodesByHandle.Clear();
		_elementsByHandle.Clear();
		_pathsByHandle.Clear();
		_itemPathsByComboIndex.Clear();
		_elementsSnapshot = new Dictionary<nint, UIElement>();
		_focusedNode = null;
		_treeInitialized = false;

		if (server is not null)
		{
			StopServerSafely(server);
		}
	}

	private static string ResolveApplicationName()
	{
		try
		{
			var displayName = Windows.ApplicationModel.Package.Current.DisplayName;
			if (!string.IsNullOrEmpty(displayName))
			{
				return displayName;
			}
		}
		catch (Exception)
		{
			// Package.Current throws outside of a packaged (MSIX) deployment.
		}

		return Process.GetCurrentProcess().ProcessName;
	}

	private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
	{
		_isActive = args.WindowActivationState != WindowActivationState.Deactivated;

		if (IsDisposed || _server is not { } server)
		{
			return;
		}

		if (!_treeInitialized)
		{
			if (_isActive && _window.RootElement is { } rootElement)
			{
				QueueTreeBuild(rootElement);
			}
			return;
		}

		if (_frame is { } frame && frame.Active != _isActive)
		{
			frame.Active = _isActive;
			server.EmitStateChanged(frame, "active", _isActive ? 1 : 0);
			server.EmitWindowActivation(frame, _isActive);
		}
	}

	private void QueueTreeBuild(UIElement rootElement)
	{
		if (_treeBuildQueued)
		{
			return;
		}
		_treeBuildQueued = true;

		X11XamlRootHost.QueueAction(_host, () =>
		{
			_treeBuildQueued = false;
			if (IsDisposed || _server is null)
			{
				return;
			}

			BuildTree(rootElement);
		});
	}

	private void BuildTree(UIElement rootElement)
	{
		try
		{
			var firstBuild = !_treeInitialized;
			_nodesByHandle.Clear();
			_elementsByHandle.Clear();
			(_originX, _originY) = GetClientOrigin();
			_scale = rootElement.XamlRoot?.RasterizationScale ?? 1.0;
			_root = BuildRootNode(rootElement);
			_treeInitialized = true;
			// Publish an immutable element snapshot before the tree so the reader-thread
			// write path never observes the dictionary mid-rebuild.
			_elementsSnapshot = new Dictionary<nint, UIElement>(_elementsByHandle);
			_server!.SetRoot(_root);

			// A rebuild replaces every node instance; re-point focus at the fresh node
			// for the same handle so the focused state survives structure changes.
			if (_focusedNode is { } prevFocus && _nodesByHandle.TryGetValue(prevFocus.Handle, out var refocused))
			{
				_focusedNode = refocused;
			}
			else
			{
				_focusedNode = null;
			}
			_server.SetFocus(_focusedNode);

			// A screen reader started after the window was activated still needs to
			// learn which window is active.
			if (firstBuild && _frame is { Active: true } frame)
			{
				_server.EmitWindowActivation(frame, true);
			}

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"[A11y] AT-SPI tree published with {_nodesByHandle.Count} node(s).");
			}
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"[A11y] Failed to build AT-SPI tree: {ex.Message}", ex);
			}
		}
	}

	private AtspiNode BuildRootNode(UIElement rootElement)
	{
		var application = new AtspiNode
		{
			Path = RootPath,
			Role = AtspiRoleMap.ApplicationRoleId,
			RoleName = AtspiRoleMap.ApplicationRoleName,
			Name = ResolveApplicationName(),
		};

		// The window is a frame under the application, as with native toolkits; screen
		// readers locate the active window through its "active" state and window events.
		TrySubscribeScrollSource(rootElement);
		var frame = BuildNode(rootElement, application, GetOrCreatePath(rootElement.Visual.Handle));
		frame.Role = AtspiRoleMap.FrameRoleId;
		frame.RoleName = AtspiRoleMap.FrameRoleName;
		frame.Name = string.IsNullOrEmpty(_window.Title) ? application.Name : _window.Title;
		frame.Active = _isActive;
		application.Children.Add(frame);
		_frame = frame;
		ApplyFrameBounds(application);

		foreach (var child in rootElement.GetChildren())
		{
			BuildNodeRecursive(frame, child);
		}
		return application;
	}

	private void BuildNodeRecursive(AtspiNode parent, UIElement child)
	{
		TrySubscribeScrollSource(child);

		// Raw elements, and the PopupPanel wrapping popup content (announced only as a
		// generic "Popup"), are skipped: their children attach to the nearest published parent.
		var accessibilityView = AutomationProperties.GetAccessibilityView(child);
		if (accessibilityView == AccessibilityView.Raw || child is Microsoft.UI.Xaml.Controls.Primitives.PopupPanel)
		{
			foreach (var childChild in child.GetChildren())
			{
				BuildNodeRecursive(parent, childChild);
			}
			return;
		}

		var node = BuildNode(child, parent, GetOrCreatePath(child.Visual.Handle));
		parent.Children.Add(node);

		// ComboBox items live in a popup outside the visual tree; BuildNode surfaces
		// them as selectable child nodes, so do not descend into the combo's content
		// presenter (which shows a copy of the selected item).
		if (child is ComboBox)
		{
			return;
		}

		foreach (var childChild in child.GetChildren())
		{
			BuildNodeRecursive(node, childChild);
		}
	}

	private AtspiNode BuildNode(UIElement element, AtspiNode? parent, string path)
	{
		var peer = element.GetOrCreateAutomationPeer();
		var (role, roleName) = peer is not null
			? AtspiRoleMap.GetRole(peer.GetAutomationControlType())
			: (39u, "panel");

		var node = new AtspiNode
		{
			Path = path,
			Handle = element.Visual.Handle,
			Role = role,
			RoleName = roleName,
			Name = ResolveName(peer),
			Parent = parent,
			Enabled = peer?.IsEnabled() ?? true,
			Focusable = peer?.IsKeyboardFocusable() ?? false,
			ItemIndex = parent?.Children.Count ?? -1,
		};
		ApplyBounds(node, element);

		if (peer is not null)
		{
			PopulateNodeFromPeer(node, peer);

			// Like GTK labels, static text implements Text, so screen readers can review it
			// by word and character instead of as one opaque name.
			if (!node.HasText && peer.GetAutomationControlType() == AutomationControlType.Text)
			{
				node.HasText = true;
				node.IsStaticText = true;
				node.Text = node.Name;
			}
		}

		// ContentDialog has no peer of its own; as a nameless panel Orca skips it, so moving
		// into it would never say which dialog opened.
		if (element is ContentDialog dialog)
		{
			(node.Role, node.RoleName) = (AtspiRoleMap.DialogRoleId, AtspiRoleMap.DialogRoleName);
			node.Modal = true;
			if (string.IsNullOrEmpty(node.Name) && dialog.Title is { } title)
			{
				node.Name = FrameworkElement.GetStringFromObject(title) ?? string.Empty;
			}
		}

		if (element is TextBox textBox)
		{
			node.SelectionStart = textBox.SelectionStart;
			node.SelectionEnd = textBox.SelectionStart + textBox.SelectionLength;
			node.MultiLine = textBox.AcceptsReturn;
			ApplyPlaceholder(node, textBox);
		}

		_nodesByHandle[node.Handle] = node;
		_elementsByHandle[node.Handle] = element;

		if (element is ComboBox comboBox)
		{
			PopulateComboBoxItems(node, comboBox);
		}

		return node;
	}

	// The TextBox peer reports PlaceholderText as help text (Narrator's hint). AT-SPI has a
	// dedicated placeholder-text attribute; as a description it would be read as a permanent
	// hint on every focus, even after the user has typed.
	private static void ApplyPlaceholder(AtspiNode node, TextBox textBox)
	{
		if (string.IsNullOrEmpty(textBox.PlaceholderText))
		{
			return;
		}

		node.Placeholder = textBox.PlaceholderText;
		if (node.Description == textBox.PlaceholderText)
		{
			node.Description = null;
		}
	}

	// Bounds come from the element's global transform (layout offsets, render transforms
	// and scroll offsets) like Win32's BoundingRectangle; "showing" follows the clipped
	// bounds, so content scrolled out of its viewport is reported as not showing.
	private void ApplyBounds(AtspiNode node, UIElement element)
	{
		var bounds = element.GetGlobalBoundsWithOptions(ignoreClipping: true, ignoreClippingOnScrollContentPresenters: true, useTargetInformation: false);
		if (bounds.IsEmpty)
		{
			(node.X, node.Y, node.W, node.H) = (_originX, _originY, 0, 0);
			node.Offscreen = true;
			return;
		}

		node.X = bounds.X * _scale + _originX;
		node.Y = bounds.Y * _scale + _originY;
		node.W = bounds.Width * _scale;
		node.H = bounds.Height * _scale;

		var visibleBounds = element.GetGlobalBoundsWithOptions(ignoreClipping: false, ignoreClippingOnScrollContentPresenters: false, useTargetInformation: false);
		node.Offscreen = !element.Visual.IsVisible || visibleBounds.IsEmpty || visibleBounds.Width <= 0 || visibleBounds.Height <= 0;
	}

	// The window's root element has no layout bounds of its own; the frame (and the
	// application, which has no geometry of its own) cover the client area.
	private void ApplyFrameBounds(AtspiNode? application)
	{
		if (_frame is not { } frame)
		{
			return;
		}

		var size = _window.Bounds;
		(frame.X, frame.Y, frame.W, frame.H) = (_originX, _originY, size.Width * _scale, size.Height * _scale);
		frame.Offscreen = false;
		if (application is not null)
		{
			(application.X, application.Y, application.W, application.H) = (frame.X, frame.Y, frame.W, frame.H);
		}
	}

	// The client area's screen position. AppWindow.Position is the outer (decorated)
	// window, which is offset from the client area by the window manager's frame.
	private (double X, double Y) GetClientOrigin()
	{
		try
		{
			var window = _host.RootX11Window;
			using (X11Helper.XLock(window.Display))
			{
				return XLib.XTranslateCoordinates(window.Display, window.Window, XLib.XDefaultRootWindow(window.Display), 0, 0, out var x, out var y, out _)
					? (x, y)
					: (0, 0);
			}
		}
		catch (Exception)
		{
			// The native window can be unavailable while closing; fall back to
			// window-relative coordinates.
			return (0, 0);
		}
	}

	private static void PopulateNodeFromPeer(AtspiNode node, AutomationPeer peer)
	{
		var attributes = AriaMapper.GetAriaAttributes(peer);

		node.Description = attributes.Description;
		node.HeadingLevel = attributes.Level ?? 0;
		node.Landmark = attributes.LandmarkRole;
		node.Required = attributes.Required;
		node.PositionInSet = attributes.PositionInSet ?? 0;
		node.SizeOfSet = attributes.SizeOfSet ?? 0;
		ApplyStructuralRole(node);

		try
		{
			if (peer.GetPattern(PatternInterface.Toggle) is IToggleProvider toggleProvider)
			{
				node.HasToggle = true;
				node.Checked = toggleProvider.ToggleState == ToggleState.On;
				node.Indeterminate = toggleProvider.ToggleState == ToggleState.Indeterminate;

				// ToggleButton and ToggleSwitch are Button-typed; as push buttons screen readers
				// would never say whether they are on.
				if (node.Role == AtspiRoleMap.PushButtonRoleId)
				{
					(node.Role, node.RoleName) = (AtspiRoleMap.ToggleButtonRoleId, AtspiRoleMap.ToggleButtonRoleName);
				}
			}

			if (peer.GetPattern(PatternInterface.RangeValue) is IRangeValueProvider rangeProvider)
			{
				node.HasRange = true;
				node.Min = rangeProvider.Minimum;
				node.Max = rangeProvider.Maximum;
				node.Val = rangeProvider.Value;
			}

			if (peer.GetPattern(PatternInterface.Value) is IValueProvider valueProvider)
			{
				node.HasText = true;
				node.Text = valueProvider.Value ?? "";
				node.ReadOnly = valueProvider.IsReadOnly;
				node.Editable = !valueProvider.IsReadOnly;
			}

			if (peer.GetPattern(PatternInterface.ExpandCollapse) is IExpandCollapseProvider expandProvider)
			{
				node.Expandable = true;
				node.Expanded = expandProvider.ExpandCollapseState is ExpandCollapseState.Expanded or ExpandCollapseState.PartiallyExpanded;
			}

			if (peer.GetPattern(PatternInterface.SelectionItem) is ISelectionItemProvider selectionItemProvider)
			{
				node.Selectable = true;
				node.Selected = selectionItemProvider.IsSelected;
			}
			else if (attributes.Selected is { } ariaSelected)
			{
				// ListViewItem/ListBoxItem peers are container peers without the
				// SelectionItem pattern; AriaMapper still resolves their selected state.
				node.Selectable = true;
				node.Selected = ariaSelected;
			}

			if (!node.HasToggle && attributes.Checked is not null)
			{
				node.HasToggle = true;
				node.Checked = attributes.Checked == "true";
			}
		}
		catch
		{
			// Some peers throw when queried before fully initialized; the attributes
			// above were captured already and are refreshed on property changes.
		}
	}

	// Headings and landmarks have dedicated AT-SPI roles; screen readers navigate by
	// role (e.g. Orca's H key), so a "level" attribute on a label is not enough.
	private static void ApplyStructuralRole(AtspiNode node)
	{
		if (node.HeadingLevel > 0)
		{
			(node.Role, node.RoleName) = (AtspiRoleMap.HeadingRoleId, AtspiRoleMap.HeadingRoleName);
		}
		else if (!string.IsNullOrEmpty(node.Landmark))
		{
			(node.Role, node.RoleName) = (AtspiRoleMap.LandmarkRoleId, AtspiRoleMap.LandmarkRoleName);
		}
	}

	private void PopulateComboBoxItems(AtspiNode comboNode, ComboBox comboBox)
	{
		comboNode.Expandable = true;
		comboNode.Expanded = comboBox.IsDropDownOpen;

		// The combo's selected value is always exposed as text (like the macOS head),
		// but the items themselves are only enumerated while the dropdown is open — an
		// eager walk of comboBox.Items would materialize a large/virtualized ItemsSource
		// on every rebuild. A rebuild is coalesced-triggered when the dropdown opens.
		comboNode.Text = GetComboBoxText(comboBox);
		comboNode.HasText = true;

		if (!comboBox.IsDropDownOpen)
		{
			return;
		}

		for (var index = 0; index < comboBox.Items.Count; index++)
		{
			var item = comboBox.Items[index];
			var itemElement = item as ComboBoxItem;
			var (itemRole, itemRoleName) = AtspiRoleMap.GetRole(AutomationControlType.ListItem);
			var itemNode = new AtspiNode
			{
				Path = itemElement is not null
					? GetOrCreatePath(itemElement.Visual.Handle)
					: GetOrCreateItemPath(comboNode.Handle, index),
				Name = (itemElement?.Content ?? item)?.ToString() ?? $"item {index}",
				Role = itemRole,
				RoleName = itemRoleName,
				Enabled = true,
				Parent = comboNode,
				Selectable = true,
				Selected = index == comboBox.SelectedIndex,
				ItemIndex = index,
			};
			// Data-only items have no ComboBoxItem container (Handle stays 0); they are
			// reached by path via the combo's children and driven through the parent's
			// SelectChild(ItemIndex), so only handle-backed items go in the handle maps.
			if (itemElement is not null)
			{
				itemNode.Handle = itemElement.Visual.Handle;
				ApplyBounds(itemNode, itemElement);
				_elementsByHandle[itemElement.Visual.Handle] = itemElement;
				_nodesByHandle[itemNode.Handle] = itemNode;
			}
			comboNode.Children.Add(itemNode);
		}
	}

	// SelectionBoxItem is only populated in inline mode; otherwise the faceplate shows the
	// selected item (or its ComboBoxItem's content) directly, as ComboBox.GetSelectionContent does.
	private static string GetComboBoxText(ComboBox comboBox)
	{
		var content = comboBox.SelectionBoxItem
			?? (comboBox.SelectedItem is ComboBoxItem item ? item.Content : comboBox.SelectedItem);
		return content is null ? string.Empty : FrameworkElement.GetStringFromObject(content) ?? string.Empty;
	}

	private string GetOrCreatePath(nint handle)
	{
		if (!_pathsByHandle.TryGetValue(handle, out var path))
		{
			path = NodePathPrefix + _nextPath++;
			_pathsByHandle[handle] = path;
		}
		return path;
	}

	// Data-only combo items have no container element (Handle stays 0), so their
	// identity is the owning combo plus the item index.
	private string GetOrCreateItemPath(nint comboHandle, int index)
	{
		if (!_itemPathsByComboIndex.TryGetValue((comboHandle, index), out var path))
		{
			path = NodePathPrefix + _nextPath++;
			_itemPathsByComboIndex[(comboHandle, index)] = path;
		}
		return path;
	}

	private static string ResolveName(AutomationPeer? peer)
	{
		if (peer is null)
		{
			return string.Empty;
		}

		var label = ResolveLabel(peer);
		if (!string.IsNullOrEmpty(label))
		{
			return label;
		}

		return peer.GetName() ?? string.Empty;
	}

	protected override void OnChildAdded(UIElement parent, UIElement child, int? index)
	{
		TrySubscribeScrollSource(child);
		QueueRebuildIfNeeded();
		// Emit after the queued rebuild (FIFO on the same queue), so a client that
		// re-fetches children on this signal observes the updated tree. The child is
		// reported under its published parent (Raw ancestors are skipped), and not at
		// all when it is itself not published.
		X11XamlRootHost.QueueAction(_host, () =>
		{
			if (_server is { } server
				&& _nodesByHandle.TryGetValue(child.Visual.Handle, out var childNode)
				&& childNode.Parent is { } parentNode)
			{
				server.EmitChildrenChanged(parentNode, added: true, parentNode.Children.IndexOf(childNode), childNode);
			}
		});
	}

	protected override void OnChildRemoved(UIElement parent, UIElement child)
	{
		// Capture the removed child's node and position before the rebuild forgets them.
		QueueRebuildIfNeeded();
		if (!_nodesByHandle.TryGetValue(child.Visual.Handle, out var removedNode) || removedNode.Parent is not { } oldParent)
		{
			return;
		}

		var removedIndex = oldParent.Children.IndexOf(removedNode);
		var parentHandle = oldParent.Handle;
		if (removedIndex < 0)
		{
			return;
		}

		X11XamlRootHost.QueueAction(_host, () =>
		{
			if (_server is { } server && _nodesByHandle.TryGetValue(parentHandle, out var parentNode))
			{
				server.EmitChildrenChanged(parentNode, added: false, removedIndex, removedNode);
			}
		});
	}

	public override void NotifyInvalidatePeer(AutomationPeer peer)
	{
		if (IsDisposed || !IsAccessibilityEnabled)
		{
			return;
		}

		// Re-evaluates the peer's automatic properties (IsEnabled/IsOffscreen/Name/...).
		base.NotifyInvalidatePeer(peer);
		QueueRebuildIfNeeded();
	}

	private void QueueRebuildIfNeeded()
	{
		if (!_treeInitialized)
		{
			return;
		}

		if (_window.RootElement is { } rootElement)
		{
			QueueTreeBuild(rootElement);
		}
	}

	// ConfigureNotify lands here through NativeWindowWrapperBase; shift every published
	// bound by the origin delta so boxes stay in screen space after a window move.
	private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
	{
		if (!_treeInitialized || IsDisposed)
		{
			return;
		}

		if (args.DidSizeChange)
		{
			ApplyFrameBounds(_root);
		}

		if (!args.DidPositionChange)
		{
			return;
		}

		var (originX, originY) = GetClientOrigin();
		var (deltaX, deltaY) = (originX - _originX, originY - _originY);
		if (deltaX == 0 && deltaY == 0)
		{
			return;
		}

		(_originX, _originY) = (originX, originY);
		if (_root is { } root)
		{
			root.X += deltaX;
			root.Y += deltaY;
		}
		foreach (var node in _nodesByHandle.Values)
		{
			node.X += deltaX;
			node.Y += deltaY;
		}
	}

	// Also re-emitted by the base for every descendant of a scrolled ScrollViewer.
	protected override void OnSizeOrOffsetChanged(Visual visual)
	{
		if (!IsAccessibilityEnabled || !_treeInitialized)
		{
			return;
		}

		if (visual is not ContainerVisual containerVisual ||
			containerVisual.Owner?.Target is not UIElement element ||
			!_nodesByHandle.TryGetValue(containerVisual.Handle, out var node))
		{
			return;
		}

		if (node == _frame)
		{
			ApplyFrameBounds(_root);
			return;
		}

		var wasOffscreen = node.Offscreen;
		ApplyBounds(node, element);
		if (node.Offscreen != wasOffscreen)
		{
			EmitShowing(node);
		}
	}

	private void EmitShowing(AtspiNode node)
	{
		_server?.EmitStateChanged(node, "showing", node.Offscreen ? 0 : 1);
		_server?.EmitStateChanged(node, "visible", node.Offscreen ? 0 : 1);
	}

	protected override void UpdateName(nint handle, AutomationPeer peer, string? label)
	{
		if (_nodesByHandle.TryGetValue(handle, out var node))
		{
			node.Name = label ?? string.Empty;
			if (node.IsStaticText)
			{
				node.Text = node.Name;
			}
			_server?.EmitPropertyChange(node, "accessible-name", node.Name);
		}
	}

	protected override void UpdateEnabled(nint handle, bool enabled)
	{
		if (_nodesByHandle.TryGetValue(handle, out var node))
		{
			node.Enabled = enabled;
			_server?.EmitStateChanged(node, "enabled", enabled ? 1 : 0);
			_server?.EmitStateChanged(node, "sensitive", enabled ? 1 : 0);
		}
	}

	protected override void UpdateFocusable(nint handle, bool focusable)
	{
		if (_nodesByHandle.TryGetValue(handle, out var node))
		{
			node.Focusable = focusable;
		}
	}

	protected override void UpdateToggleState(nint handle, AutomationPeer peer, ToggleState newState)
	{
		if (_nodesByHandle.TryGetValue(handle, out var node))
		{
			node.Checked = newState == ToggleState.On;
			node.Indeterminate = newState == ToggleState.Indeterminate;
			_server?.EmitStateChanged(node, "checked", node.Checked ? 1 : 0);
			_server?.EmitStateChanged(node, "indeterminate", node.Indeterminate ? 1 : 0);
		}
	}

	protected override void UpdateRangeValue(nint handle, AutomationPeer peer, double value)
	{
		if (_nodesByHandle.TryGetValue(handle, out var node))
		{
			node.Val = value;
			_server?.EmitPropertyChange(node, "accessible-value", value);
		}
	}

	protected override void UpdateRangeBounds(nint handle, double min, double max)
	{
		if (_nodesByHandle.TryGetValue(handle, out var node))
		{
			node.Min = min;
			node.Max = max;
		}
	}

	// Raised both for the Value property change and the text-changed automation event
	// of the same edit; only the first one carries a difference.
	protected override void UpdateTextValue(nint handle, string? value)
	{
		if (!_nodesByHandle.TryGetValue(handle, out var node))
		{
			return;
		}

		// The ComboBox peer reports its value from SelectionBoxItem, which is empty outside
		// inline mode; read what the combo actually displays instead.
		var newText = _elementsByHandle.TryGetValue(handle, out var element) && element is ComboBox comboBox
			? GetComboBoxText(comboBox)
			: value ?? string.Empty;
		var oldText = node.Text;
		if (string.Equals(oldText, newText, StringComparison.Ordinal))
		{
			return;
		}

		node.Text = newText;
		// Clients handling text-changed read the caret right away (Orca's word echo checks the
		// character before it), so it must already reflect the edit; the caret-moved signal
		// itself still follows, from PublishCaret.
		if (element is TextBox editedTextBox)
		{
			node.SelectionStart = editedTextBox.SelectionStart;
			node.SelectionEnd = editedTextBox.SelectionStart + editedTextBox.SelectionLength;
		}

		if (_server is not { } server)
		{
			return;
		}

		if (node.Editable)
		{
			EmitTextDiff(server, node, oldText, newText);
		}
		else
		{
			server.EmitPropertyChange(node, "accessible-value", newText);
		}
	}

	// Screen readers echo edits from text-changed:insert/delete, which carry the changed
	// span; the edit is the single range between the common prefix and suffix.
	private static void EmitTextDiff(AtspiServer server, AtspiNode node, string oldText, string newText)
	{
		var prefix = 0;
		var maxPrefix = Math.Min(oldText.Length, newText.Length);
		while (prefix < maxPrefix && oldText[prefix] == newText[prefix])
		{
			prefix++;
		}
		if (prefix > 0 && char.IsHighSurrogate(oldText[prefix - 1]))
		{
			prefix--;
		}

		var suffix = 0;
		var maxSuffix = maxPrefix - prefix;
		while (suffix < maxSuffix && oldText[oldText.Length - 1 - suffix] == newText[newText.Length - 1 - suffix])
		{
			suffix++;
		}
		if (suffix > 0 && char.IsLowSurrogate(oldText[oldText.Length - suffix]))
		{
			suffix--;
		}

		var offset = AtspiTextSegmentation.CharacterCount(oldText, prefix);
		var deleted = oldText.Substring(prefix, oldText.Length - prefix - suffix);
		var inserted = newText.Substring(prefix, newText.Length - prefix - suffix);
		if (deleted.Length > 0)
		{
			server.EmitTextChanged(node, inserted: false, offset, AtspiTextSegmentation.CharacterCount(deleted, deleted.Length), deleted);
		}
		if (inserted.Length > 0)
		{
			server.EmitTextChanged(node, inserted: true, offset, AtspiTextSegmentation.CharacterCount(inserted, inserted.Length), inserted);
		}
	}

	protected override void UpdateExpandCollapseState(nint handle, bool isExpanded)
	{
		if (_nodesByHandle.TryGetValue(handle, out var node))
		{
			node.Expanded = isExpanded;
			_server?.EmitStateChanged(node, "expanded", isExpanded ? 1 : 0);
		}
	}

	protected override void UpdateSelected(nint handle, bool selected)
	{
		if (_nodesByHandle.TryGetValue(handle, out var node))
		{
			node.Selected = selected;
			_server?.EmitStateChanged(node, "selected", selected ? 1 : 0);
			if (node.Parent is { } parentNode)
			{
				_server?.EmitSelectionChanged(parentNode);
			}
		}
	}

	protected override void UpdateRoleDescription(nint handle, string? roleDescription) { }

	protected override void UpdateHelpText(nint handle, string? helpText)
	{
		if (_nodesByHandle.TryGetValue(handle, out var node))
		{
			if (helpText is not null && helpText == node.Placeholder)
			{
				return;
			}

			node.Description = helpText;
			_server?.EmitPropertyChange(node, "accessible-description", node.Description ?? string.Empty);
		}
	}

	protected override void UpdateHeadingLevel(nint handle, int level)
	{
		if (_nodesByHandle.TryGetValue(handle, out var node))
		{
			node.HeadingLevel = level;
			UpdateStructuralRole(handle, node);
		}
	}

	protected override void UpdateLandmark(nint handle, string? landmarkRole)
	{
		if (_nodesByHandle.TryGetValue(handle, out var node))
		{
			node.Landmark = landmarkRole;
			UpdateStructuralRole(handle, node);
		}
	}

	private void UpdateStructuralRole(nint handle, AtspiNode node)
	{
		if (_elementsByHandle.TryGetValue(handle, out var element) && element.GetOrCreateAutomationPeer() is { } peer)
		{
			(node.Role, node.RoleName) = AtspiRoleMap.GetRole(peer.GetAutomationControlType());
		}
		ApplyStructuralRole(node);
		_server?.EmitPropertyChange(node, "accessible-role", (int)node.Role);
	}

	protected override void UpdateIsReadOnly(nint handle, bool isReadOnly)
	{
		if (_nodesByHandle.TryGetValue(handle, out var node))
		{
			node.ReadOnly = isReadOnly;
			_server?.EmitStateChanged(node, "read-only", isReadOnly ? 1 : 0);
		}
	}

	protected override void UpdateIsOffscreen(nint handle, bool isOffscreen)
	{
		if (_nodesByHandle.TryGetValue(handle, out var node) && node.Offscreen != isOffscreen)
		{
			node.Offscreen = isOffscreen;
			EmitShowing(node);
		}
	}

	protected override void SetNativeFocus(nint handle)
	{
		if (!_nodesByHandle.TryGetValue(handle, out var node))
		{
			return;
		}

		// Only un-focus a node that is still published: one dropped by a rebuild has no
		// path left for the client to query.
		if (_focusedNode is { } previous && previous != node
			&& _nodesByHandle.TryGetValue(previous.Handle, out var current) && ReferenceEquals(current, previous))
		{
			_server?.EmitStateChanged(previous, "focused", 0);
		}

		_focusedNode = node;
		_server?.SetFocus(node);
		_server?.EmitStateChanged(node, "focused", 1);
		TrackFocusedTextBox(_elementsByHandle.TryGetValue(handle, out var element) ? element : null);
	}

	// Caret moves are only observable through TextBox.SelectionChanged, and only the
	// focused text box has a caret a screen reader follows.
	private void TrackFocusedTextBox(UIElement? element)
	{
		if (ReferenceEquals(_focusedTextBox, element))
		{
			return;
		}

		if (_focusedTextBox is { } previous)
		{
			previous.SelectionChanged -= OnFocusedTextSelectionChanged;
		}

		_focusedTextBox = element as TextBox;
		if (_focusedTextBox is { } textBox)
		{
			textBox.SelectionChanged += OnFocusedTextSelectionChanged;
			_announcedCaret = textBox.SelectionStart + textBox.SelectionLength;
			_announcedSelection = textBox.SelectionLength > 0;
		}
	}

	private void OnFocusedTextSelectionChanged(object sender, RoutedEventArgs e)
	{
		if (_caretUpdateQueued || sender is not TextBox textBox)
		{
			return;
		}

		// Uno raises SelectionChanged before the text change that moved the caret. Publish it
		// after the edit: screen readers tell typing from caret navigation by text-changed
		// arriving ahead of text-caret-moved (Orca's word echo depends on it).
		_caretUpdateQueued = true;
		X11XamlRootHost.QueueAction(_host, () =>
		{
			_caretUpdateQueued = false;
			PublishCaret(textBox);
		});
	}

	private void PublishCaret(TextBox textBox)
	{
		if (_server is not { } server || !_nodesByHandle.TryGetValue(textBox.Visual.Handle, out var node))
		{
			return;
		}

		if (!string.Equals(node.Text, textBox.Text, StringComparison.Ordinal))
		{
			UpdateTextValue(textBox.Visual.Handle, textBox.Text);
		}

		var start = textBox.SelectionStart;
		var end = start + textBox.SelectionLength;
		node.SelectionStart = start;
		node.SelectionEnd = end;

		// Compared with what was last announced, not with the node: the text-change path
		// already moved the node's caret.
		if (end != _announcedCaret)
		{
			server.EmitTextCaretMoved(node, AtspiTextSegmentation.CharacterCount(textBox.Text, end));
		}
		if (_announcedSelection || end > start)
		{
			server.EmitTextSelectionChanged(node);
		}
		_announcedCaret = end;
		_announcedSelection = end > start;
	}

	protected override void OnNativeStructureChanged()
	{
		QueueRebuildIfNeeded();
	}

	// Called from the base's debounce timer, off the UI thread. Sent from the frame: a
	// screen reader drops an event whose source it can no longer resolve, and the focused
	// node may already be gone (e.g. a closed dialog's button).
	protected override void AnnounceOnPlatform(string text, bool assertive)
		=> X11XamlRootHost.QueueAction(_host, () =>
		{
			if (_server is { } server && (_frame ?? _focusedNode) is { } source)
			{
				server.EmitAnnouncement(source, text, assertive);
			}
		});

	// A live region (AutomationProperties.LiveSetting) announces its new content, as on macOS.
	public override void NotifyAutomationEvent(AutomationPeer peer, AutomationEvents eventId)
	{
		base.NotifyAutomationEvent(peer, eventId);

		if (eventId != AutomationEvents.LiveRegionChanged
			|| IsDisposed
			|| !IsAccessibilityEnabled
			|| !TryGetPeerOwner(peer, out var liveElement))
		{
			return;
		}

		var text = ResolveName(peer);
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		if (AutomationProperties.GetLiveSetting(liveElement) == AutomationLiveSetting.Assertive)
		{
			AnnounceAssertive(text);
		}
		else
		{
			AnnouncePolite(text);
		}
	}

	// ──────────────────────────────────────────────────────────────
	//  AtspiServer.IWriteTarget — drive the real control from the
	//  D-Bus thread. Each method dispatches to the UI thread and
	//  returns once the action is enqueued; the state-changed signal
	//  emitted by the Update* overrides is the confirmation.
	// ──────────────────────────────────────────────────────────────

	bool AtspiServer.IWriteTarget.Invoke(AtspiNode node)
	{
		if (node.Selectable && node.Parent is { } parentNode && parentNode.RoleName == "combo box")
		{
			return ((AtspiServer.IWriteTarget)this).SelectChild(parentNode, node.ItemIndex);
		}

		if (!_elementsSnapshot.TryGetValue(node.Handle, out var element))
		{
			return false;
		}

		return element.DispatcherQueue.TryEnqueue(() => InvokeOnUiThread(element));
	}

	bool AtspiServer.IWriteTarget.SetRangeValue(AtspiNode node, double value)
	{
		if (!_elementsSnapshot.TryGetValue(node.Handle, out var element))
		{
			return false;
		}

		return element.DispatcherQueue.TryEnqueue(() => SetRangeValueOnUiThread(element, value));
	}

	bool AtspiServer.IWriteTarget.SetText(AtspiNode node, string text)
	{
		if (!_elementsSnapshot.TryGetValue(node.Handle, out var element))
		{
			return false;
		}

		return element.DispatcherQueue.TryEnqueue(() => SetTextOnUiThread(element, text));
	}

	bool AtspiServer.IWriteTarget.SetTextSelection(AtspiNode node, int start, int end)
	{
		if (!_elementsSnapshot.TryGetValue(node.Handle, out var element) || element is not TextBox textBox)
		{
			return false;
		}

		return textBox.DispatcherQueue.TryEnqueue(() =>
		{
			var length = textBox.Text.Length;
			var from = Math.Clamp(Math.Min(start, end), 0, length);
			var to = Math.Clamp(Math.Max(start, end), 0, length);
			textBox.Select(from, to - from);
		});
	}

	bool AtspiServer.IWriteTarget.SelectChild(AtspiNode node, int index)
	{
		if (_elementsSnapshot.TryGetValue(node.Handle, out var element) && element is ComboBox comboBox)
		{
			return comboBox.DispatcherQueue.TryEnqueue(() => SelectComboBoxItemOnUiThread(comboBox, index));
		}

		// Other selection containers (ListBox/ListView): drive the item's own provider.
		if (index < 0 || index >= node.Children.Count ||
			!_elementsSnapshot.TryGetValue(node.Children[index].Handle, out var itemElement))
		{
			return false;
		}

		return itemElement.DispatcherQueue.TryEnqueue(() => SelectItemOnUiThread(itemElement));
	}

	private static void SelectItemOnUiThread(UIElement element)
	{
		var peer = element.GetOrCreateAutomationPeer();
		if (peer?.GetPattern(PatternInterface.SelectionItem) is not ISelectionItemProvider selectionItemProvider)
		{
			return;
		}

		// AddToSelection throws on a single-selection container that already has a
		// selection; there, selecting the child means replacing the selection.
		var owner = ItemsControl.ItemsControlFromItemContainer(element);
		var multiple = owner is ListViewBase { SelectionMode: ListViewSelectionMode.Multiple or ListViewSelectionMode.Extended }
			|| owner is ListBox { SelectionMode: SelectionMode.Multiple or SelectionMode.Extended };
		if (multiple)
		{
			selectionItemProvider.AddToSelection();
		}
		else
		{
			selectionItemProvider.Select();
		}
	}

	private static void InvokeOnUiThread(UIElement element)
	{
		var peer = element.GetOrCreateAutomationPeer();
		if (peer is null)
		{
			return;
		}

		if (peer.GetPattern(PatternInterface.Invoke) is IInvokeProvider invokeProvider)
		{
			invokeProvider.Invoke();
		}
		else if (peer.GetPattern(PatternInterface.Toggle) is IToggleProvider toggleProvider)
		{
			toggleProvider.Toggle();
		}
		else if (peer.GetPattern(PatternInterface.ExpandCollapse) is IExpandCollapseProvider expandCollapseProvider)
		{
			if (expandCollapseProvider.ExpandCollapseState == ExpandCollapseState.Expanded)
			{
				expandCollapseProvider.Collapse();
			}
			else
			{
				expandCollapseProvider.Expand();
			}
		}
		else if (peer.GetPattern(PatternInterface.SelectionItem) is ISelectionItemProvider selectionItemProvider)
		{
			selectionItemProvider.Select();
		}
	}

	private static void SetRangeValueOnUiThread(UIElement element, double value)
	{
		var peer = element.GetOrCreateAutomationPeer();
		if (peer?.GetPattern(PatternInterface.RangeValue) is IRangeValueProvider rangeValueProvider)
		{
			var clamped = Math.Max(rangeValueProvider.Minimum, Math.Min(rangeValueProvider.Maximum, value));
			rangeValueProvider.SetValue(clamped);
		}
	}

	private static void SetTextOnUiThread(UIElement element, string text)
	{
		var peer = element.GetOrCreateAutomationPeer();
		if (peer?.GetPattern(PatternInterface.Value) is IValueProvider { IsReadOnly: false } valueProvider)
		{
			valueProvider.SetValue(text);
		}
	}

	private void SelectComboBoxItemOnUiThread(ComboBox comboBox, int index)
	{
		if (index < 0 || index >= comboBox.Items.Count)
		{
			return;
		}

		// A ComboBox is single-selection: AddToSelection would be rejected once an item
		// is selected, so select (replace) instead.
		var itemElement = comboBox.Items[index] as ComboBoxItem;
		if (itemElement?.GetOrCreateAutomationPeer()?.GetPattern(PatternInterface.SelectionItem) is ISelectionItemProvider selectionItemProvider)
		{
			selectionItemProvider.Select();
		}
		else
		{
			comboBox.SelectedIndex = index;
		}

		// A selection made in the open dropdown is only tentative: light dismiss restores
		// the previous one. Close it the way picking an item does, which commits.
		if (comboBox.IsDropDownOpen)
		{
			comboBox.IsDropDownOpen = false;
		}

		// The combo's text and its items' selected states are captured at build time.
		QueueRebuildIfNeeded();
		X11XamlRootHost.QueueAction(_host, () =>
		{
			if (_server is { } server && _nodesByHandle.TryGetValue(comboBox.Visual.Handle, out var comboNode))
			{
				server.EmitSelectionChanged(comboNode);
			}
		});
	}

	protected override void DisposeCore()
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug("[A11y] Disposing X11Accessibility.");
		}

		_window.Activated -= OnWindowActivated;
		_window.AppWindow.Changed -= OnAppWindowChanged;
		AtspiEnablement.Changed -= OnEnablementChanged;

		StopServer();
	}

	private async void StopServerSafely(AtspiServer server)
	{
		try
		{
			await server.StopAsync();
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"[A11y] Failed to stop AT-SPI server: {ex.Message}");
			}
		}
	}
}
