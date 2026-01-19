using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml.Input;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.DataBinding;
using Windows.Foundation;
using Windows.System;

#if HAS_UNO_WINUI
using _WindowActivatedEventArgs = Microsoft.UI.Xaml.WindowActivatedEventArgs;
#else
using _WindowActivatedEventArgs = Windows.UI.Core.WindowActivatedEventArgs;
#endif

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
			if (!node.Value.IsDisposed && node.Value.Target is Popup { IsLightDismissEnabled: true } popup)
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

	internal IDisposable RegisterOpenPopup(IPopup popup)
	{
		CleanupPopupReferences();

		var popupRegistration = _openPopups.FirstOrDefault(
			p => !p.IsDisposed && p.Target == popup);

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
			if (node.Value.IsDisposed || node.Value.Target is null)
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
