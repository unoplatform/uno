using System;
using Uno.UI;
using Uno.UI.DataBinding;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

#if __IOS__
using CoreGraphics;
using UIKit;
#elif __MACOS__
using CoreGraphics;
using AppKit;
#endif

namespace Microsoft.UI.Xaml.Controls.Primitives;

public partial class Popup : FrameworkElement, IPopup
{
	private ManagedWeakReference _lastFocusedElement;
	private FocusState _lastFocusState = FocusState.Unfocused;
	private IDisposable _openPopupRegistration;

	public event EventHandler<object> Closed;
	public event EventHandler<object> Opened;

	/// <summary>
	/// Defines a custom layouter which overrides the default placement logic of the <see cref="PopupPanel"/>
	/// </summary>
	internal IDynamicPopupLayouter CustomLayouter { get; set; }

	/// <summary>
	/// Controls whether the Popup should propagate its own DataContext to its Child.
	///
	/// This is particularly useful when the child is a direct dependency of an entered UIElement
	/// while the popup is not (e.g. ToolTip created through ToolTipService)
	/// </summary>
	internal bool PropagatesDataContextToChild { get; set; } = true;

	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		if (args.Property == AllowFocusOnInteractionProperty ||
			args.Property == AllowFocusWhenDisabledProperty)
		{
			PropagateFocusProperties();
		}

