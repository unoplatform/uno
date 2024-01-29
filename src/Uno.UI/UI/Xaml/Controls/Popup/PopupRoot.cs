using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.DataBinding;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Islands;
using Windows.Foundation;
using Windows.System;
using Microsoft.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Controls.Primitives;

internal partial class PopupRoot : Panel
{
	private readonly List<ManagedWeakReference> _openPopups = new();

	private readonly SerialDisposable _subscriptions = new();

	public PopupRoot()
	{
		KeyDown += OnKeyDown;
		Loaded += OnRootLoaded;
		Unloaded += OnRootUnloaded;
	}

	private void OnRootLoaded(object sender, RoutedEventArgs args)
	{
		if (XamlRoot is { } xamlRoot)
		{
			void OnChanged(object sender, object args) => CloseFlyouts();

			CompositeDisposable disposables = new();
			xamlRoot.Changed += OnChanged;
			disposables.Add(() => xamlRoot.Changed -= OnChanged);

			if (xamlRoot.HostWindow is { } window)
			{
				window.Activated += OnChanged;
				disposables.Add(() => window.Activated -= OnChanged);
			}

			_subscriptions.Disposable = disposables;
		}
	}

	private void OnRootUnloaded(object sender, RoutedEventArgs args)
	{
		_subscriptions.Disposable = null;
	}

	private void CloseFlyouts()
	{
		for (var i = _openPopups.Count - 1; i >= 0; i--)
		{
			var reference = _openPopups[i];
			if (!reference.IsDisposed && reference.Target is Popup { IsForFlyout: true } popup)
			{
				var f = popup.AssociatedFlyout;
				f.Hide();
			}
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

			_openPopups.Add(popupRegistration);
		}

		return Disposable.Create(() => _openPopups.Remove(popupRegistration));
	}

	private void CleanupPopupReferences()
	{
		for (int i = _openPopups.Count - 1; i >= 0; i--)
		{
			if (_openPopups[i].IsDisposed || _openPopups[i].Target is null)
			{
				_openPopups.RemoveAt(i);
			}
		}
	}

	protected void OnKeyDown(object sender, KeyRoutedEventArgs args)
	{
		if (args.Key == VirtualKey.Escape)
		{
			var popup = GetTopmostPopup(PopupFilter.LightDismissOrFlyout);
			if (popup is { })
			{
				popup.IsOpen = false;
				args.Handled = popup.IsOpen;
			}
		}
	}
}
