#nullable enable
#if WINUI || HAS_UNO_WINUI
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

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
	private readonly List<IDiagnosticView> _localRegistrations = new();
	private readonly Dictionary<IDiagnosticView, DiagnosticElement> _elements = new();
	private readonly Dictionary<string, bool> _configuredElementVisibilities = new();

	private ViewContext? _context;
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
		_context = ViewContext.TryCreate(this);

		root.Changed += static (snd, e) =>
		{
			var overlay = Get(snd);
			var context = ViewContext.TryCreate(overlay);
			if (context != overlay._context) // I.e. dispatcher changed ... is this even possible ???
			{
				lock (overlay._updateGate)
				{
					overlay._context = context;

					// Clean all dispatcher bound states
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
	public void Show(bool? isExpanded = false)
	{
		_isVisible = true;
		if (isExpanded is not null)
		{
			_isExpanded = isExpanded.Value;
		}
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
	/// Hide the given view from the overlay.
	/// </summary>
	/// <param name="viewId"><see cref="IDiagnosticView.Id"/> of the <see cref="IDiagnosticView"/> to hide.</param>
	public void Hide(string viewId)
	{
		lock (_updateGate)
		{
			_configuredElementVisibilities[viewId] = false;
		}
		EnqueueUpdate();
	}

	/// <summary>
	/// Hide the given view from the overlay.
	/// </summary>
	/// <param name="viewId"><see cref="IDiagnosticView.Id"/> of the <see cref="IDiagnosticView"/> to hide.</param>
	/// <remarks>This will also make this overlay visible (cf. <see cref="Show(bool?)"/>).</remarks>
	public void Show(string viewId)
	{
		lock (_updateGate)
		{
			_configuredElementVisibilities[viewId] = true;
		}
		Show();
	}

	/// <summary>
	/// Add a UI diagnostic element to this overlay.
	/// </summary>
	/// <remarks>This will also make this overlay visible (cf. <see cref="Show(bool?)"/>).</remarks>
	public void Add(string id, string name, UIElement preview, Func<UIElement>? details = null)
		=> Add(new DiagnosticView(id, name, _ => preview, (_, ct) => new(details?.Invoke())));

	/// <summary>
	/// Add a UI diagnostic element to this overlay.
	/// </summary>
	/// <remarks>This will also make this overlay visible (cf. <see cref="Show(bool?)"/>).</remarks>
	/// <param name="provider">The provider to add.</param>
	public void Add(IDiagnosticView provider)
	{
		lock (_updateGate)
		{
			_localRegistrations.Add(provider);
		}

		EnqueueUpdate(); // Making IsVisible = true wil (try) to re-enqueue the update, but safer to keep it here anyway.
		Show();
	}

	public UIElement? Find(string viewId)
		=> _elements.Values.FirstOrDefault(elt => elt.View.Id == viewId)?.Value;

#if HAS_UNO
	/// <inheritdoc />
	protected override void OnVisibilityChanged(Visibility oldValue, Visibility newValue)
	{
		base.OnVisibilityChanged(oldValue, newValue);

		EnqueueUpdate(forceUpdate: newValue is not Visibility.Visible); // Force update when hiding.
	}
#endif

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

		_elementsPanel = GetTemplateChild(ElementsPanelPartName) as Panel;
		_anchor = GetTemplateChild(AnchorPartName) as UIElement;
		_notificationPresenter = GetTemplateChild(NotificationPartName) as ContentPresenter;

		if (_anchor is not null)
		{
			_anchor.Tapped += OnAnchorTapped;
			_anchor.ManipulationDelta += OnAnchorManipulated;
			_anchor.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY | ManipulationModes.TranslateInertia;
			RenderTransform = new TranslateTransform { X = 15, Y = 15 };
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
		var context = _context;
		var isHidden = !_isVisible;
		if ((isHidden && !forceUpdate)
			|| context is null
			|| Interlocked.CompareExchange(ref _updateEnqueued, 1, 0) is not 0)
		{
			return;
		}

		context.Schedule(() =>
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

			var visibleViews = 0;
			lock (_updateGate)
			{
				var viewsThatShouldBeMaterialized = DiagnosticViewRegistry
					.Registrations
					.Where(ShouldMaterialize)
					.Select(reg => reg.View)
					.Concat(_localRegistrations)
					.Distinct()
					.ToList();

				foreach (var view in viewsThatShouldBeMaterialized)
				{
					ref var element = ref CollectionsMarshal.GetValueRefOrAddDefault(_elements, view, out var hasElement);
					if (!hasElement)
					{
						element = new DiagnosticElement(this, view, _context!);
						_elementsPanel.Children.Add(element.Value);
					}
				}

				foreach (var element in _elements.Values)
				{
					if (_configuredElementVisibilities.GetValueOrDefault(element.View.Id, true))
					{
						var currentIndex = _elementsPanel.Children.IndexOf(element.Value);
						if (currentIndex is -1)
						{
							_elementsPanel.Children.Insert(visibleViews, element.Value);
						}
						else if (currentIndex != visibleViews)
						{
							global::System.Diagnostics.Debug.Fail("Invalid index, patching");
							_elementsPanel.Children.Move((uint)currentIndex, (uint)visibleViews);
						}

						visibleViews++;
					}
					else
					{
						_elementsPanel.Children.Remove(element.Value);
					}
				}
			}

			ShowHost(host, isVisible: visibleViews is not 0);
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
	{
		if (_configuredElementVisibilities.TryGetValue(registration.View.Id, out var isVisible) && isVisible)
		{
			// We explicitly requested to show that view, so yes we have to materialize it!
			return true;
		}

		return registration.Mode switch
		{
			DiagnosticViewRegistrationMode.All => true,
			DiagnosticViewRegistrationMode.OnDemand => false,
			_ => _overlays.Count(overlay => overlay.Value.IsMaterialized(registration.View)) is 0
		};
	}

	private bool IsMaterialized(IDiagnosticView provider)
	{
		lock (_updateGate)
		{
			return _elements.ContainsKey(provider);
		}
	}
}
#endif
