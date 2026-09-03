#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
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
internal sealed class X11Accessibility : SkiaAccessibilityBase
{
	private const string RootPath = "/org/a11y/atspi/accessible/root";
	private const string NodePathPrefix = "/org/a11y/atspi/accessible/";

	private readonly X11XamlRootHost _host;
	private readonly Window _window;

	private AtspiServer? _server;
	private AtspiNode? _root;
	private readonly Dictionary<nint, AtspiNode> _nodesByHandle = new();
	private bool _treeInitialized;
	private bool _treeBuildQueued;
	private int _nextPath = 1;

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
			var server = await AtspiServer.TryStartAsync(ResolveApplicationName());
			if (IsDisposed || server is null)
			{
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
		catch
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
			_nextPath = 1;
			_root = BuildRootNode(rootElement);
			_treeInitialized = true;
			_server!.SetRoot(_root);

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
		var root = BuildNode(rootElement, null, RootPath);

		// The AT-SPI application root must keep ATSPI_ROLE_APPLICATION regardless of
		// the root element's own peer role, or clients mislabel the whole app.
		root.Role = 75; // ATSPI_ROLE_APPLICATION
		root.RoleName = "application";
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

		// GetBoundingRectangle returns client coordinates. The X11 window origin is not
		// yet wired here, so boxes are window-relative; a follow-up PR adds the window
		// position offset before publishing.
		var bounds = peer?.GetBoundingRectangle() ?? default;

		var node = new AtspiNode
		{
			Path = path,
			Handle = element.Visual.Handle,
			Role = role,
			RoleName = roleName,
			Name = ResolveName(peer),
			Parent = parent,
			X = bounds.X,
			Y = bounds.Y,
			W = bounds.Width,
			H = bounds.Height,
			Enabled = peer?.IsEnabled() ?? true,
			Focusable = peer?.IsKeyboardFocusable() ?? false,
		};

		_nodesByHandle[node.Handle] = node;

		return node;
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
		=> QueueRebuildIfNeeded();

	protected override void OnChildRemoved(UIElement parent, UIElement child)
		=> QueueRebuildIfNeeded();

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
			containerVisual.Owner?.Target is not UIElement owner ||
			!_nodesByHandle.TryGetValue(containerVisual.Handle, out var node))
		{
			return;
		}

		var peer = owner.GetOrCreateAutomationPeer();
		if (peer is null)
		{
			return;
		}

		var rect = peer.GetBoundingRectangle();
		node.X = rect.X;
		node.Y = rect.Y;
		node.W = rect.Width;
		node.H = rect.Height;
	}

	protected override void UpdateName(nint handle, AutomationPeer peer, string? label)
	{
		if (_nodesByHandle.TryGetValue(handle, out var node))
		{
			node.Name = label ?? string.Empty;
		}
	}

	protected override void UpdateEnabled(nint handle, bool enabled)
	{
		if (_nodesByHandle.TryGetValue(handle, out var node))
		{
			node.Enabled = enabled;
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
		// AT-SPI event emission lands in the live-events PR.
	}

	protected override void UpdateRangeValue(nint handle, AutomationPeer peer, double value)
	{
		// AT-SPI event emission lands in the live-events PR.
	}

	protected override void UpdateRangeBounds(nint handle, double min, double max)
	{
		// AT-SPI event emission lands in the live-events PR.
	}

	protected override void UpdateTextValue(nint handle, string? value)
	{
		// AT-SPI event emission lands in the live-events PR.
	}

	protected override void UpdateExpandCollapseState(nint handle, bool isExpanded)
	{
		// AT-SPI event emission lands in the live-events PR.
	}

	protected override void UpdateSelected(nint handle, bool selected)
	{
		// AT-SPI event emission lands in the live-events PR.
	}

	protected override void UpdateHelpText(nint handle, string? helpText)
	{
		// AT-SPI event emission lands in the live-events PR.
	}

	protected override void UpdateHeadingLevel(nint handle, int level)
	{
		// AT-SPI event emission lands in the live-events PR.
	}

	protected override void UpdateLandmark(nint handle, string? landmarkRole)
	{
		// AT-SPI event emission lands in the live-events PR.
	}

	protected override void UpdateIsReadOnly(nint handle, bool isReadOnly)
	{
		// AT-SPI event emission lands in the live-events PR.
	}

	protected override void UpdateIsOffscreen(nint handle, bool isOffscreen)
	{
		// AT-SPI event emission lands in the live-events PR.
	}

	protected override void SetNativeFocus(nint handle)
	{
		// AT-SPI event emission lands in the live-events PR.
	}

	protected override void OnNativeStructureChanged()
	{
		// AT-SPI event emission lands in the live-events PR.
	}

	protected override void AnnounceOnPlatform(string text, bool assertive)
	{
		// AT-SPI event emission lands in the live-events PR.
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
