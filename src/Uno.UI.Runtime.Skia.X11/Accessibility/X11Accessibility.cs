#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Skia;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// Publishes the Skia-rendered X11 visual tree to the AT-SPI2 accessibility bus
/// (<c>org.a11y.atspi</c>) as a tree of <see cref="AtspiNode"/> objects served by an
/// <see cref="AtspiServer"/>. Mirrors <c>MacOSAccessibility</c>, but instead of a
/// native per-window context it hands the whole tree to the process-scoped AT-SPI
/// server (the a11y D-Bus exposes one application root per process).
/// </summary>
internal sealed class X11Accessibility : SkiaAccessibilityBase, AtspiServer.IWriteTarget
{
	private const string RootPath = "/org/a11y/atspi/accessible/root";
	private const string NodePathPrefix = "/org/a11y/atspi/accessible/";

	private readonly X11XamlRootHost _host;
	private readonly Window _window;

	private AtspiServer? _server;
	private AtspiNode? _root;
	private readonly Dictionary<nint, AtspiNode> _nodesByHandle = new();
	private readonly Dictionary<nint, UIElement> _elementsByHandle = new();
	// Immutable snapshot published for the D-Bus reader thread (write-path lookups).
	// The mutable _elementsByHandle above is only touched on the UI thread during a build.
	private volatile IReadOnlyDictionary<nint, UIElement> _elementsSnapshot = new Dictionary<nint, UIElement>();
	private AtspiNode? _focusedNode;
	private bool _treeInitialized;
	private bool _treeBuildQueued;
	private int _nextPath = 1;
	// Window top-left in screen space, captured per build; added to each node's
	// client-space offset so AT-SPI extents are absolute screen coordinates.
	private double _originX;
	private double _originY;

	internal X11Accessibility(X11XamlRootHost host, Window window)
	{
		_host = host;
		_window = window;
	}

	public override bool IsAccessibilityEnabled => !IsDisposed && _server is not null;

	/// <summary>
	/// Starts the AT-SPI server asynchronously and, once it resolves, builds and
	/// publishes the accessibility tree on the UI thread. Called by the host after
	/// the native X11 window exists.
	/// </summary>
	internal void Initialize()
	{
		if (_server is not null || IsDisposed)
		{
			return;
		}

		// Subscribe on the UI thread so a RootElement that only becomes available
		// after the server starts still triggers the initial tree build.
		_window.Activated += OnWindowActivated;

		StartServerSafely();
	}

