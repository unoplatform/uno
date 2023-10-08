// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
#nullable enable

#if __IOS__ || __ANDROID__
#define HAS_NATIVE_COMMANDBAR
#endif

using System;
using System.Collections.Generic;
using System.Text;
using DirectUI;
using Uno.Disposables;
using Uno.UI;
using Uno.UI.Helpers.WinUI;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.Extensions;
using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;
using Uno.UI.Xaml.Input;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Popup = Microsoft.UI.Xaml.Controls.Primitives.Popup;
using Windows.System;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation;
using Uno.UI.Controls;
using Uno.UI.Xaml.Core;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
#else
using Windows.UI.Input;
using Windows.Devices.Input;
using Windows.UI.Core;
#endif

namespace Microsoft.UI.Xaml.Controls
{
	public partial class AppBar : ContentControl
#if HAS_NATIVE_COMMANDBAR
		, ICustomClippingElement
#endif
	{
		public event EventHandler<object>? Opened;
		public event EventHandler<object>? Opening;
		public event EventHandler<object>? Closed;
		public event EventHandler<object>? Closing;

		private const string TEXT_HUB_SEE_MORE = nameof(TEXT_HUB_SEE_MORE);
		private const string TEXT_HUB_SEE_LESS = nameof(TEXT_HUB_SEE_LESS);
		private const string UIA_MORE_BUTTON = nameof(UIA_MORE_BUTTON);
		private const string UIA_LESS_BUTTON = nameof(UIA_LESS_BUTTON);

		protected Grid? m_tpLayoutRoot;
		protected FrameworkElement? m_tpContentRoot;
		protected ButtonBase? m_tpExpandButton;
		protected WeakReference<VisualStateGroup?>? m_tpDisplayModesStateGroupRef;

		protected double m_compactHeight;
		protected double m_minimalHeight;

		AppBarMode m_Mode;

		// Owner, if this AppBar is owned by a Page using TopAppBar/BottomAppBar.
		WeakReference<Page>? m_wpOwner;

		SerialDisposable m_contentRootSizeChangedEventHandler = new SerialDisposable();
		SerialDisposable m_windowSizeChangedEventHandler = new SerialDisposable();
		SerialDisposable m_expandButtonClickEventHandler = new SerialDisposable();
		SerialDisposable m_displayModeStateChangedEventHandler = new SerialDisposable();

#pragma warning disable CS0414
#pragma warning disable CS0649
#pragma warning disable CS0169
		// Focus state to be applied on loaded.
		FocusState m_onLoadFocusState;
		//UIElement? m_layoutTransitionElement;
		UIElement? m_overlayLayoutTransitionElement;
		private bool _isNativeTemplate;
		//UIElement m_parentElementForLTEs;
#pragma warning restore CS0414
#pragma warning restore CS0649
#pragma warning restore CS0169


		FrameworkElement? m_overlayElement;
		SerialDisposable m_overlayElementPointerPressedEventHandler = new SerialDisposable();

		WeakReference<DependencyObject>? m_savedFocusedElementWeakRef;
		FocusState m_savedFocusState;

		bool m_isInOverlayState;
		bool m_isChangingOpenedState;
		bool m_hasUpdatedTemplateSettings;

		// We refresh this value in the OnSizeChanged() & OnContentSizeChanged() handlers.
		double m_contentHeight;

		bool m_isOverlayVisible;
		Storyboard? m_overlayOpeningStoryboard;
		Storyboard? m_overlayClosingStoryboard;

		protected double ContentHeight => m_contentHeight;

		public AppBar()
		{
			m_Mode = AppBarMode.Inline;
			m_onLoadFocusState = FocusState.Unfocused;
			m_savedFocusState = FocusState.Unfocused;
			m_isInOverlayState = false;
			m_isChangingOpenedState = false;
			m_hasUpdatedTemplateSettings = false;
			m_compactHeight = 0d;
			m_minimalHeight = 0d;
			m_contentHeight = 0d;
			m_isOverlayVisible = false;

			PrepareState();
			DefaultStyleKey = typeof(AppBar);
		}

		protected virtual void PrepareState()
		{
			SizeChanged += OnSizeChanged;

			this.SetValue(TemplateSettingsProperty, new AppBarTemplateSettings());
		}

		// Note that we need to wait for OnLoaded event to set focus.
		// When we get the on opened event children of AppBar will not be populated
		// yet which will prevent them from getting focus.
		private protected override void OnLoaded()
		{
			base.OnLoaded();

			var isOpen = IsOpen;
			if (isOpen)
			{
				OnIsOpenChanged(true);
			}

			// TODO: Uno specific - use XamlRoot instead of Window
			if (XamlRoot is not null)
			{
				XamlRoot.Changed += OnXamlRootChanged;
				m_windowSizeChangedEventHandler.Disposable = Disposable.Create(() => XamlRoot.Changed -= OnXamlRootChanged);
			}

			//UNO TODO

			//if (m_Mode != AppBarMode_Inline)
			//{
			//	ctl::ComPtr<IApplicationBarService> applicationBarService;
			//	IFC_RETURN(DXamlCore::GetCurrent()->GetApplicationBarService(applicationBarService));

			//	if (m_Mode == AppBarMode_Floating)
			//	{
			//		IFC_RETURN(applicationBarService->RegisterApplicationBar(this, m_Mode));
			//	}

			//	// Focus the AppBar only if this is a Threshold app and if the AppBar that is being loaded is already open.
			//	isOpen = FALSE;
			//	IFC_RETURN(get_IsOpen(&isOpen));

			//	if (isOpen)
			//	{
			//		auto focusState = (m_onLoadFocusState != xaml::FocusState_Unfocused ? m_onLoadFocusState : xaml::FocusState_Programmatic);
			//		IFC_RETURN(applicationBarService->FocusApplicationBar(this, focusState));
			//	}

			//	// Reset the saved focus state
			//	m_onLoadFocusState = xaml::FocusState_Unfocused;
			//}

			UpdateVisualState();
		}

		private void OnLayoutUpdated(object? sender, object e)
		{
			//if (m_layoutTransitionElement is { })
			//{
			//	PositionLTEs();
			//}
		}

		private void OnSizeChanged(object sender, SizeChangedEventArgs args)
		{
			RefreshContentHeight();
			UpdateTemplateSettings();

			if (GetOwner() is { } pageOwner)
			{
				// UNO TODO
				//pageOwner.AppBarClosedSizeChanged();
			}
		}

		internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			if (args.Property == IsOpenProperty)
			{
				var isOpen = (bool)args.NewValue;
				OnIsOpenChanged(isOpen);

				UpdateVisualState();
			}
			else if (args.Property == IsStickyProperty)
			{
				OnIsStickyChanged();
			}
			else if (args.Property == ClosedDisplayModeProperty)
			{
				// UNO TODO
				/*
				if (m_Mode != AppBarMode.Inline)
				{
					ctl::ComPtr<IApplicationBarService> applicationBarService;
					IFC_RETURN(DXamlCore::GetCurrent()->GetApplicationBarService(applicationBarService));

					IFC_RETURN(applicationBarService->HandleApplicationBarClosedDisplayModeChange(this, m_Mode));
				}*/

				InvalidateMeasure();
				UpdateVisualState();
			}
			else if (args.Property == LightDismissOverlayModeProperty)
			{
				ReevaluateIsOverlayVisible();
			}
			else if (args.Property == IsEnabledProperty)
			{
				UpdateVisualState();
			}
		}