		base.OnPropertyChanged2(args);
	}

	private protected override void OnUnloaded()
	{
		IsOpen = false;
		OnUnloadedPartial();
		base.OnUnloaded();
	}

	partial void OnUnloadedPartial();

	/// <inheritdoc />
	protected override Size MeasureOverride(Size availableSize)
	{
		// As the Child is NOT part of the visual tree, it does not have to be measured
		return new Size(Width, Height).FiniteOrDefault(default);
	}

	/// <inheritdoc />
	protected override Size ArrangeOverride(Size finalSize)
	{
		// As the Child is NOT part of the visual tree, it does not have to be arranged,
		// but we need to manually propagate Translation
		PopupPanel.Translation = Translation;
		return finalSize;
	}

	partial void OnIsOpenChangedPartial(bool oldIsOpen, bool newIsOpen)
	{
		if (newIsOpen)
		{
			var xamlRoot = XamlRoot ?? Child?.XamlRoot ?? WinUICoreServices.Instance.ContentRootCoordinator.CoreWindowContentRoot?.XamlRoot;

			if (xamlRoot != XamlRoot)
			{
				XamlRoot = xamlRoot;
			}

			if (xamlRoot is not null)
			{
				_openPopupRegistration = xamlRoot.VisualTree.PopupRoot.RegisterOpenPopup(this);
			}

			if (IsLightDismissEnabled || AssociatedFlyout is { })
			{
				// Store last focused element
				var focusManager = VisualTree.GetFocusManagerForElement(this);
				var focusedElement = focusManager?.FocusedElement as UIElement;
				var focusState = focusManager?.GetRealFocusStateForFocusedElement() ?? FocusState.Unfocused;
				if (focusedElement != null && focusState != FocusState.Unfocused)
				{
					_lastFocusedElement = WeakReferencePool.RentWeakReference(this, focusedElement);
					_lastFocusState = focusState;
				}

				// Usually, FrameworkElements handle focus management inside OnLoaded/OnUnloaded,
				// but since popups are (un)loaded, we have to do it here.
				if (Child is FrameworkElement fw && fw.AllowFocusOnInteraction)
				{
					// Give the child focus if allowed
					Focus(FocusState.Programmatic);
				}
			}
		}
		else
		{
			_openPopupRegistration?.Dispose();
			if (IsLightDismissEnabled)
			{
				if (_lastFocusedElement != null && _lastFocusedElement.Target is UIElement target)
				{
					target.Focus(_lastFocusState);
					_lastFocusedElement = null;
				}
			}
		}
	}

	partial void OnChildChangedPartial(UIElement oldChild, UIElement newChild)
	{
		if (oldChild is IDependencyObjectStoreProvider provider &&
			provider.Store.GetValue(provider.Store.DataContextProperty, DependencyPropertyValuePrecedences.Local, true) != DependencyProperty.UnsetValue)
		{
			provider.Store.ClearValue(provider.Store.TemplatedParentProperty, DependencyPropertyValuePrecedences.Local);
			provider.Store.ClearValue(AllowFocusOnInteractionProperty, DependencyPropertyValuePrecedences.Local);
			provider.Store.ClearValue(AllowFocusWhenDisabledProperty, DependencyPropertyValuePrecedences.Local);
		}

		UpdateDataContext(null);
		UpdateTemplatedParent();
		PropagateFocusProperties();
	}

	protected internal override void OnDataContextChanged(DependencyPropertyChangedEventArgs e)
	{
		base.OnDataContextChanged(e);

		UpdateDataContext(e);
	}

	protected internal override void OnTemplatedParentChanged(DependencyPropertyChangedEventArgs e)
	{
		base.OnTemplatedParentChanged(e);

		UpdateTemplatedParent();
	}

	private void UpdateDataContext(DependencyPropertyChangedEventArgs e)
	{
		if (PropagatesDataContextToChild)
		{
			((IDependencyObjectStoreProvider)PopupPanel).Store.SetValue(((IDependencyObjectStoreProvider)PopupPanel).Store.DataContextProperty, DataContext, DependencyPropertyValuePrecedences.Local);
		}
	}

	private void UpdateTemplatedParent()
	{
		if (Child is IDependencyObjectStoreProvider provider)
		{
			provider.Store.SetValue(provider.Store.TemplatedParentProperty, this.TemplatedParent, DependencyPropertyValuePrecedences.Local);
		}
	}

	private void PropagateFocusProperties()
	{
		if (Child is IDependencyObjectStoreProvider provider)
		{
			provider.Store.SetValue(AllowFocusOnInteractionProperty, AllowFocusOnInteraction, DependencyPropertyValuePrecedences.Local);
			provider.Store.SetValue(AllowFocusWhenDisabledProperty, AllowFocusWhenDisabled, DependencyPropertyValuePrecedences.Local);
		}
	}

	/// <summary>
	/// A layouter responsible to layout the content of a popup at the right place
	/// </summary>
	internal interface IDynamicPopupLayouter
	{
		/// <summary>
		/// Measure the content of the popup
		/// </summary>
		/// <param name="available">The available size to place to render the popup. This is expected to be the screen size.</param>
		/// <param name="visibleSize">The size of the visible bounds of the window. This is expected to be AtMost the available.</param>
		/// <returns>The desired size to render the content</returns>
		Size Measure(Size available, Size visibleSize);

		/// <summary>
		/// Render the content of the popup at its final location
		/// </summary>
		/// <param name="finalSize">The final size available to render the view. This is expected to be the screen size.</param>
		/// <param name="visibleBounds">The frame of the visible bounds of the window. This is expected to be AtMost the finalSize.</param>
		/// <param name="desiredSize">The size at which the content expect to be rendered. This is the result of the last <see cref="Measure"/>.</param>
		/// <param name="upperLeftOffset">Coordinate system adjustment, applied to the resulting frame computed from the popup content</param>
		void Arrange(Size finalSize, Rect visibleBounds, Size desiredSize);
	}

	partial void OnIsLightDismissEnabledChangedPartial(bool oldIsLightDismissEnabled, bool newIsLightDismissEnabled)
	{
	}

	event EventHandler<object> IPopup.Closed
	{
		add => Closed += value;
		remove => Closed -= value;
	}

	event EventHandler<object> IPopup.Opened
	{
		add => Opened += value;
		remove => Opened -= value;
	}

	bool IPopup.IsOpen
	{
		get => IsOpen;
		set => IsOpen = value;
	}

	UIElement IPopup.Child
	{
		get => Child;
		set => Child = value;
	}
}