	private async void StartServerSafely()
	{
		try
		{
			var server = await AtspiServer.TryStartAsync(ResolveApplicationName(), this);
			if (server is null)
			{
				return;
			}

			if (IsDisposed)
			{
				// The window closed while the connection was being established;
				// stop the server so the D-Bus connection is not leaked.
				StopServerSafely(server);
				return;
			}

			_server = server;

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
		if (args.WindowActivationState == WindowActivationState.Deactivated)
		{
			return;
		}

		if (IsDisposed || _server is null || _treeInitialized)
		{
			return;
		}

		if (_window.RootElement is { } rootElement)
		{
			QueueTreeBuild(rootElement);
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
			_nodesByHandle.Clear();
			_elementsByHandle.Clear();
			_nextPath = 1;
			(_originX, _originY) = GetWindowOrigin();
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
		TrySubscribeScrollSource(rootElement);
		var root = BuildNode(rootElement, null, RootPath);

		// The AT-SPI application root must keep ATSPI_ROLE_APPLICATION regardless of
		// the root element's own peer role, or clients mislabel the whole app.
		root.Role = AtspiRoleMap.ApplicationRoleId;
		root.RoleName = AtspiRoleMap.ApplicationRoleName;
		if (string.IsNullOrEmpty(root.Name))
		{
			root.Name = ResolveApplicationName();
		}

		foreach (var child in rootElement.GetChildren())
		{
			BuildNodeRecursive(root, child);
		}
		return root;
	}

	private void BuildNodeRecursive(AtspiNode parent, UIElement child)
	{
		TrySubscribeScrollSource(child);

		var accessibilityView = AutomationProperties.GetAccessibilityView(child);
		if (accessibilityView == AccessibilityView.Raw)
		{
			foreach (var childChild in child.GetChildren())
			{
				BuildNodeRecursive(parent, childChild);
			}
			return;
		}

		var node = BuildNode(child, parent, NodePathPrefix + _nextPath++);
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

		// Client-space offset (sum of GetTotalOffset up the Visual parent chain, like
		// MacOSAccessibility.GetAbsoluteOffset) plus the window's screen origin, so the
		// published extents are absolute screen coordinates.
		var offset = GetAbsoluteOffset(element.Visual);
		var size = element.Visual.Size;

		var node = new AtspiNode
		{
			Path = path,
			Handle = element.Visual.Handle,
			Role = role,
			RoleName = roleName,
			Name = ResolveName(peer),
			Parent = parent,
			X = offset.X + _originX,
			Y = offset.Y + _originY,
			W = size.X,
			H = size.Y,
			Enabled = peer?.IsEnabled() ?? true,
			Focusable = peer?.IsKeyboardFocusable() ?? false,
			ItemIndex = parent?.Children.Count ?? -1,
		};

		if (peer is not null)
		{
			PopulateNodeFromPeer(node, peer);
		}

		_nodesByHandle[node.Handle] = node;
		_elementsByHandle[node.Handle] = element;

		if (element is ComboBox comboBox)
		{
			PopulateComboBoxItems(node, comboBox);
		}

		return node;
	}

	private static (double X, double Y) GetAbsoluteOffset(Visual visual)
	{
		double x = 0;
		double y = 0;
		var current = visual;
		while (current is not null)
		{
			var offset = current.GetTotalOffset();
			x += offset.X;
			y += offset.Y;
			current = current.Parent;
		}
		return (x, y);
	}

	private (double X, double Y) GetWindowOrigin()
	{
		try
		{
			var position = _window.AppWindow.Position;
			return (position.X, position.Y);
		}
		catch (Exception)
		{
			// AppWindow position can be unavailable before the window is mapped; fall
			// back to window-relative coordinates.
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

		try
		{
			if (peer.GetPattern(PatternInterface.Toggle) is IToggleProvider toggleProvider)
			{
				node.HasToggle = true;
				node.Checked = toggleProvider.ToggleState == ToggleState.On;
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

	private void PopulateComboBoxItems(AtspiNode comboNode, ComboBox comboBox)
	{
		comboNode.Expandable = true;
		for (var index = 0; index < comboBox.Items.Count; index++)
		{
			var item = comboBox.Items[index];
			var itemElement = item as ComboBoxItem;
			var (itemRole, itemRoleName) = AtspiRoleMap.GetRole(AutomationControlType.ListItem);
			var itemNode = new AtspiNode
			{
				Path = NodePathPrefix + _nextPath++,
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
				_elementsByHandle[itemElement.Visual.Handle] = itemElement;
				_nodesByHandle[itemNode.Handle] = itemNode;
			}
			comboNode.Children.Add(itemNode);
		}

		// The combo also exposes its current selection as text.
		comboNode.Text = comboNode.Children.Find(c => c.Selected)?.Name ?? "";
		comboNode.HasText = true;
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
		if (_server is { } server && _nodesByHandle.TryGetValue(parent.Visual.Handle, out var parentNode))
		{
			server.EmitChildrenChanged(parentNode, added: true, index ?? -1);
		}
	}

	protected override void OnChildRemoved(UIElement parent, UIElement child)
	{
		if (_server is { } server && _nodesByHandle.TryGetValue(parent.Visual.Handle, out var parentNode))
		{
			var childNode = _nodesByHandle.TryGetValue(child.Visual.Handle, out var removed) ? removed : null;
			var index = childNode is not null ? parentNode.Children.IndexOf(childNode) : -1;
			server.EmitChildrenChanged(parentNode, added: false, index);
		}
		QueueRebuildIfNeeded();
	}

	public override void NotifyInvalidatePeer(AutomationPeer peer)
	{
		if (IsDisposed || !IsAccessibilityEnabled)
		{
			return;
		}

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

	protected override void OnSizeOrOffsetChanged(Visual visual)
	{
		if (!IsAccessibilityEnabled || !_treeInitialized)
		{
			return;
		}

		if (visual is not ContainerVisual containerVisual ||
			containerVisual.Owner?.Target is not UIElement ||
			!_nodesByHandle.TryGetValue(containerVisual.Handle, out var node))
		{
			return;
		}

		var offset = GetAbsoluteOffset(containerVisual);
		node.X = offset.X + _originX;
		node.Y = offset.Y + _originY;
		node.W = containerVisual.Size.X;
		node.H = containerVisual.Size.Y;
	}

	protected override void UpdateName(nint handle, AutomationPeer peer, string? label)
	{
		if (_nodesByHandle.TryGetValue(handle, out var node))
		{
			node.Name = label ?? string.Empty;
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
			_server?.EmitStateChanged(node, "checked", node.Checked ? 1 : 0);
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

	protected override void UpdateTextValue(nint handle, string? value)
	{
		if (_nodesByHandle.TryGetValue(handle, out var node))
		{
			node.Text = value ?? string.Empty;
			_server?.EmitPropertyChange(node, "accessible-value", node.Text);
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

	protected override void UpdateHelpText(nint handle, string? helpText)
	{
		if (_nodesByHandle.TryGetValue(handle, out var node))
		{
			node.Description = helpText;
		}
	}

	protected override void UpdateHeadingLevel(nint handle, int level)
	{
		if (_nodesByHandle.TryGetValue(handle, out var node))
		{
			node.HeadingLevel = level;
		}
	}

	protected override void UpdateLandmark(nint handle, string? landmarkRole)
	{
		if (_nodesByHandle.TryGetValue(handle, out var node))
		{
			node.Landmark = landmarkRole;
		}
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
		if (_nodesByHandle.TryGetValue(handle, out var node))
		{
			node.Offscreen = isOffscreen;
			_server?.EmitStateChanged(node, "showing", isOffscreen ? 0 : 1);
			_server?.EmitStateChanged(node, "visible", isOffscreen ? 0 : 1);
		}
	}

	protected override void SetNativeFocus(nint handle)
	{
		if (!_nodesByHandle.TryGetValue(handle, out var node))
		{
			return;
		}

		if (_focusedNode is { } previous && previous != node)
		{
			_server?.EmitStateChanged(previous, "focused", 0);
		}

		_focusedNode = node;
		_server?.SetFocus(node);
		_server?.EmitStateChanged(node, "focused", 1);
	}

	protected override void OnNativeStructureChanged()
	{
		QueueRebuildIfNeeded();
	}

	protected override void AnnounceOnPlatform(string text, bool assertive)
	{
		// AT-SPI has no simple bus-level announce; live regions are surfaced via
		// StateChanged/PropertyChange signals rather than a native speech API.
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

	bool AtspiServer.IWriteTarget.SelectChild(AtspiNode node, int index)
	{
		if (!_elementsSnapshot.TryGetValue(node.Handle, out var element) || element is not ComboBox comboBox)
		{
			return false;
		}

		return comboBox.DispatcherQueue.TryEnqueue(() => SelectChildOnUiThread(comboBox, index));
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

	private static void SelectChildOnUiThread(ComboBox comboBox, int index)
	{
		if (index < 0 || index >= comboBox.Items.Count)
		{
			return;
		}

		var itemElement = comboBox.Items[index] as ComboBoxItem;
		var peer = itemElement?.GetOrCreateAutomationPeer();
		if (peer?.GetPattern(PatternInterface.SelectionItem) is ISelectionItemProvider selectionItemProvider)
		{
			selectionItemProvider.AddToSelection();
		}
		else
		{
			comboBox.SelectedIndex = index;
		}
	}

	protected override void DisposeCore()
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug("[A11y] Disposing X11Accessibility.");
		}

		_window.Activated -= OnWindowActivated;

		var server = _server;
		_server = null;
		_root = null;
		_nodesByHandle.Clear();
		_elementsByHandle.Clear();
		_focusedNode = null;
		_treeInitialized = false;

		if (server is not null)
		{
			StopServerSafely(server);
		}
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