		protected override void OnVisibilityChanged(Visibility oldValue, Visibility newValue)
		{
			if (GetOwner() is { } pageOwner)
			{
				// UNO TODO
				//pageOwner.AppBarClosedSizeChanged();
			}
		}
		private void UnregisterEvents()
		{
			m_contentRootSizeChangedEventHandler.Disposable = null;
			m_windowSizeChangedEventHandler.Disposable = null;
			m_expandButtonClickEventHandler.Disposable = null;
			m_displayModeStateChangedEventHandler.Disposable = null;
			m_overlayElementPointerPressedEventHandler.Disposable = null;

			m_tpLayoutRoot = null;
			m_tpContentRoot = null;
			m_tpExpandButton = null;
			m_tpDisplayModesStateGroupRef = null;

			m_overlayClosingStoryboard = null;
			m_overlayOpeningStoryboard = null;
		}

		private protected override void OnUnloaded()
		{
			LayoutUpdated -= OnLayoutUpdated;
			SizeChanged -= OnSizeChanged;
			if (m_isInOverlayState)
			{
				TeardownOverlayState();
			}

			UnregisterEvents();

			base.OnUnloaded();

		}

		protected override void OnApplyTemplate()
		{
			UnregisterEvents();

			// Clear our previous template parts.
			m_tpLayoutRoot = null;
			m_tpContentRoot = null;
			m_tpExpandButton = null;
			m_tpDisplayModesStateGroupRef = null;

			base.OnApplyTemplate();

			GetTemplatePart("LayoutRoot", out m_tpLayoutRoot);
			GetTemplatePart("ContentRoot", out m_tpContentRoot);

#if HAS_NATIVE_COMMANDBAR
			_isNativeTemplate = Uno.UI.Extensions.DependencyObjectExtensions
				.FindFirstChild<NativeCommandBarPresenter?>(this) != null;
#endif

			if (m_tpContentRoot is { })
			{
				m_tpContentRoot.SizeChanged += OnContentRootSizeChanged;
				m_contentRootSizeChangedEventHandler.Disposable = Disposable.Create(() => m_tpContentRoot.SizeChanged -= OnContentRootSizeChanged);
			}

			GetTemplatePart("ExpandButton", out m_tpExpandButton);

			if (m_tpExpandButton == null)
			{
				// The previous CommandBar template used "MoreButton" for this template part's name,
				// so now we're stuck with it, as much as I'd like to converge them..
				GetTemplatePart("MoreButton", out m_tpExpandButton);
			}

			if (m_tpExpandButton is { })
			{
				m_tpExpandButton.Click += OnExpandButtonClick;
				m_expandButtonClickEventHandler.Disposable = Disposable.Create(() => m_tpExpandButton.Click -= OnExpandButtonClick);

				var toolTip = new ToolTip();
				toolTip.Content = DXamlCore.Current.GetLocalizedResourceString(TEXT_HUB_SEE_MORE);

				ToolTipService.SetToolTip(m_tpExpandButton, toolTip);

				var automationName = AutomationProperties.GetName((Button)m_tpExpandButton);
				if (automationName == null)
				{
					automationName = DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString(UIA_MORE_BUTTON);
					AutomationProperties.SetName((Button)m_tpExpandButton, automationName);
				}
			}

			// Query compact & minimal height from resource dictionary.
			{
				m_compactHeight = ResourceResolver.ResolveTopLevelResourceDouble("AppBarThemeCompactHeight");
				m_minimalHeight = ResourceResolver.ResolveTopLevelResourceDouble("AppBarThemeMinimalHeight");
			}

			// Lookup the animations to use for the window overlay.
			if (m_tpLayoutRoot is { })
			{
				var gridResources = m_tpLayoutRoot.Resources;

				if (gridResources.TryGetValue("OverlayOpeningAnimation", out var oWindowOverlayOpeningStoryboard)
					&& oWindowOverlayOpeningStoryboard is Storyboard windowOverlayOpeningStoryboard)
				{
					m_overlayOpeningStoryboard = windowOverlayOpeningStoryboard;
				}

				if (gridResources.TryGetValue("OverlayClosingAnimation", out var oWindowOverlayClosingStoryboard)
					&& oWindowOverlayClosingStoryboard is Storyboard windowOverlayClosingStoryboard)
				{
					m_overlayClosingStoryboard = windowOverlayClosingStoryboard;
				}
			}

			ReevaluateIsOverlayVisible();
		}

#if HAS_NATIVE_COMMANDBAR
		bool ICustomClippingElement.AllowClippingToLayoutSlot => !_isNativeTemplate;
		bool ICustomClippingElement.ForceClippingToLayoutSlot => false;
#endif

		protected override Size MeasureOverride(Size availableSize)
		{
			var size = base.MeasureOverride(availableSize);

			if (_isNativeTemplate)
			{
				return size;
			}

			if (m_Mode == AppBarMode.Top || m_Mode == AppBarMode.Bottom)
			{
				// regardless of what we desire, settings of alignment or fixed size content, we will always take up full width
				size.Width = availableSize.Width;
			}

			// Make sure our returned height matches the configured state.
			var closedDisplayMode = ClosedDisplayMode;

			size.Height = closedDisplayMode switch
			{
				AppBarClosedDisplayMode.Compact => m_compactHeight,
				AppBarClosedDisplayMode.Minimal => m_minimalHeight,
				_ => 0d,
			};

			return size;
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			var layoutRootDesiredSize = new Size();
			if (m_tpLayoutRoot is { })
			{
				layoutRootDesiredSize = m_tpLayoutRoot.DesiredSize;
			}
			else
			{
				layoutRootDesiredSize = finalSize;
			}

			var baseSize = base.ArrangeOverride(new Size(finalSize.Width, layoutRootDesiredSize.Height));

			baseSize.Height = finalSize.Height;

			return baseSize;
		}

