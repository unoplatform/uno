using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

using Uno;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Helpers.Boxes;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Input;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Uno.UI.DataBinding;
using Uno.UI.Xaml.Core;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;
using System.Runtime.CompilerServices;
using Microsoft.UI.Dispatching;
using Windows.System;
using View = Microsoft.UI.Xaml.UIElement;

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	public partial class FlyoutBase : DependencyObject
	{
		public event EventHandler<object> Opened;
		public event EventHandler<object> Closed;
		public event EventHandler<object> Opening;
		public event TypedEventHandler<FlyoutBase, FlyoutBaseClosingEventArgs> Closing;

		private static readonly List<FlyoutBase> _openFlyouts = new List<FlyoutBase>();

		private bool _isClosedPending;

		protected internal Popup _popup;
		private bool _isLightDismissEnabled = true;

		// MUX Reference: FlyoutBase_partial.h:359 — m_isFlyoutPresenterRequestedThemeOverridden.
		// Element-level theme forwarding is a Skia/WASM-only feature (native flyouts follow the
		// application/OS theme), so this flag is gated alongside ForwardThemeToPresenter.
		private bool m_isFlyoutPresenterRequestedThemeOverridden;

		/// <summary>
		/// Whether touch was the input device that opened this flyout. Set before <see cref="Opening"/> fires.
		/// </summary>
		private protected bool WasOpenedByTouch => m_inputDeviceTypeUsedToOpen == InputDeviceType.Touch;

		protected FlyoutBase()
		{
		}

		/// <summary>
		/// Propagates Enter to the flyout's content for keyboard accelerator registration.
		/// Subclasses (Flyout, MenuFlyout) override to propagate to their specific content.
		/// </summary>
		internal new virtual void Enter(DependencyObject pNamescopeOwner, EnterParams @params)
		{
		}

		/// <summary>
		/// Propagates Leave to the flyout's content for keyboard accelerator unregistration.
		/// Subclasses (Flyout, MenuFlyout) override to propagate to their specific content.
		/// </summary>
		internal new virtual void Leave(DependencyObject pNamescopeOwner, LeaveParams @params)
		{
		}

		internal static IReadOnlyList<FlyoutBase> OpenFlyouts => _openFlyouts.AsReadOnly();

		// MUX Reference FlyoutBase_partial.cpp, lines 1357-1430
		internal void EnsurePopupAndPresenter()
		{
			if (_popup == null)
			{
				var child = CreatePresenter();

				if (child is FlyoutPresenter flyoutPresenter)
				{
					flyoutPresenter.SetOwnerFlyout(this);
				}

				_popup = new Popup()
				{
					Child = child,
					IsLightDismissEnabled = _isLightDismissEnabled,
					AssociatedFlyout = this,
				};

				_popup.ShouldConstrainToRootBounds = ShouldConstrainToRootBounds;

				_popup.Closed += OnPopupClosed;
				_popup.LostFocus += OnPopupLostFocus;

				m_tpPresenter = child;
				PreparePresenter(child);

				ReevaluateIsOverlayVisible();

				// The popup deliberately carries no DataContext (WinUI parity): a flyout has none, and forwarding the
				// owner's DataContext onto the kept-alive popup leaks it. The placement target's DataContext is set on
				// the presenter at show-time (ForwardTargetPropertiesToPresenter) and cleared on close instead.
				SynchronizePropertyToPopup(Popup.AllowFocusOnInteractionProperty, Boxer.Box(AllowFocusOnInteraction));
				SynchronizePropertyToPopup(Popup.AllowFocusWhenDisabledProperty, Boxer.Box(AllowFocusWhenDisabled));
			}
		}

		private void OnPresenterLoaded(object sender, RoutedEventArgs args)
		{
			var allowFocusOnInteraction = AllowFocusOnInteraction;
			DependencyObject target = this;

			if (allowFocusOnInteraction && Target is { } t)
			{
				allowFocusOnInteraction = t.AllowFocusOnInteraction;
				target = t;
			}

			var contentRoot = VisualTree.GetContentRootForElement(target);

			var focusState = contentRoot.FocusManager.GetRealFocusStateForFocusedElement();

			if (m_shouldTakeFocus && focusState != FocusState.Unfocused)
			{
				var presenter = GetPresenter();
				if (presenter.AllowFocusOnInteraction && _popup?.AssociatedFlyout.AllowFocusOnInteraction is true)
				{
					var childFocused = presenter.Focus(focusState);

					if (!childFocused)
					{
						_popup.Focus(focusState);
					}
				}
			}

			OnOpened();

			// TODO Uno: A reopened presenter that keeps its size raises no SizeChanged, as its size survives closing.
			if (_popup?.IsOpen == true && !m_presenterResized)
			{
				PerformPlacement(GetPresenterSize());
			}
		}

		private protected bool IsLightDismissOverlayEnabled
		{
			get => _isLightDismissEnabled;
			set
			{
				_isLightDismissEnabled = value;

				if (_popup != null)
				{
					_popup.IsLightDismissEnabled = value;
					ReevaluateIsOverlayVisible();
				}
			}
		}

		public bool IsOpen
		{
			get => (bool)GetValue(IsOpenProperty);
			private set => SetValue(IsOpenProperty, value);
		}
		public static DependencyProperty IsOpenProperty { get; } =
			DependencyProperty.Register(
				nameof(IsOpen), typeof(bool),
				typeof(FlyoutBase),
				new FrameworkPropertyMetadata(BoolBoxes.False));

		/// <summary>
		/// Gets a value that indicates whether the flyout is shown within the bounds of the XAML root.
		/// </summary>
		public bool IsConstrainedToRootBounds
		{
			get
			{
				EnsurePopupAndPresenter();
				return _popup.IsConstrainedToRootBounds;
			}
		}

		/// <summary>
		/// Gets or sets a value that indicates whether the flyout should be shown within the bounds of the XAML root.
		/// </summary>
		public bool ShouldConstrainToRootBounds
		{
			get => (bool)GetValue(ShouldConstrainToRootBoundsProperty);
			set => SetValue(ShouldConstrainToRootBoundsProperty, Boxer.Box(value));
		}

		/// <summary>
		/// Identifies the ShouldConstrainToRootBounds dependency property.
		/// </summary>
		public static DependencyProperty ShouldConstrainToRootBoundsProperty { get; } =
			DependencyProperty.Register(
				nameof(ShouldConstrainToRootBounds),
				typeof(bool),
				typeof(FlyoutBase),
				new FrameworkPropertyMetadata(BoolBoxes.True));

		// MUX Reference DependencyProperty.cpp, CDependencyProperty::GetDefaultValue, tag winui3/release/2.5.1, commit ba3a8d59e
		internal override bool GetDefaultValue2(DependencyProperty property, out object defaultValue)
		{
			if (property == ShouldConstrainToRootBoundsProperty)
			{
				// MenuFlyouts aren't constrained by default.
				defaultValue = this is MenuFlyout ? BoolBoxes.False : BoolBoxes.True;
				return true;
			}

			return base.GetDefaultValue2(property, out defaultValue);
		}

		#region Placement

		/// <summary>
		/// Preferred placement of the flyout.
		/// </summary>
		/// <remarks>
		/// If there's not enough place, the following logic will be used:
		/// https://docs.microsoft.com/en-us/previous-versions/windows/apps/dn308515(v=win.10)#placing-a-flyout
		/// </remarks>
		public FlyoutPlacementMode Placement
		{
			get { return (FlyoutPlacementMode)GetValue(PlacementProperty); }
			set { SetValue(PlacementProperty, value); }
		}

		public static DependencyProperty PlacementProperty { get; } =
			DependencyProperty.Register(
				"Placement",
				typeof(FlyoutPlacementMode),
				typeof(FlyoutBase),
				new FrameworkPropertyMetadata(default(FlyoutPlacementMode))
			);

		#endregion

		public LightDismissOverlayMode LightDismissOverlayMode
		{
			get
			{
				return (LightDismissOverlayMode)this.GetValue(LightDismissOverlayModeProperty);
			}
			set
			{
				this.SetValue(LightDismissOverlayModeProperty, value);
			}
		}

		public static DependencyProperty LightDismissOverlayModeProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			"LightDismissOverlayMode", typeof(LightDismissOverlayMode),
			typeof(FlyoutBase),
			new FrameworkPropertyMetadata(default(LightDismissOverlayMode)));


		public DependencyObject OverlayInputPassThroughElement
		{
			get => (DependencyObject)GetValue(OverlayInputPassThroughElementProperty);
			set => SetValue(OverlayInputPassThroughElementProperty, value);
		}

		public static DependencyProperty OverlayInputPassThroughElementProperty { get; } =
			DependencyProperty.Register(nameof(OverlayInputPassThroughElement), typeof(DependencyObject), typeof(FlyoutBase), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

		/// <summary>
		/// Gets or sets whether a disabled control can receive focus.
		/// </summary>
		public bool AllowFocusWhenDisabled
		{
			get => GetAllowFocusWhenDisabledValue();
			set => SetAllowFocusWhenDisabledValue(value);
		}

		/// <summary>
		/// Identifies the AllowFocusWhenDisabled  dependency property.
		/// </summary>
		[GeneratedDependencyProperty(DefaultValue = false, Options = FrameworkPropertyMetadataOptions.Inherits, ChangedCallback = true)]
		public static DependencyProperty AllowFocusWhenDisabledProperty { get; } = CreateAllowFocusWhenDisabledProperty();

		private void OnAllowFocusWhenDisabledChanged(bool oldValue, bool newValue) =>
			SynchronizePropertyToPopup(Popup.AllowFocusWhenDisabledProperty, Boxer.Box(AllowFocusWhenDisabled));

		/// <summary>
		/// Gets or sets a value that indicates whether the element automatically gets focus when the user interacts with it.
		/// </summary>
		public bool AllowFocusOnInteraction
		{
			get => GetAllowFocusOnInteractionValue();
			set => SetAllowFocusOnInteractionValue(value);
		}

		/// <summary>
		/// Identifies for the AllowFocusOnInteraction dependency property.
		/// </summary>
		[GeneratedDependencyProperty(DefaultValue = true, Options = FrameworkPropertyMetadataOptions.Inherits, ChangedCallback = true)]
		public static DependencyProperty AllowFocusOnInteractionProperty { get; } = CreateAllowFocusOnInteractionProperty();

		/// <summary>
		/// Gets or sets a value that indicates how a flyout behaves when shown.
		/// </summary>
		public FlyoutShowMode ShowMode
		{
			get => (FlyoutShowMode)this.GetValue(ShowModeProperty);
			set => this.SetValue(ShowModeProperty, value);
		}

		/// <summary>
		/// Identifies the ShowMode dependency property.
		/// </summary>
		public static DependencyProperty ShowModeProperty { get; } =
			DependencyProperty.Register(
				nameof(ShowMode),
				typeof(FlyoutShowMode),
				typeof(FlyoutBase),
				new FrameworkPropertyMetadata(FlyoutShowMode.Standard, OnShowModePropertyChanged));

		private static void OnShowModePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			// UpdateStateToShowMode normalizes Auto → Standard and writes back to ShowMode,
			// but DependencyProperty coalescing prevents infinite recursion.
			if (sender is FlyoutBase flyoutBase)
			{
				flyoutBase.UpdateStateToShowMode((FlyoutShowMode)args.NewValue);
			}
		}

		/// <summary>
		/// Gets a value that indicates whether the flyout should show commands
		/// as primary (toolbar) based on the input device used to open it.
		/// </summary>
		public bool InputDevicePrefersPrimaryCommands
		{
			get => (bool)GetValue(InputDevicePrefersPrimaryCommandsProperty);
			private set => SetValue(InputDevicePrefersPrimaryCommandsProperty, value);
		}

		/// <summary>
		/// Identifies the InputDevicePrefersPrimaryCommands dependency property.
		/// </summary>
		public static DependencyProperty InputDevicePrefersPrimaryCommandsProperty { get; } =
			DependencyProperty.Register(
				nameof(InputDevicePrefersPrimaryCommands),
				typeof(bool),
				typeof(FlyoutBase),
				new FrameworkPropertyMetadata(BoolBoxes.False));

		private void OnAllowFocusOnInteractionChanged(bool oldValue, bool newValue) =>
			SynchronizePropertyToPopup(Popup.AllowFocusOnInteractionProperty, Boxer.Box(AllowFocusOnInteraction));

		// In WinUI, Target is declared as a back-reference (weak reference) in
		// IsDependencyPropertyBackReference(). Using ManagedWeakReference (via WeakReferencePool)
		// prevents shared flyouts from leaking the previous placement target's ViewModel
		// via FlyoutBase → Target → DataContext, and avoids per-show allocations.
		private ManagedWeakReference _targetWeakRef;

		public FrameworkElement Target
		{
			get => _targetWeakRef?.IsAlive == true ? _targetWeakRef.Target as FrameworkElement : null;
			private set
			{
				WeakReferencePool.ReturnWeakReference(this, _targetWeakRef);
				_targetWeakRef = value is not null ? WeakReferencePool.RentWeakReference(this, value) : null;
			}
		}

		public void Hide()
		{
			Hide(canCancel: true);
		}

		internal bool Hide(bool canCancel)
		{
			var cancel = false;
			if (canCancel)
			{
				OnClosing(ref cancel);
			}

			if (!cancel && canCancel)
			{
				var flyout = _openFlyouts.SkipWhile(f => f != this).Skip(1).FirstOrDefault();
				flyout?.Hide(true);
			}

			if (!cancel)
			{
				// Detach from the placement target on close to avoid holding the flyout alive
				m_placementTargetSubscriptions.Disposable = null;

				m_openingCanceled = true;

				if (_popup != null)
				{
					_popup.IsOpen = false;
				}
				IsOpen = false;

				OnClosed();

				RemoveFromOpenFlyouts();
			}

			return cancel;
		}

		private protected void RemoveFromOpenFlyouts()
		{
			if (_openFlyouts.Count > 0 && _openFlyouts[0] == this)
			{
				_openFlyouts.Remove(this);

				_isClosedPending = true;

				// TODO Uno: Closed should occur on PresenterUnloaded,
				// but that requires aligned loading/unloading lifecycle. #2895
				_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
				{
					Closed?.Invoke(this, EventArgs.Empty);
					_isClosedPending = false;

					if (_openFlyouts.Count > 0)
					{
						_openFlyouts[0].Hide();
					}
				});
			}
		}

		private void UpdateStateToShowMode(FlyoutShowMode showMode)
		{
			// TODO Uno: m_shouldHideIfPointerMovesAway not handled yet.

			if (showMode == FlyoutShowMode.Auto)
			{
				showMode = FlyoutShowMode.Standard;
			}

			ShowMode = showMode;

			switch (showMode)
			{
				case FlyoutShowMode.Standard:
					m_shouldTakeFocus = true;
					m_shouldOverlayPassThroughAllInput = false;
					break;
				case FlyoutShowMode.Transient:
				case FlyoutShowMode.TransientWithDismissOnPointerMoveAway:
					m_shouldTakeFocus = false;
					m_shouldOverlayPassThroughAllInput = true;
					break;
			}
		}

		private protected virtual void OnOpening()
		{
			// Set InputDevicePrefersPrimaryCommands based on input device type (mirrors WinUI OnOpening)
			switch (m_inputDeviceTypeUsedToOpen)
			{
				case InputDeviceType.None:
				case InputDeviceType.Mouse:
				case InputDeviceType.Keyboard:
				case InputDeviceType.GamepadOrRemote:
					InputDevicePrefersPrimaryCommands = false;
					break;
				case InputDeviceType.Touch:
				case InputDeviceType.Pen:
					InputDevicePrefersPrimaryCommands = true;
					break;
			}

			m_openingCanceled = false;
			Opening?.Invoke(this, EventArgs.Empty);
		}

		private protected virtual void OnClosing(ref bool cancel)
		{
			var closing = new FlyoutBaseClosingEventArgs();
			Closing?.Invoke(this, closing);
			cancel = closing.Cancel;
		}

		private protected virtual void OnClosed()
		{
			m_isTargetPositionSet = false;
			InputDevicePrefersPrimaryCommands = false;

			// Now that the flyout has closed, reset its m_hasPlacementOverride flag so that
			// it will be valid the next time it opens again.
			m_hasPlacementOverride = false;

			// Target is a WeakReference (matching WinUI's back-reference), so it doesn't need explicit clearing.
			// We intentionally do NOT clear Target here because commands (e.g., in
			// TextCommandBarFlyout) may still access Target after the flyout closes.
			if (_popup?.Child is FrameworkElement presenter)
			{
				presenter.ClearValue(FrameworkElement.DataContextProperty);
			}

		}

		/// <summary>
		/// Forwards DataContext, flow direction and theme from the placement target to the presenter.
		/// Ported from WinUI: FlyoutBase_partial.cpp ForwardTargetPropertiesToPresenter.
		/// </summary>
		private void ForwardTargetPropertiesToPresenter()
		{
			if (_popup?.Child is FrameworkElement presenter)
			{
				if (Target is { } target)
				{
					presenter.DataContext = target.DataContext;
				}

				// The presenter is reused across openings, so a missing target resets the direction rather than keeping the previous target's.
				presenter.FlowDirection = Target?.FlowDirection ?? FlowDirection.LeftToRight;
			}

			ForwardThemeToPresenter();
		}

		// MUX Reference: FlyoutBase_Partial.cpp FlyoutBase::ForwardThemeToPresenter (lines 1534-1592).
		// Forwards the placement target's theme to the presenter and popup so the flyout matches the region
		// it was opened from. Element-level theme forwarding is Skia/WASM-only; native targets support
		// OS + application theme, so on native this is a no-op and the flyout follows the app/OS theme.
		private void ForwardThemeToPresenter()
		{
			if (_popup?.Child is not Control presenter)
			{
				return;
			}

			// Ensure the Flyout matches the theme of its placement target.
			var requestedThemeProperty = FrameworkElement.RequestedThemeProperty;

			// We'll only override the requested theme on the presenter if its value hasn't been explicitly set.
			// Otherwise, we'll abide by its existing value.
			if (presenter.GetCurrentHighestValuePrecedence(requestedThemeProperty) == DependencyPropertyValuePrecedences.DefaultValue ||
				m_isFlyoutPresenterRequestedThemeOverridden)
			{
				var currentFlyoutPresenterTheme = presenter.RequestedTheme;
				var requestedTheme = ElementTheme.Default;

				// Walk up the tree from the placement target until we find an element with a RequestedTheme.
				DependencyObject spCurrent = Target;
				while (spCurrent is not null)
				{
					var spCurrentAsFE = spCurrent as FrameworkElement;

					requestedTheme = spCurrentAsFE?.RequestedTheme ?? ElementTheme.Default;

					if (requestedTheme != ElementTheme.Default)
					{
						break;
					}

					var spParent = VisualTreeHelper.GetParent(spCurrent);
					if (spParent is PopupRoot)
					{
						// If the target is in a Popup and the Popup is in the Visual Tree, we want to inherit the theme
						// from that Popup's parent. Otherwise we will get the App's theme, which might not be what
						// is expected.
						spParent = spCurrentAsFE?.Parent;
					}

					spCurrent = spParent;
				}

				if (requestedTheme != currentFlyoutPresenterTheme)
				{
					presenter.RequestedTheme = requestedTheme;
					m_isFlyoutPresenterRequestedThemeOverridden = true;
				}

				// Also set the popup's theme. If there is a SystemBackdrop on the menu, it'll be watching the theme on the
				// popup itself rather than the presenter set as the popup's child.
				_popup.RequestedTheme = requestedTheme;
			}
		}

		private protected virtual void OnOpened() { }

		protected virtual Control CreatePresenter() => null;

		private void OnPopupClosed(object sender, object e)
		{
			Hide(canCancel: false);
		}

		// Ported from WinUI: FlyoutBase_partial.cpp OnPopupLostFocus (lines 2349-2434)
		private void OnPopupLostFocus(object sender, RoutedEventArgs args)
		{
			if (_popup is not { IsOpen: true })
			{
				return;
			}

			var contentRoot = VisualTree.GetContentRootForElement(this);
			if (contentRoot?.FocusManager.FocusedElement is not UIElement focusedElement)
			{
				return;
			}

			// Walk up from the focused element (WinUI: FlyoutBase_partial.cpp lines 2394-2420).
			var currentElement = focusedElement;
			Popup popupAncestor = null;
			bool popupRootExists = false;
			bool popupAncestorIsLightDismiss = false;

			while (currentElement != null)
			{
				// WinUI: FlyoutBase_partial.cpp lines 2396-2401
				if (currentElement is PopupRoot)
				{
					popupRootExists = true;
					currentElement = currentElement.GetUIElementAdjustedParentInternal(false);
					continue;
				}

				if (currentElement is Popup popup)
				{
					popupAncestor = popup;
					// WinUI: IsSelfOrAncestorLightDismiss() = m_fIsLightDismiss || m_fIsSubMenu
					popupAncestorIsLightDismiss = popup.IsLightDismissEnabled || popup.IsSubMenu;
					break;
				}

				currentElement = currentElement.GetUIElementAdjustedParentInternal(false);
			}

			// WinUI: FlyoutBase_partial.cpp lines 2422-2429
			if (!popupRootExists && (popupAncestor == null || !popupAncestorIsLightDismiss))
			{
				Hide();
			}
		}

		private protected void AddToOpenFlyouts()
		{
			if (!_openFlyouts.Contains(this))
			{
				_openFlyouts.Add(this);
			}
		}

		private void SynchronizePropertyToPopup(DependencyProperty property, object value)
		{
			// This is present to force properties to be propagated to the popup of the flyout
			// since it is not directly a child in the visual tree of the flyout.
			_popup?.SetValue(property, value, precedence: DependencyPropertyValuePrecedences.Local);
		}

		public static FlyoutBase GetAttachedFlyout(FrameworkElement element)
		{
			return (FlyoutBase)element.GetValue(AttachedFlyoutProperty);
		}

		public static void SetAttachedFlyout(FrameworkElement element, FlyoutBase value)
		{
			element.SetValue(AttachedFlyoutProperty, value);
		}

		public static DependencyProperty AttachedFlyoutProperty
		{
			[DynamicDependency(nameof(GetAttachedFlyout))]
			[DynamicDependency(nameof(SetAttachedFlyout))]
			get;
		} = DependencyProperty.RegisterAttached(
				"AttachedFlyout",
				typeof(FlyoutBase),
				typeof(FlyoutBase),
				new FrameworkPropertyMetadata(null));

		public static void ShowAttachedFlyout(FrameworkElement flyoutOwner)
		{
			var flyout = GetAttachedFlyout(flyoutOwner);

			// FlyoutBase has no DataContext of its own (it is a DependencyObject, not a FrameworkElement — WinUI parity).
			// ShowAt → ShowAtCore → ForwardTargetPropertiesToPresenter forwards the owner's DataContext to the presenter;
			// content and items then resolve their {Binding}s through normal visual-tree inheritance from the presenter.
			flyout?.ShowAt(flyoutOwner);
		}

		internal virtual Control GetPresenter() => _popup?.Child as Control;
	}
}
