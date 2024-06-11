#nullable enable
#if WINAPPSDK || HAS_UNO_WINUI
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Uno.Diagnostics.UI;

/// <summary>
/// An overlay layer used to inject analytics and diagnostics indicators into the UI.
/// </summary>
[TemplatePart(Name = ElementsPanelPartName, Type = typeof(Panel))]
[TemplatePart(Name = AnchorPartName, Type = typeof(UIElement))]
[TemplatePart(Name = NotificationPartName, Type = typeof(ContentPresenter))]
[TemplateVisualState(GroupName = "DisplayMode", Name = DisplayModeCompactStateName)]
[TemplateVisualState(GroupName = "DisplayMode", Name = DisplayModeExpandedStateName)]
[TemplateVisualState(GroupName = "Notification", Name = NotificationCollapsedStateName)]
[TemplateVisualState(GroupName = "Notification", Name = NotificationVisibleStateName)]
public sealed partial class DiagnosticsOverlay : Control
{
	private const string ElementsPanelPartName = "PART_Elements";
	private const string AnchorPartName = "PART_Anchor";
	private const string NotificationPartName = "PART_Notification";

	private const string DisplayModeCompactStateName = "Compact";
	private const string DisplayModeExpandedStateName = "Expanded";

	private const string NotificationCollapsedStateName = "Collapsed";
	private const string NotificationVisibleStateName = "Visible";

	private static readonly ConditionalWeakTable<XamlRoot, DiagnosticsOverlay> _overlays = new();

	/// <summary>
	/// Gets the <see cref="DiagnosticsOverlay"/> for the specified <see cref="XamlRoot"/>.
	/// </summary>
	/// <param name="root">The root onto which the overlay is being rendered.</param>
	/// <returns></returns>
	public static DiagnosticsOverlay Get(XamlRoot root)
		=> _overlays.GetValue(root, static r => new DiagnosticsOverlay(r));

	private readonly XamlRoot _root;
	private readonly object _updateGate = new();
	private readonly List<IDiagnosticView> _localProviders = new();
	private readonly Dictionary<IDiagnosticView, DiagnosticElement> _elements = new();

	private DispatcherQueue? _dispatcher;
	private Context? _context;
	private Popup? _overlayHost;
	private bool _isVisible;
	private bool _isExpanded;
	private int _updateEnqueued;
	private Panel? _elementsPanel;
	private UIElement? _anchor;
	private ContentPresenter? _notificationPresenter;

	static DiagnosticsOverlay()
	{
		DiagnosticViewRegistry.Added += static (snd, e) =>
		{
			foreach (var overlay in _overlays)
			{
				overlay.Value.EnqueueUpdate();
			}
		};
	}

	private DiagnosticsOverlay(XamlRoot root)
	{
		_root = root;
		_dispatcher = root.Content?.DispatcherQueue;
		_context = _dispatcher is null ? null : new Context(this, _dispatcher);

		root.Changed += static (snd, e) =>
		{
			var overlay = Get(snd);
			var dispatcher = snd.Content?.DispatcherQueue;
			if (dispatcher != overlay._dispatcher) // Is this even possible ???
			{
				lock (overlay._updateGate)
				{
					overlay._dispatcher = dispatcher;
					overlay._context = dispatcher is null ? null : new Context(overlay, dispatcher);

					// Clean all dispatcher bound state
					overlay._overlayHost = null;
					overlay._elementsPanel = null;
					foreach (var element in overlay._elements.Values)
					{
						element.Dispose();
					}
					overlay._elements.Clear();
				}
			}
			overlay.EnqueueUpdate();
		};

		DefaultStyleKey = typeof(DiagnosticsOverlay);
	}

	/// <summary>
	/// Make the overlay visible.
	/// </summary>
	/// <remarks>This can be invoked from any thread.</remarks>>
	public void Show(bool isExpanded = false)
	{
		_isVisible = true;
		EnqueueUpdate();
	}

	/// <summary>
	/// Hide the overlay.
	/// </summary>
	/// <remarks>This can be invoked from any thread.</remarks>>
	public void Hide()
	{
		_isVisible = false;
		EnqueueUpdate(forceUpdate: true);
	}

	/// <summary>
	/// Add a UI diagnostic element to this overlay.
	/// </summary>
	/// <remarks>This will also make this overlay visible (cf. <see cref="Show"/>).</remarks>
	public void Add(string id, string name, UIElement preview, Func<UIElement>? details = null)
		=> Add(new DiagnosticView(id, name, _ => preview, (_, ct) => new(details?.Invoke())));

	/// <summary>
	/// Add a UI diagnostic element to this overlay.
	/// </summary>
	/// <remarks>This will also make this overlay visible (cf. <see cref="Show"/>).</remarks>
	/// <param name="provider">The provider to add.</param>
	public void Add(IDiagnosticView provider)
	{
		lock (_updateGate)
		{
			_localProviders.Add(provider);
		}

		EnqueueUpdate(); // Making IsVisible = true wil (try) to re-enqueue the update, but safer to keep it here anyway.
		Show();
	}