		protected virtual void OnOpening(object e)
		{
			TryQueryDisplayModesStatesGroup();

			if (m_Mode == AppBarMode.Inline)
			{
				//// If we're in a popup that is light-dismissable, then we don't want to set up
				//// a light-dismiss layer - the popup will have its own light-dismiss layer,
				//// and it can interfere with ours.
				var popupAncestor = this.FindFirstParent<Popup>();
				if (popupAncestor == null || (popupAncestor.IsLightDismissEnabled || popupAncestor.IsSubMenu))
				{
					if (!m_isInOverlayState)
					{
						if (IsInLiveTree)
						{
							// Setup our LTEs and light-dismiss layer.
							SetupOverlayState();

							if (m_isOverlayVisible)
							{
								PlayOverlayOpeningAnimation();
							}
						}
					}
				}

				var isSticky = IsSticky;
				if (!isSticky)
				{
					SetFocusOnAppBar();
				}
			}
			else
			{
				// Pre-Threshold AppBars were hidden and would get added to the tree upon opening which
				// would invoke their loaded handlers to set focus.
				// In threshold, hidden appbars are always in the tree, so we have to simulate the same
				// behavior by focusing the appbar whenever it opens.
				var closedDisplayMode = ClosedDisplayMode;
				if (closedDisplayMode == AppBarClosedDisplayMode.Hidden)
				{
					// UNO TODO
					//ctl::ComPtr<IApplicationBarService> applicationBarService;
					//IFC_RETURN(DXamlCore::GetCurrent()->GetApplicationBarService(applicationBarService));

					// Determine the focus state

					// UNO TODO
					//var focusState = m_onLoadFocusState != FocusState.Unfocused ? m_onLoadFocusState : FocusState.Programmatic;
					//IFC_RETURN(applicationBarService->FocusApplicationBar(this, focusState));

					// Reset the saved focus state
					m_onLoadFocusState = FocusState.Unfocused;
				}
			}

			if (m_tpExpandButton is { })
			{
				// Set a tooltip with "See Less" for the expand button.
				var toolTip = new ToolTip();
				toolTip.Content = DXamlCore.Current.GetLocalizedResourceString(TEXT_HUB_SEE_LESS);

				ToolTipService.SetToolTip(m_tpExpandButton, toolTip);

				AutomationProperties.SetName(m_tpExpandButton, DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString(UIA_LESS_BUTTON));
			}

			ElementSoundPlayer.RequestInteractionSoundForElement(ElementSoundKind.Show, this);

			Opening?.Invoke(this, e);
		}


		protected virtual void OnOpened(object e)
		{
			Opened?.Invoke(this, e);

			// UNO TODO
			//if (DXamlCore::GetCurrent()->GetHandle()->BackButtonSupported())
			//{
			//	IFC_RETURN(BackButtonIntegration_RegisterListener(this));
			//}

		}

		protected virtual void OnClosing(object e)
		{
			if (m_Mode == AppBarMode.Inline)
			{
				// Only restore focus if this AppBar isn't in a flyout - if it is,
				// then focus will be restored when the flyout closes.
				// We'll interfere with that if we restore focus before that time.
				var popupAncestor = this.FindFirstParent<Popup>();
				if (popupAncestor == null || !(popupAncestor.PopupPanel is FlyoutBasePopupPanel))
				{
					RestoreSavedFocus();
				}

				if (m_isOverlayVisible && m_isInOverlayState)
				{
					PlayOverlayClosingAnimation();
				}
			}

			if (m_tpExpandButton is { })
			{
				// Set a tooltip with "See More" for the expand button.
				var tooltipText = DXamlCore.Current.GetLocalizedResourceString(TEXT_HUB_SEE_MORE);
				var tooltip = new ToolTip();
				tooltip.Content = tooltipText;

				ToolTipService.SetToolTip(m_tpExpandButton, tooltip);

				// Update the localized accessibility name for expand button with the more app bar button.
				AutomationProperties.SetName(m_tpExpandButton, DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString(UIA_MORE_BUTTON));
			}

			// Request a play hide sound for closed
			ElementSoundPlayer.RequestInteractionSoundForElement(ElementSoundKind.Hide, this);

			// Raise the event
			Closing?.Invoke(this, e);
		}

		protected virtual void OnClosed(object e)
		{
			if (m_Mode == AppBarMode.Inline && m_isInOverlayState)
			{
				TeardownOverlayState();
			}

			// Raise the event
			Closed?.Invoke(this, e);

			// UNO TODO
			//IFC_RETURN(BackButtonIntegration_UnregisterListener(this));

		}


		internal override TabStopProcessingResult ProcessTabStopOverride(
			DependencyObject? focusedElement,
			DependencyObject? candidateTabStopElement,
			bool isBackward,
			bool didCycleFocusAtRootVisualScope)
		{
			var result = new TabStopProcessingResult()
			{
				NewTabStop = null,
				IsOverriden = false,
			};

			if (m_Mode == AppBarMode.Inline)
			{
				var isOpen = IsOpen;
				var isSticky = IsSticky;

				// We don't override tab-stop behavior for closed or sticky appbars.
				if (!isOpen || isSticky)
				{
					return result;
				}

				var isAncestorOfFocusedElement = this.IsAncestorOf(focusedElement);
				var isAncestorOfCandidateElement = this.IsAncestorOf(candidateTabStopElement);

				// If the element losing focus is a child of the appbar and the element
				// we're losing focus to is not, then we override tab-stop to keep the
				// focus within the appbar.
				if (isAncestorOfFocusedElement && !isAncestorOfCandidateElement)
				{
					var newTabStop = isBackward ? FocusManager.FindLastFocusableElement(this) : FocusManager.FindFirstFocusableElement(this);

					if (newTabStop is { })
					{
						result.NewTabStop = newTabStop;
						result.IsOverriden = true;
					}
				}
			}

			return result;
		}


		private void OnContentRootSizeChanged(object sender, SizeChangedEventArgs args)
		{
			var didChange = RefreshContentHeight();

			if (didChange)
			{
				UpdateTemplateSettings();
			}
		}

		private void OnXamlRootChanged(object sender, XamlRootChangedEventArgs e)
		{
			if (m_Mode == AppBarMode.Inline)
			{
				TryDismissInlineAppBar();
			}
		}

		// floating appbars are managed through vsm. System appbars (as set by page) use
		// transitions that are triggered by layout to load, unload and move around.
		private protected override void ChangeVisualState(bool useTransitions)
		{
			base.ChangeVisualState(useTransitions);

			bool ignored = false;
			bool isEnabled = false;
			bool isOpen = false;

			var closedDisplayMode = AppBarClosedDisplayMode.Hidden;
			bool shouldOpenUp = false;

			isEnabled = IsEnabled;
			isOpen = IsOpen;
			closedDisplayMode = ClosedDisplayMode;

			// We only need to check this if we're going to an opened state.
			if (isOpen)
			{
				shouldOpenUp = GetShouldOpenUp();
			}

			// CommonStates
			GoToState(useTransitions, isEnabled ? "Normal" : "Disabled", out ignored);

			// FloatingStates
			if (m_Mode == AppBarMode.Floating)
			{
				GoToState(useTransitions, isOpen ? "FloatingVisible" : "FloatingHidden", out ignored);
			}

			// DockPositions
			switch (m_Mode)
			{
				case AppBarMode.Top:
					GoToState(useTransitions, "Top", out ignored);
					break;

				case AppBarMode.Bottom:
					GoToState(useTransitions, "Bottom", out ignored);
					break;

				default:
					GoToState(useTransitions, "Undocked", out ignored);
					break;
			}

			// DisplayModeStates
			var displayMode = closedDisplayMode switch
			{
				AppBarClosedDisplayMode.Compact => "Compact",
				AppBarClosedDisplayMode.Minimal => "Minimal",
				_ => "Hidden",
			};

			var placement = shouldOpenUp ? "Up" : "Down";
			var openState = string.Empty;

			if (isOpen)
			{
				openState = "Open";
			}
			else
			{
				openState = "Closed";
				placement = string.Empty;
			}

			ignored = GoToState(useTransitions, $"{displayMode}{openState}{placement}");
		}

