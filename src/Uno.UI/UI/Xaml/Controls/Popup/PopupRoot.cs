using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.DataBinding;
using Windows.Foundation;
using Windows.System;

using _WindowActivatedEventArgs = Microsoft.UI.Xaml.WindowActivatedEventArgs;

namespace Microsoft.UI.Xaml.Controls.Primitives;

internal partial class PopupRoot : Canvas
{
	// A linked list of open popups. The most recently opened is at the head.
	private readonly LinkedList<ManagedWeakReference> _openPopups = new();

	private readonly SerialDisposable _subscriptions = new();

	public PopupRoot()
	{
		KeyDown += OnKeyDown;
		Loaded += OnRootLoaded;
		Unloaded += OnRootUnloaded;

		// See https://github.com/unoplatform/uno/issues/16358#issuecomment-2115276460
		// This is a hack to prevent Unfocus from being called.
		PointerReleased += (_, e) => e.Handled = true;
	}

	private void OnRootLoaded(object sender, RoutedEventArgs args)
	{
		if (XamlRoot is { } xamlRoot)
		{
			void OnChanged(object sender, object args) => CloseLightDismissablePopups();

			CompositeDisposable disposables = new();
			xamlRoot.Changed += OnChanged;
			disposables.Add(() => xamlRoot.Changed -= OnChanged);

			if (xamlRoot.HostWindow is { } window)
			{
				window.Activated += OnWindowActivated;
				disposables.Add(() => window.Activated -= OnWindowActivated);
			}

			_subscriptions.Disposable = disposables;
		}
	}

	private void OnWindowActivated(object sender, _WindowActivatedEventArgs e)
	{
		if (FeatureConfiguration.Popup.PreventLightDismissOnWindowDeactivated)
		{
			return;
		}

		CloseLightDismissablePopups();
	}

	private void OnRootUnloaded(object sender, RoutedEventArgs args)
	{
		_subscriptions.Disposable = null;
	}

	internal void CloseLightDismissablePopups()
	{
		var node = _openPopups.First;
		while (node != null)
		{
			var next = node.Next;
			if (node.Value.TryGetTarget<Popup>(out var popup) && popup.IsLightDismissEnabled)
			{
				if (popup.AssociatedFlyout is { } flyout)
				{
					flyout.Hide();
				}
				else
				{
					popup.IsOpen = false;
				}
			}
			node = next;
		}
	}

#if UNO_HAS_ENHANCED_LIFECYCLE
	// MUX Reference: Popup.cpp CPopupRoot::NotifyThemeChanged (lines 5339-5374)
	// PopupRoot should NEVER store a theme. Its GetTheme() must always return
	// Theme.None so that popup content entering the visual tree under PopupRoot
	// does not accidentally inherit PopupRoot's theme during deferred Loading.
	// Instead, PopupRoot only sets a flag and propagates to open popups.
	//
	// In WinUI, the ASSERT is: ASSERT(GetTheme() == Theming::Theme::None);
	// MUX Reference: Popup.h m_hasThemeChanged
	// "Has theme ever changed from startup theme?" — used when popups are
	// added to the open list (CompleteAdditionToOpenPopupList) to notify
	// non-parented popups that missed the theme walk while they were closed.
	private bool _hasThemeChanged;

	private protected override void NotifyThemeChangedCore(Theme theme, bool forceRefresh)
	{
		// Do NOT call base — PopupRoot must not update its own theme bindings,
		// propagate to visual children (PopupPanels), or persist a theme.
		// Open popups are handled exclusively via the open-popup list below.
		_hasThemeChanged = true;

		// Propagate theme to all open popups
		var node = _openPopups.First;
		while (node != null)
		{
			var next = node.Next;
			if (node.Value.TryGetTarget<Popup>(out var popup))
			{
				// MUX Reference: Popup.cpp ShouldPopupRootNotifyThemeChange (lines 3551-3563)
				// Skip parented popups — they receive theme from their parent walk.
				// Only notify popups whose visual parent is null or PopupRoot itself.
				if (VisualTreeHelper.GetParent(popup) is null or PopupRoot)
				{
					popup.NotifyThemeChanged(theme, forceRefresh);
				}
			}
			node = next;
		}
	}
#endif