	/// <inheritdoc />
	protected override void OnVisibilityChanged(Visibility oldValue, Visibility newValue)
	{
		base.OnVisibilityChanged(oldValue, newValue);

		EnqueueUpdate(forceUpdate: newValue is not Visibility.Visible); // Force update when hiding.
	}

	/// <inheritdoc />
	protected override void OnApplyTemplate()
	{
		if (_anchor is not null)
		{
			_anchor.Tapped -= OnAnchorTapped;
			_anchor.ManipulationDelta -= OnAnchorManipulated;
		}
		if (_notificationPresenter is not null)
		{
			_notificationPresenter.Tapped -= OnNotificationTapped;
		}

		base.OnApplyTemplate();

		_elementsPanel = GetTemplateChild<Panel>(ElementsPanelPartName);
		_anchor = GetTemplateChild<UIElement>(AnchorPartName);
		_notificationPresenter = GetTemplateChild<ContentPresenter>(NotificationPartName);

		if (_anchor is not null)
		{
			_anchor.Tapped += OnAnchorTapped;
			_anchor.ManipulationDelta += OnAnchorManipulated;
			_anchor.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY | ManipulationModes.TranslateInertia;
		}
		if (_notificationPresenter is not null)
		{
			_notificationPresenter.Tapped += OnNotificationTapped;
		}

		VisualStateManager.GoToState(this, _isExpanded ? DisplayModeExpandedStateName : DisplayModeCompactStateName, false);
		VisualStateManager.GoToState(this, NotificationCollapsedStateName, false);
		EnqueueUpdate();
	}

	private void OnAnchorTapped(object sender, TappedRoutedEventArgs args)
	{
		_isExpanded = !_isExpanded;
		VisualStateManager.GoToState(this, _isExpanded ? DisplayModeExpandedStateName : DisplayModeCompactStateName, true);
		args.Handled = true;
	}

	private void OnAnchorManipulated(object sender, ManipulationDeltaRoutedEventArgs e)
	{
		var transform = RenderTransform as TranslateTransform;
		if (transform is null)
		{
			RenderTransform = transform = new TranslateTransform();
		}

		transform.X += e.Delta.Translation.X;
		transform.Y += e.Delta.Translation.Y;
	}

	private void EnqueueUpdate(bool forceUpdate = false)
	{
		var dispatcher = _dispatcher;
		var isHidden = !_isVisible;
		if ((isHidden && !forceUpdate)
			|| dispatcher is null
			|| Interlocked.CompareExchange(ref _updateEnqueued, 1, 0) is not 0)
		{
			return;
		}

		dispatcher.TryEnqueue(() =>
		{
			_updateEnqueued = 0;

			if (isHidden || Visibility is not Visibility.Visible)
			{
				if (_overlayHost is { } h)
				{
					ShowHost(h, false);
				}

				return;
			}

			// If the _elementsPanel is null, we need to let layout pass to get it.
			var host = _overlayHost ??= CreateHost(_root, this);
			if (_elementsPanel is null)
			{
				// Once injected in the visual tree (cf. CreateHost), we try to force the template to be applied.
				ApplyTemplate();
				if (_elementsPanel is null)
				{
					// Template still not applied, we'll try again later (OnApplyTemplate will invoke back EnqueueUpdate).
					ShowHost(host, true);
					return;
				}
			}

			lock (_updateGate)
			{
				var providers = DiagnosticViewRegistry
					.Registrations
					.Where(ShouldMaterialize)
					.Select(reg => reg.Provider)
					.Concat(_localProviders)
					.Distinct()
					.ToList();

				foreach (var provider in providers)
				{
					if (!_elements.ContainsKey(provider))
					{
						var element = new DiagnosticElement(this, provider, _context!);
						_elements[provider] = element;

						_elementsPanel.Children.Add(element.Preview);
					}
				}

				ShowHost(host, true);
			}
		});
	}

	private static Popup CreateHost(XamlRoot root, DiagnosticsOverlay overlay)
		=> new()
		{
			XamlRoot = root,
			Child = overlay,
			IsLightDismissEnabled = false,
			LightDismissOverlayMode = LightDismissOverlayMode.Off,
		};

	private static void ShowHost(Popup host, bool isVisible)
		=> host.IsOpen = isVisible;

	private bool ShouldMaterialize(DiagnosticViewRegistration registration)
		=> registration.Mode switch
		{
			DiagnosticViewRegistrationMode.All => true,
			DiagnosticViewRegistrationMode.OnDemand => false,
			_ => _overlays.Count(overlay => overlay.Value.IsMaterialized(registration.Provider)) is 0
		};

	private bool IsMaterialized(IDiagnosticView provider)
	{
		lock (_updateGate)
		{
			return _elements.ContainsKey(provider);
		}
	}
}
#endif