		protected override void OnPointerPressed(PointerRoutedEventArgs e)
		{
			base.OnPointerPressed(e);

			var isOpen = IsOpen;
			if (isOpen)
			{
				var isSticky = IsSticky;

				if (!isSticky)
				{
					// If the app bar is in a modal-like state, then don't propagate pointer
					// events.
					e.Handled = true;
				}
			}
			else
			{
				var closedDisplayMode = ClosedDisplayMode;
				if (closedDisplayMode == AppBarClosedDisplayMode.Minimal)
				{
					IsOpen = true;
					e.Handled = true;
				}
			}
		}

		protected override void OnRightTapped(RightTappedRoutedEventArgs e)
		{
			base.OnRightTapped(e);

			if (m_Mode != AppBarMode.Inline)
			{
				var pointerDeviceType = e.PointerDeviceType;
				if (pointerDeviceType != PointerDeviceType.Mouse)
				{
					return;
				}

				var isOpen = IsOpen;
				var isHandled = e.Handled;

				if (isOpen && !isHandled)
				{
					// UNO TODO
					//ctl::ComPtr<IApplicationBarService> applicationBarService;
					//IFC_RETURN(DXamlCore::GetCurrent()->GetApplicationBarService(applicationBarService));

					//applicationBarService->SetFocusReturnState(xaml::FocusState_Pointer);
					//IFC_RETURN(applicationBarService->ToggleApplicationBars());

					//applicationBarService->ResetFocusReturnState();
					e.Handled = true;
				}
			}
		}

		private void OnIsStickyChanged()
		{
			if (m_Mode != AppBarMode.Inline)
			{
				// UNO TODO
				//ctl::ComPtr<IApplicationBarService> applicationBarService;
				//IFC_RETURN(DXamlCore::GetCurrent()->GetApplicationBarService(applicationBarService));
				//IFC_RETURN(applicationBarService->UpdateDismissLayer());
			}

			if (m_overlayElement is { })
			{
				var isSticky = IsSticky;
				m_overlayElement.IsHitTestVisible = !isSticky;
			}
		}

		private void OnIsOpenChanged(bool isOpen)
		{
			// If the AppBar is not live, then wait until it's loaded before
			// responding to changes to opened state and firing our Opening/Opened events.
			if (!IsInLiveTree)
			{
				return;
			}

			if (m_Mode != AppBarMode.Inline)
			{
				// UNO TODO
				//ctl::ComPtr<IApplicationBarService> applicationBarService;
				//IFC_RETURN(DXamlCore::GetCurrent()->GetApplicationBarService(applicationBarService));

				//BOOLEAN hasFocus = FALSE;
				//IFC_RETURN(HasFocus(&hasFocus));

				//if (isOpen)
				//{
				//	IFC_RETURN(applicationBarService->SaveCurrentFocusedElement(this));
				//	IFC_RETURN(applicationBarService->OpenApplicationBar(this, m_Mode));

				//	// If the AppBar does not already have focus (i.e. it was opened programmatically),
				//	// then focus the AppBar.
				//	if (!hasFocus)
				//	{
				//		IFC_RETURN(applicationBarService->FocusApplicationBar(this, xaml::FocusState_Programmatic));
				//	}
				//}
				//else
				//{
				//	IFC_RETURN(applicationBarService->CloseApplicationBar(this, m_Mode));

				//	// Only restore the focus to the saved element if we have the focus just before closing.
				//	// For CommandBar, we also check if the Overflow has focus in the override method "HasFocus"
				//	if (hasFocus)
				//	{
				//		IFC_RETURN(applicationBarService->FocusSavedElement(this));
				//	}
				//}

				//IFC_RETURN(applicationBarService->UpdateDismissLayer());
			}

			// Flag that we're transitions between opened & closed states.
			m_isChangingOpenedState = true;

			// Fire our Opening/Closing events.  If we're a legacy app or a badly
			// re-templated app, then fire the Opened/Closed events as well.
			{
				var routedEventArgs = new RoutedEventArgs(this);

				if (isOpen)
				{
					OnOpening(routedEventArgs);
				}
				else
				{
					OnClosing(routedEventArgs);
				}

				// We only query the display modes visual state group for post-WinBlue AppBars
				// so in cases where we don't have it (either via re-templating or legacy apps)
				// fire the Opening/Closing & Opened/Closed events immediately.
				// For WinBlue apps, firing the Opening/Closing events as well doesn't
				// matter because Blue apps wouldn't have had access to them.
				// For post-WinBlue AppBars, we fire the Opening/Closing & Opened/Closed
				// events based on our display mode state transitions.
				//if (m_tpDisplayModesStateGroup == null)
				{
					if (isOpen)
					{
						OnOpened(routedEventArgs);
					}
					else
					{
						OnClosed(routedEventArgs);
					}
				}
			}
		}

#if false
		private void OnIsOpenChangedForAutomation(DependencyPropertyChangedEventArgs args)
		{
			var isOpen = (bool)args.NewValue;
			bool bAutomationListener = false;

			if (isOpen)
			{
				AutomationPeer.RaiseEventIfListener(this, AutomationEvents.MenuOpened);
			}
			else
			{
				AutomationPeer.RaiseEventIfListener(this, AutomationEvents.MenuClosed);
			}

			// Raise ToggleState Property change event for Automation clients if they are listening for property changed events.
			bAutomationListener = AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged);
			if (bAutomationListener)
			{
				var automationPeer = GetAutomationPeer();
				if (automationPeer is AppBarAutomationPeer applicationBarAutomationPeer)
				{
					applicationBarAutomationPeer.RaiseToggleStatePropertyChangedEvent(args.OldValue, args.NewValue);
					applicationBarAutomationPeer.RaiseExpandCollapseAutomationEvent(isOpen);
				}

			}
		}