	protected override void OnChildrenChanged()
	{
		base.OnChildrenChanged();
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		Size size = default;
		foreach (var child in Children)
		{
			if (!(child is PopupPanel))
			{
				continue;
			}
			// Note that we should always be arranged with the full size of the window, so we don't care too much about the return value here.
			size = MeasureElement(child, availableSize);
		}
		return size;
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		foreach (var child in Children)
		{
			if (!(child is PopupPanel panel))
			{
				continue;
			}

			child.EnsureLayoutStorage();
			// Note: The popup alignment is ensure by the PopupPanel itself
			ArrangeElement(child, new Rect(new Point(), finalSize));
		}

		return finalSize;
	}

	internal IDisposable OpenPopup(Popup popup)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"Opening popup");
		}

		var popupPanel = popup.PopupPanel;
		Children.Add(popupPanel);
		var disposable = RegisterOpenPopup(popup);

#if UNO_HAS_ENHANCED_LIFECYCLE
		// MUX Reference: CPopupRoot::CompleteAdditionToOpenPopupList (lines 4289-4302)
		// If app's theme has changed since startup, notify non-parented popups
		// that missed the theme walk while they were closed.
		// Parented popups (ShouldPopupRootNotifyThemeChange == false) already
		// received the theme from their parent's walk.
		if (_hasThemeChanged && ShouldPopupRootNotifyThemeChange(popup))
		{
			var appTheme = Application.Current?.RequestedTheme == ApplicationTheme.Dark
				? Theme.Dark : Theme.Light;
			popup.NotifyThemeChanged(appTheme);
		}
#endif

		return Disposable.Create(() =>
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Closing popup");
			}

			Children.Remove(popupPanel);

			disposable.Dispose();
		});
	}

	// MUX Reference: CPopup::ShouldPopupRootNotifyThemeChange (lines 3551-3563)
	// A parented popup gets theme change notification from its parent walk.
	// Only non-parented popups (visual parent is null or PopupRoot) need
	// explicit notification from PopupRoot.
	private static bool ShouldPopupRootNotifyThemeChange(Popup popup)
		=> VisualTreeHelper.GetParent(popup) is null or PopupRoot;

	internal IDisposable RegisterOpenPopup(IPopup popup)
	{
		CleanupPopupReferences();

		var popupRegistration = _openPopups.FirstOrDefault(
			p => p.TryGetTarget<IPopup>(out var target) && target == popup);

		if (popupRegistration is null)
		{
			popupRegistration = WeakReferencePool.RentWeakReference(popup, popup);

			// Insert at head so the most recently opened popup is first
			_openPopups.AddFirst(popupRegistration);
		}

		return Disposable.Create(() => _openPopups.Remove(popupRegistration));
	}

	private void CleanupPopupReferences()
	{
		var node = _openPopups.First;
		while (node != null)
		{
			var next = node.Next;
			if (!node.Value.TryGetTarget<object>(out _))
			{
				_openPopups.Remove(node);
			}
			node = next;
		}
	}

	// The ESC key closes the topmost light-dismiss-enabled popup.
	// Handling must be done by CPopupRoot because the popups reparent their children to be under CPopupRoot,
	// so routed events from beneanth the popups route to CPopupRoot and skip the popups themselves.
	protected void OnKeyDown(object sender, KeyRoutedEventArgs args)
	{
		if (args.Key == VirtualKey.Escape)
		{
			var didCloseAPopup = CloseTopmostPopup(FocusState.Keyboard, PopupFilter.LightDismissOrFlyout);
			args.Handled = didCloseAPopup;
		}
	}
}