#endif
		protected override AutomationPeer OnCreateAutomationPeer()
		{
			AutomationPeer? ppAutomationPeer = null;

			AppBarAutomationPeer spAutomationPeer;
			spAutomationPeer = new AppBarAutomationPeer(this);
			ppAutomationPeer = spAutomationPeer;

			return ppAutomationPeer;
		}

		protected override void OnKeyDown(KeyRoutedEventArgs args)
		{
			base.OnKeyDown(args);

			bool isHandled = false;
			isHandled = args.Handled;

			if (isHandled)
			{
				return;
			}

			var key = args.Key;
			if (key == VirtualKey.Escape)
			{
				bool isAnyAppBarClosed = false;

				if (m_Mode == AppBarMode.Inline)
				{
					isAnyAppBarClosed = TryDismissInlineAppBar();
				}
				else
				{
					// UNO TODO
					//BOOLEAN isSticky = FALSE;
					//IFC_RETURN(get_IsSticky(&isSticky));

					//// If we have focus and the app bar is not sticky close all light-dismiss app bars on ESC
					//ctl::ComPtr<IApplicationBarService> applicationBarService;
					//IFC_RETURN(DXamlCore::GetCurrent()->GetApplicationBarService(applicationBarService));

					//BOOLEAN hasFocus = FALSE;
					//IFC_RETURN(HasFocus(&hasFocus));
					//if (hasFocus)
					//{
					//	IFC_RETURN(applicationBarService->CloseAllNonStickyAppBars(&isAnyAppBarClosed));

					//	if (isSticky)
					//	{
					//		// If the appbar is sticky restore focus to the saved element without closing the appbar
					//		applicationBarService->SetFocusReturnState(xaml::FocusState_Keyboard);
					//		IFC_RETURN(applicationBarService->FocusSavedElement(this));
					//		applicationBarService->ResetFocusReturnState();
					//	}
					//}
				}

				args.Handled = isAnyAppBarClosed;
			}
		}

		public void SetOwner(Page pOwner)
		{
			m_wpOwner = new WeakReference<Page>(pOwner);
		}

		public Page? GetOwner()
		{
			if (m_wpOwner != null
				&& m_wpOwner.TryGetTarget(out var pageOwner)
				&& pageOwner is { })
			{
				return pageOwner;
			}

			return null;
		}

		protected virtual bool ContainsElement(DependencyObject pElement)
		{
			bool isAncestorOfElement = false;

			// For AppBar, ContainsElement is equivalent to IsAncestorOf.
			// However, ContainsElement is a virtual method, and CommandBar's
			// implementation of it also checks the overflow popup separately from
			// IsAncestorOf since the popup isn't part of the same visual tree.
			isAncestorOfElement = this.IsAncestorOf(pElement);

			return isAncestorOfElement;
		}

		protected bool IsExpandButton(UIElement element)
		{
			return m_tpExpandButton is { } && element == m_tpExpandButton;
		}

		private void OnExpandButtonClick(object sender, RoutedEventArgs e)
		{
			bool bIsOpen = false;
			bIsOpen = IsOpen;
			IsOpen = !bIsOpen;
		}

		private void OnDisplayModesStateChanged(object sender, VisualStateChangedEventArgs pArgs)
		{
			// We only fire the opened/closed events if we're changing our opened state (either
			// from open to closed or closed to open).  We don't fire the event if we changed
			// between 2 opened states or 2 closed states such as might happen when changing
			// closed display mode.
			if (m_isChangingOpenedState)
			{
				// Create the event args we'll use for our Opened/Closed events.
				var routedEventArgs = new RoutedEventArgs(this);

				var isOpen = IsOpen;

				if (isOpen)
				{
					OnOpened(routedEventArgs);
				}
				else
				{
					OnClosed(routedEventArgs);
				}

				m_isChangingOpenedState = false;
			}
		}

		protected virtual void UpdateTemplateSettings()
		{
			//
			// AppBar/CommandBar TemplateSettings and how they're used.
			//
			// The template settings are core to acheiving the desired experience
			// for AppBar/CommandBar at least to how it relates to the various
			// ClosedDisplayModes.
			//
			// This comment block will describe how the code uses TemplateSettings
			// to achieve the desired bar interation experience which is controlled
			// via the ClosedDisplayMode property.
			//
			// Anatomy of the bar component of an AppBar/CommandBar:
			//
			//  !==================================================!
			//  !                  Clip Rectangle                  !
			//  !                                                  !
			//  ! |----------------------------------------------| !
			//  ! |                                              | !
			//  ! |                 Content Root                 | !
			//  ! |                                              | !
			//  !=|==============================================|=!
			//    |::::::::::::::::::::::::::::::::::::::::::::::|
			//    |::::::::::::::::::::::::::::::::::::::::::::::|
			//    |----------------------------------------------|
			//
			// * The region covered in '::' is clipped away.
			//
			// ** The diagram shows the clip rect wider than the content, but
			//    that is just done to make it more readable.  In reality, they
			//    are the same width.
			//
			// When we measure and arrange an AppBar/CommandBar, the size we return
			// as our desired sized (in the case of measure) and the final size
			// (in the case of arrange) depends on the closed display mode.  We
			// measure our sub-tree normally but we modify the returned height to
			// make it match our closed display mode.
			//
			// This causes the control to get arranged such that the top portion
			// of the content root that is within our closed display mode height
			// will be visible, while the rest that is below will get covered up
			// by other content.  It's similar to if we had a negative margin on
			// the bottom.
			//
			// The clip rectangle is then used to make sure this bottom portion does
			// not get rendered; so we are left with just the top portion representing
			// our closed display mode.
			//
			// This is where the template settings start to play a part.  We need
			// to make sure to translate the clip rectangle up by a value that is equal
			// to the difference between the content's height and our closed display
			// mode height.  Since we want to translate up, we have to make that value
			// negative, which results in this equation:
			//
			//      VerticalDelta = ClosedDisplayMode_Height - ContentHeight
			//
			// This value is calculated for each of our supported ClosedDisplayModes
			// and is then used in our template & VSM to create the Closed/OpenUp/OpenDown
			// experiences.
			//
			// We apply it in the following ways to achieve our various states:
			//
			//     Closed:
			//      - Clip Rectangle translated by VerticalDelta (essentially translated up).
			//      - Content Root not translated.
			//
			//     OpenUp:
			//      - Clip Rectangle translated by VerticalDelta (essentially translated up).
			//      - Content Root translated by VerticalDelta (essentially translated up).
			//
			//     OpenDown:
			//      - Clip Rectangle not translated.
			//      - Content Root not translated.
			//

			var templateSettings = TemplateSettings;

			var actualWidth = ActualWidth;

			var contentHeight = m_contentHeight;

			templateSettings.ClipRect = new Rect(0, 0, actualWidth, contentHeight);

			double compactVerticalDelta = m_compactHeight - contentHeight;
			templateSettings.CompactVerticalDelta = compactVerticalDelta;
			templateSettings.NegativeCompactVerticalDelta = -compactVerticalDelta;

			double minimalVerticalDelta = m_minimalHeight - contentHeight;
			templateSettings.MinimalVerticalDelta = minimalVerticalDelta;
			templateSettings.NegativeMinimalVerticalDelta = -minimalVerticalDelta;

			templateSettings.HiddenVerticalDelta = -contentHeight;
			templateSettings.NegativeHiddenVerticalDelta = contentHeight;

			if (m_hasUpdatedTemplateSettings)
			{
				UpdateVisualState();

				// We wait until after the first call to update template settings to query DisplayModesStates VSG
				// to to prevent a performance hit on app startup
				TryQueryDisplayModesStatesGroup();

				// Force animations that reference our template settings in the current visual state
				// to update their bindings.
				VisualStateGroup? displayModesStateGroup = null;
				if (m_tpDisplayModesStateGroupRef?.TryGetTarget(out displayModesStateGroup) ?? false)
				{
					var currentState = displayModesStateGroup?.CurrentState;

					if (currentState is { })
					{
						var storyboard = currentState.Storyboard;
						if (storyboard is { })
						{
							storyboard.Begin();
							storyboard.SkipToFill();
						}
					}
				}
			}
			m_hasUpdatedTemplateSettings = true;
		}

		protected bool GetShouldOpenUp()
		{
			// Top appbars always open down.  All other appbars by default
			// open up.
			bool shouldOpenUp = (m_Mode != AppBarMode.Top);

			// If the appbar is inline, check to see if opening up would
			// cause the appbar to appear partially offscreen and if so
			// switch to opening down instead.
			if (m_Mode == AppBarMode.Inline)
			{
				var transform = TransformToVisual(null);

				GetVerticalOffsetNeededToOpenUp(out var offsetNeededToOpenUp, out var opensWindowed);


				// Subtract layout bounds to avoid using the System Tray area to open the AppBar.
				var offsetFromRootOpenedUp = transform.TransformPoint(new Point(0, -offsetNeededToOpenUp));

				var layoutBounds = new Rect();

				if (opensWindowed)
				{
					// UNO TODO: Windowed modes are not supported
					//wf::Point topLeftPoint = { 0, 0 };
					//IFC_RETURN(transform->TransformPoint(topLeftPoint, &topLeftPoint));

					//IFC_RETURN(DXamlCore::GetCurrent()->CalculateAvailableMonitorRect(this, topLeftPoint, &layoutBounds));
				}
				else
				{
					layoutBounds = XamlRoot?.Bounds ?? Microsoft.UI.Xaml.Window.CurrentSafe?.Bounds ?? default;

					if (WinUICoreServices.Instance.InitializationType == InitializationType.IslandsOnly)
					{
						layoutBounds = XamlRoot?.VisualTree.VisibleBounds ?? new();
					}

					shouldOpenUp = offsetFromRootOpenedUp.Y >= layoutBounds.Y;
				}
			}

			return shouldOpenUp;

		}

		protected virtual void GetVerticalOffsetNeededToOpenUp(out double neededOffset, out bool opensWindowed)
		{
			double verticalDelta = 0d;
			var templateSettings = TemplateSettings;

			var closedDisplayMode = ClosedDisplayMode;

			verticalDelta = closedDisplayMode switch
			{
				AppBarClosedDisplayMode.Compact => templateSettings.CompactVerticalDelta,
				AppBarClosedDisplayMode.Minimal => templateSettings.MinimalVerticalDelta,
				_ => templateSettings.HiddenVerticalDelta,
			};

			neededOffset = -verticalDelta;
			opensWindowed = false;
		}

		// This is an internal method used in CommandBar-related workarounds without
		// breaking the public API
		internal bool TryDismissInlineAppBarInternal() => TryDismissInlineAppBar();

		protected bool TryDismissInlineAppBar()
		{
			MUX_ASSERT(m_Mode == AppBarMode.Inline);

			bool isAppBarDismissed = false;

			var isSticky = IsSticky;
			if (!isSticky)
			{
				var isOpen = IsOpen;
				if (isOpen)
				{
					isAppBarDismissed = true;
				}

				IsOpen = false;
			}

			return isAppBarDismissed;
		}

		private void SetFocusOnAppBar()
		{
			MUX_ASSERT(m_Mode == AppBarMode.Inline);

			var focusedElement = this.GetFocusedElement();

			// Only steal focus if focus isn't already within the appbar.
			if (focusedElement is { } && !this.IsAncestorOf(focusedElement))
			{
				m_savedFocusedElementWeakRef = new WeakReference<DependencyObject>(focusedElement);

				if (focusedElement is Control focusedElementAsControl && focusedElementAsControl.FocusState != FocusState.Unfocused)
				{
					m_savedFocusState = focusedElementAsControl.FocusState;
				}
				else
				{
					m_savedFocusState = FocusState.Programmatic;
				}

				var firstFocusableElement = FocusManager.FindFirstFocusableElement(this);
				if (firstFocusableElement is { })
				{
					this.SetFocusedElement(firstFocusableElement, m_savedFocusState, animateIfBringIntoView: false);
				}
			}
		}

		private void RestoreSavedFocus()
		{
			MUX_ASSERT(m_Mode == AppBarMode.Inline);

			DependencyObject? savedFocusedElement = null;

			_ = m_savedFocusedElementWeakRef?.TryGetTarget(out savedFocusedElement);

			RestoreSavedFocusImpl(savedFocusedElement, m_savedFocusState);

			m_savedFocusedElementWeakRef = null;

			m_savedFocusState = FocusState.Unfocused;
		}


		protected virtual void RestoreSavedFocusImpl(DependencyObject? savedFocusedElement, FocusState savedFocusState)
		{
			if (savedFocusedElement is { })
			{
				this.SetFocusedElement(savedFocusedElement, m_savedFocusState, animateIfBringIntoView: false);
			}
		}

		private bool RefreshContentHeight()
		{
			double oldHeight = m_contentHeight;

			if (m_tpContentRoot is { })
			{
				m_contentHeight = m_tpContentRoot.ActualHeight;
			}

			return oldHeight != m_contentHeight;
		}

		private void SetupOverlayState()
		{
			MUX_ASSERT(m_Mode == AppBarMode.Inline);
			MUX_ASSERT(!m_isInOverlayState);
			// The approach used to achieve light-dismiss is to create a 1x1 element that is added
			// as the first child of our layout root panel.  Adding it as the first child ensures that
			// it is below our actual content and will therefore not affect the content area's hit-testing.
			// We then use a scale transform to scale up an LTE targeted to the element to match the
			// dimensions of our window.  Finally, we translate that same LTE to make sure it's upper-left
			// corner aligns with the window's upper left corner, causing it to cover the entire window.
			// A pointer pressed handler is attached to the element to intercept any pointer
			// input that is not directed at the actual content.  The value of AppBar.IsSticky controls
			// whether the light-dismiss element is hit-testable (IsSticky=True -> hit-testable=False).
			// The pointer pressed handler simply closes the appbar and marks the routed event args
			// message as handled.
			if (m_tpLayoutRoot is { })
			{
				// Create our overlay element if necessary.
				if (m_overlayElement == null)
				{
					var rectangle = new Rectangle();
					rectangle.Width = 1;
					rectangle.Height = 1;
					rectangle.UseLayoutRounding = false;

					var isSticky = IsSticky;
					rectangle.IsHitTestVisible = !isSticky;

					rectangle.PointerPressed += OnOverlayElementPointerPressed;
					m_overlayElementPointerPressedEventHandler.Disposable = Disposable.Create(() => rectangle.PointerPressed -= OnOverlayElementPointerPressed);

					m_overlayElement = rectangle;

					UpdateOverlayElementBrush();
				}

				// Add our overlay element to our layout root panel.
				m_tpLayoutRoot.Children.Insert(0, m_overlayElement);
			}

			//CreateLTEs();

			// Update the animations to target the newly created overlay element LTE.
			if (m_isOverlayVisible)
			{
				UpdateTargetForOverlayAnimations();
			}

			m_isInOverlayState = true;
		}

		private void TeardownOverlayState()
		{
			MUX_ASSERT(m_Mode == AppBarMode.Inline);
			MUX_ASSERT(m_isInOverlayState);

			//DestroyLTEs();

			// Remove our light-dismiss element from our layout root panel.
			if (m_tpLayoutRoot is { })
			{
				var indexOfOverlayElement = m_tpLayoutRoot.Children.IndexOf(m_overlayElement);
				MUX_ASSERT(indexOfOverlayElement != -1);

				if (indexOfOverlayElement != -1)
				{
					m_tpLayoutRoot.Children.RemoveAt(indexOfOverlayElement);
				}
			}

			m_isInOverlayState = false;
		}

		//AppBar::CreateLTEs()
		//{
		//	ASSERT(!m_layoutTransitionElement);
		//		ASSERT(!m_overlayLayoutTransitionElement);
		//		ASSERT(!m_parentElementForLTEs);

		//	// If we're under the PopupRoot or FullWindowMediaRoot, then we'll explicitly set
		//	// our LTE's parent to make sure the LTE doesn't get placed under the TransitionRoot,
		//	// which is lower in z-order than these other roots.
		//	if (ShouldUseParentedLTE())
		//	{
		//		ctl::ComPtr<xaml::IDependencyObject> parent;
		//		IFC_RETURN(VisualTreeHelper::GetParentStatic(this, &parent));
		//		IFCEXPECT_RETURN(parent);

		//		IFC_RETURN(SetPtrValueWithQI(m_parentElementForLTEs, parent.Get()));
		//	}

		//	if (m_overlayElement)
		//	{
		//		// Create an LTE for our overlay element.
		//IFC_RETURN(LayoutTransitionElement_Create(
		//	DXamlCore::GetCurrent()->GetHandle(),
		//	m_overlayElement.Cast<FrameworkElement>()->GetHandle(),
		//	m_parentElementForLTEs? m_parentElementForLTEs.Cast<UIElement>()->GetHandle() : nullptr,
		//	false /*isAbsolutelyPositioned*/,
		//	m_overlayLayoutTransitionElement.ReleaseAndGetAddressOf()
		//));

		//		// Configure the overlay LTE.
		//		{
		//			ctl::ComPtr<DependencyObject> overlayLTEPeer;
		//	IFC_RETURN(DXamlCore::GetCurrent()->GetPeer(m_overlayLayoutTransitionElement.get(), &overlayLTEPeer));

		//			wf::Rect windowBounds = { };
		//	IFC_RETURN(DXamlCore::GetCurrent()->GetContentBoundsForElement(GetHandle(), &windowBounds));

		//			ctl::ComPtr<CompositeTransform> compositeTransform;
		//	IFC_RETURN(ctl::make(&compositeTransform));

		//			IFC_RETURN(compositeTransform->put_ScaleX(windowBounds.Width));
		//	IFC_RETURN(compositeTransform->put_ScaleY(windowBounds.Height));

		//	IFC_RETURN(overlayLTEPeer.Cast<UIElement>()->put_RenderTransform(compositeTransform.Get()));

		//			ctl::ComPtr<xaml_media::IGeneralTransform> transformToVisual;
		//	IFC_RETURN(m_overlayElement.Cast<FrameworkElement>()->TransformToVisual(nullptr, &transformToVisual));

		//			wf::Point offsetFromRoot = { };
		//	IFC_RETURN(transformToVisual->TransformPoint({ 0, 0 }, &offsetFromRoot));

		//			auto flowDirection = xaml::FlowDirection_LeftToRight;
		//	IFC_RETURN(get_FlowDirection(&flowDirection));

		//			// Translate the light-dismiss layer so that it is positioned at the top-left corner of the window (for LTR cases)
		//			// or the top-right corner of the window (for RTL cases).
		//			// TransformToVisual(nullptr) will return an offset relative to the top-left corner of the window regardless of
		//			// flow direction, so for RTL cases subtract the window width from the returned offset.x value to make it relative
		//			// to the right edge of the window.
		//			IFC_RETURN(compositeTransform->put_TranslateX(flowDirection == xaml::FlowDirection_LeftToRight? -offsetFromRoot.X : offsetFromRoot.X - windowBounds.Width));
		//			IFC_RETURN(compositeTransform->put_TranslateY(-offsetFromRoot.Y));
		//		}
		//	}

		//	IFC_RETURN(LayoutTransitionElement_Create(
		//		DXamlCore::GetCurrent()->GetHandle(),
		//		GetHandle(),
		//		m_parentElementForLTEs ? m_parentElementForLTEs.Cast<UIElement>()->GetHandle() : nullptr,
		//		false /*isAbsolutelyPositioned*/,
		//		m_layoutTransitionElement.ReleaseAndGetAddressOf()
		//	));

		//// Forward our control's opacity to the LTE since it doesn't happen automatically.
		//{
		//	double opacity = 0.0;
		//	IFC_RETURN(get_Opacity(&opacity));
		//	IFC_RETURN(m_layoutTransitionElement->SetValueByKnownIndex(KnownPropertyIndex::UIElement_Opacity, static_cast<float>(opacity)));
		//}

		//IFC_RETURN(PositionLTEs());

		//return S_OK;
		//}

		//_Check_return_ HRESULT
		//AppBar::PositionLTEs()
		//{
		//	ASSERT(m_layoutTransitionElement);

		//	ctl::ComPtr<xaml::IDependencyObject> parentDO;
		//	ctl::ComPtr<xaml::IUIElement> parent;

		//	IFC_RETURN(VisualTreeHelper::GetParentStatic(this, &parentDO));

		//	// If we don't have a parent, then there's nothing for us to do.
		//	if (parentDO)
		//	{
		//		IFC_RETURN(parentDO.As(&parent));

		//		ctl::ComPtr<xaml_media::IGeneralTransform> transform;
		//		IFC_RETURN(TransformToVisual(parent.Cast<UIElement>(), &transform));

		//		wf::Point offset = { };
		//		IFC_RETURN(transform->TransformPoint({ 0, 0 }, &offset));

		//		IFC_RETURN(LayoutTransitionElement_SetDestinationOffset(m_layoutTransitionElement, offset.X, offset.Y));
		//	}

		//	return S_OK;
		//}

		//_Check_return_ HRESULT
		//AppBar::DestroyLTEs()
		//{
		//	if (m_layoutTransitionElement)
		//	{
		//		IFC_RETURN(LayoutTransitionElement_Destroy(
		//			DXamlCore::GetCurrent()->GetHandle(),
		//			GetHandle(),
		//			m_parentElementForLTEs ? m_parentElementForLTEs.Cast<UIElement>()->GetHandle() : nullptr,
		//			m_layoutTransitionElement.get()
		//		));

		//		m_layoutTransitionElement.reset();
		//	}

		//	if (m_overlayLayoutTransitionElement)
		//	{
		//		// Destroy our light-dismiss element's LTE.
		//		IFC_RETURN(LayoutTransitionElement_Destroy(
		//			DXamlCore::GetCurrent()->GetHandle(),
		//			m_overlayElement.Cast<FrameworkElement>()->GetHandle(),
		//			m_parentElementForLTEs ? m_parentElementForLTEs.Cast<UIElement>()->GetHandle() : nullptr,
		//			m_overlayLayoutTransitionElement.get()
		//			));

		//		m_overlayLayoutTransitionElement.reset();
		//	}

		//	m_parentElementForLTEs.Clear();

		//	return S_OK;
		//}


		private void OnOverlayElementPointerPressed(object sender, PointerRoutedEventArgs e)
		{
			MUX_ASSERT(m_Mode == AppBarMode.Inline);

			TryDismissInlineAppBar();
			e.Handled = true;
		}

		private void TryQueryDisplayModesStatesGroup()
		{
			if (m_tpDisplayModesStateGroupRef == null)
			{
				GetTemplatePart<VisualStateGroup>("DisplayModeStates", out var displayModesStateGroup);

				m_tpDisplayModesStateGroupRef?.SetTarget(displayModesStateGroup);

				VisualStateGroup? group = null;
				if (m_tpDisplayModesStateGroupRef?.TryGetTarget(out group) ?? false)
				{
					if (group != null)
					{
						group.CurrentStateChanged += OnDisplayModesStateChanged;
						m_displayModeStateChangedEventHandler.Disposable = Disposable.Create(() => group.CurrentStateChanged -= OnDisplayModesStateChanged);
					}
				}
			}
		}

		//_Check_return_ HRESULT
		//AppBar::OnBackButtonPressedImpl(_Out_ BOOLEAN* pHandled)
		//{

		//	BOOLEAN isOpen = FALSE;
		//		BOOLEAN isSticky = FALSE;

		//		IFCPTR_RETURN(pHandled);

		//		IFC_RETURN(get_IsOpen(&isOpen));
		//	IFC_RETURN(get_IsSticky(&isSticky));
		//	if (isOpen && !isSticky)
		//	{
		//		IFC_RETURN(put_IsOpen(FALSE));
		//		*pHandled = TRUE;

		//		if (m_Mode != AppBarMode_Inline)
		//		{
		//			ctl::ComPtr<IApplicationBarService> spApplicationBarService;
		//		IFC_RETURN(DXamlCore::GetCurrent()->GetApplicationBarService(spApplicationBarService));
		//		IFC_RETURN(spApplicationBarService->CloseAllNonStickyAppBars());
		//	}
		//}

		//return S_OK;
		//}

		private void ReevaluateIsOverlayVisible()
		{
			bool isOverlayVisible = false;
			var overlayMode = LightDismissOverlayMode;

			if (overlayMode == LightDismissOverlayMode.Auto)
			{
				isOverlayVisible = SharedHelpers.IsOnXbox();
			}
			else
			{
				isOverlayVisible = overlayMode == LightDismissOverlayMode.On;
			}

			// Only inline app bars can enable their overlays.  Top/Bottom/Floating will use
			// the overlay from the ApplicationBarService.
			isOverlayVisible = isOverlayVisible && (m_Mode == AppBarMode.Inline);

			if (isOverlayVisible != m_isOverlayVisible)
			{
				m_isOverlayVisible = isOverlayVisible;

				if (m_isOverlayVisible)
				{
					if (m_isInOverlayState)
					{
						UpdateTargetForOverlayAnimations();
					}
				}
				else
				{
					// Make sure we've stopped our animations.
					if (m_overlayOpeningStoryboard is { })
					{
						m_overlayOpeningStoryboard.Stop();
					}

					if (m_overlayClosingStoryboard is { })
					{
						m_overlayClosingStoryboard.Stop();
					}
				}

				if (m_overlayElement is { })
				{
					UpdateOverlayElementBrush();
				}
			}
		}

		private void UpdateOverlayElementBrush()
		{
			MUX_ASSERT(m_overlayElement is { });

			if (m_isOverlayVisible)
			{
				// Create a theme resource for the overlay brush.
				//auto core = DXamlCore::GetCurrent()->GetHandle();
				//auto dictionary = core->GetThemeResources();

				//xstring_ptr themeBrush;
				//IFC_RETURN(xstring_ptr::CloneBuffer(L"AppBarLightDismissOverlayBackground", &themeBrush));

				//CDependencyObject* initialValueNoRef = nullptr;
				//IFC_RETURN(dictionary->GetKeyNoRef(themeBrush, &initialValueNoRef));

				//CREATEPARAMETERS cp(core);
				//xref_ptr<CThemeResourceExtension> themeResourceExtension;
				//IFC_RETURN(CThemeResourceExtension::Create(
				//	reinterpret_cast<CDependencyObject**>(themeResourceExtension.ReleaseAndGetAddressOf()),
				//	&cp));

				//themeResourceExtension->m_strResourceKey = themeBrush;

				//IFC_RETURN(themeResourceExtension->SetInitialValueAndTargetDictionary(initialValueNoRef, dictionary));

				//IFC_RETURN(themeResourceExtension->SetThemeResourceBinding(
				//	m_overlayElement.Cast<FrameworkElement>()->GetHandle(),
				//	DirectUI::MetadataAPI::GetPropertyByIndex(KnownPropertyIndex::Shape_Fill))
				//	);

				var oBrush = ResourceResolver.ResolveTopLevelResource("AppBarLightDismissOverlayBackground");
				if (oBrush is Brush brush)
				{
					if (m_overlayElement is Rectangle rectangle)
					{
						rectangle.Fill = brush;
					}
				}
			}
			else
			{
				var transparentBrush = SolidColorBrushHelper.Transparent;

				if (m_overlayElement is Rectangle rectangle)
				{
					rectangle.Fill = transparentBrush;
				}
			}
		}

		private void UpdateTargetForOverlayAnimations()
		{
			//MUX_ASSERT(m_layoutTransitionElement is { });
			MUX_ASSERT(m_isOverlayVisible);

			if (m_overlayOpeningStoryboard is { })
			{
				m_overlayOpeningStoryboard.Stop();

				Storyboard.SetTarget(m_overlayOpeningStoryboard, m_overlayLayoutTransitionElement);
			}

			if (m_overlayClosingStoryboard is { })
			{
				m_overlayClosingStoryboard.Stop();

				Storyboard.SetTarget(m_overlayClosingStoryboard, m_overlayLayoutTransitionElement);
			}
		}

		private void PlayOverlayOpeningAnimation()
		{
			MUX_ASSERT(m_isInOverlayState);
			MUX_ASSERT(m_isOverlayVisible);

			if (m_overlayClosingStoryboard is { })
			{
				m_overlayClosingStoryboard.Stop();
			}

			if (m_overlayOpeningStoryboard is { })
			{
				m_overlayOpeningStoryboard.Begin();
			}
		}

		private void PlayOverlayClosingAnimation()
		{
			MUX_ASSERT(m_isInOverlayState);
			MUX_ASSERT(m_isOverlayVisible);

			if (m_overlayOpeningStoryboard is { })
			{
				m_overlayOpeningStoryboard.Stop();
			}

			if (m_overlayClosingStoryboard is { })
			{
				m_overlayClosingStoryboard.Begin();
			}
		}

		protected void GetTemplatePart<T>(string name, out T? element) where T : class
		{
			element = GetTemplateChild(name) as T;
		}
	}
}
