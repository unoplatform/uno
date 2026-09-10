// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Page_Partial.cpp

#nullable enable

using System;
using System.Runtime.CompilerServices;
using DirectUI;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Microsoft.UI.Xaml.Controls;

public partial class Page
{
	private const float PageApplyingLayoutBoundsTolerance = 0.1f;

	private string m_descriptor;
	private bool m_shouldRegisterNewAppbars;

	private IDisposable? m_layoutBoundsChangedRegistration;
	private Rect m_mostRecentLayoutBounds;

	private protected override void OnLoaded()
	{
		base.OnLoaded();

		RegisterAppBars();

		var spCurrentFocusedElement = this.GetFocusedElement();

		var focusManager = VisualTree.GetFocusManagerForElement(this);
		bool setDefaultFocus = focusManager?.IsPluginFocused() == true;

		if (setDefaultFocus && spCurrentFocusedElement == null)
		{
			// Uno specific: If the page is focusable itself, we want to
			// give it focus instead of the first element.
			if (FocusProperties.IsFocusable(this))
			{
				this.SetFocusedElement(
					this,
					FocusState.Programmatic,
					animateIfBringIntoView: false);
				return;
			}

			// Set the focus on the first focusable control
			var spFirstFocusableElementCDO = focusManager?.GetFirstFocusableElement(this);

			if (spFirstFocusableElementCDO != null && focusManager != null)
			{
				var spFirstFocusableElementDO = spFirstFocusableElementCDO;

				focusManager.InitialFocus = true;

				TrySetFocusedElement(spFirstFocusableElementDO);

				focusManager.InitialFocus = false;
			}

			if (spFirstFocusableElementCDO == null)
			{
				// Narrator listens for focus changed events to determine when the UI Automation tree needs refreshed. If we don't set default focus (on Phone) or if we fail to find a focusable element,
				// we will need notify the narror of the UIA tree change when page is loaded.
				var bAutomationListener = AutomationPeer.ListenerExistsHelper(AutomationEvents.AutomationFocusChanged);

				if (bAutomationListener)
				{
					Uno.UI.Xaml.Core.CoreServices.Instance.UIARaiseFocusChangedEventOnUIAWindow(this);
				}
			}
		}
	}

	private protected override void OnUnloaded()
	{
		base.OnUnloaded();

		m_layoutBoundsChangedRegistration?.Dispose();
		m_layoutBoundsChangedRegistration = null;

		UnregisterAppBars();
	}

	/// <remarks>
	/// This method contains or is called by a try/catch containing method and
	/// can be significantly slower than other methods as a result on WebAssembly.
	/// See https://github.com/dotnet/runtime/issues/56309
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void TrySetFocusedElement(DependencyObject spFirstFocusableElementDO)
	{
		try
		{
			var focusUpdated = this.SetFocusedElement(
				spFirstFocusableElementDO,
				FocusState.Programmatic,
				false /*animateIfBringIntoView*/);
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().LogError($"Setting initial page focus failed: {ex}");
			}
		}
	}

	internal void SetDescriptor(string descriptor)
	{
		m_descriptor = descriptor;
	}


	internal void InvokeOnNavigatedFrom(
		object contentObject,
		object parameterObject,
		NavigationTransitionInfo pTransitionInfo,
		string descriptor,
		NavigationMode navigationMode)
	{
		if (descriptor == null)
		{
			throw new ArgumentNullException(nameof(descriptor));
		}

		if (contentObject is null)
		{
			throw new ArgumentNullException(nameof(contentObject));
		}

		var spINavigationEventArgs = NavigationHelpers.CreateINavigationEventArgs(contentObject, parameterObject, pTransitionInfo, descriptor, navigationMode);
		OnNavigatedFrom(spINavigationEventArgs);
	}

	internal void InvokeOnNavigatedTo(
		object contentObject,
		object parameterObject,
		NavigationTransitionInfo pTransitionInfo,
		string descriptor,
		NavigationMode navigationMode)
	{
		if (descriptor == null)
		{
			throw new ArgumentNullException(nameof(descriptor));
		}

		if (contentObject is null)
		{
			throw new ArgumentNullException(nameof(contentObject));
		}


		NavigationEventArgs spINavigationEventArgs = NavigationHelpers.CreateINavigationEventArgs(contentObject, parameterObject, pTransitionInfo, descriptor, navigationMode);
		OnNavigatedTo(spINavigationEventArgs);

		// Set Automation Page Navigation complete event.
		// TODO:MZ: Implement this
		//if (DXamlCore.Current.HasPageNavigationCompleteEvent())
		//{
		//	DXamlCore.Current.SetPageNavigationCompleteEvent();
		//}
	}

	internal void InvokeOnNavigatingFrom(
		object parameterObject,
		NavigationTransitionInfo transitionInfo,
		string descriptor,
		NavigationMode navigationMode,
		out bool isCanceled)
	{
		if (descriptor == null)
		{
			throw new ArgumentNullException(nameof(descriptor));
		}

		var spINavigatingCancelEventArgs = NavigationHelpers.CreateINavigatingCancelEventArgs(parameterObject, transitionInfo, descriptor, navigationMode);
		OnNavigatingFrom(spINavigatingCancelEventArgs);
		isCanceled = spINavigatingCancelEventArgs.Cancel;
	}

	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		base.OnPropertyChanged2(args);

		if (args.Property == TopAppBarProperty || args.Property == BottomAppBarProperty)
		{
			var newAppBarMode = args.Property == BottomAppBarProperty ? AppBarMode.Bottom : AppBarMode.Top;
			var oldAppBar = args.OldValue as AppBar;
			var newAppBar = args.NewValue as AppBar;

			double oldClosedHeight = 0.0;
			double newClosedHeight = 0.0;

			// First, grab the new AppBar value and carry out important stateful operations
			if (newAppBar is not null)
			{
				newClosedHeight = GetAppBarClosedHeight(newAppBar);
				newAppBar.Mode = newAppBarMode;
			}

			// XamlRoot may not yet be available if called before OnLoaded. Nothing further to do.
			var xamlRoot = XamlRoot.GetForElement(this, createIfNotExist: false);
			if (xamlRoot is null)
			{
				return;
			}

			var applicationBarService = xamlRoot.GetApplicationBarService();

			// Unregister the old app bar
			if (oldAppBar is not null)
			{
				oldClosedHeight = GetAppBarClosedHeight(oldAppBar);
				applicationBarService.UnregisterApplicationBar(oldAppBar);
				oldAppBar.SetOwner(null);
				oldAppBar.Mode = AppBarMode.Inline;
			}

			// Register the new app bar
			if (newAppBar is not null && m_shouldRegisterNewAppbars)
			{
				newAppBar.SetOwner(this);
				applicationBarService.RegisterApplicationBar(newAppBar, newAppBarMode);

				// Forward the data context to the new app bar only when we're on the live tree.
				// The DC is not guaranteed correct unless the Page is on the live tree for any other bindings.
				// We can save some time while building the tree.
				// Once the page enters the tree its DataContext will be propagated down, including to the AppBars.
				newAppBar.DataContext = DataContext;
			}

			if (Math.Abs(newClosedHeight - oldClosedHeight) > PageApplyingLayoutBoundsTolerance)
			{
				AppBarClosedSizeChanged();
			}
		}
		else if (args.Property == NavigationCacheModeProperty)
		{
			Frame pFrame = Frame;
			if (pFrame is not null)
			{
				NavigationCacheMode navigationCacheMode = NavigationCacheMode;

				// Remove the page from Cache if the NavigationCacheMode is set to Disabled.
				// We don't handle the transition from Disabled to Enabled/Required since we
				// do not have any scenarios that need it. The Caching (if NavigationCacheMode
				// is Enabled/Required) that is done as a part navigation when content is loaded
				// covers all the scenarios.
				if (navigationCacheMode == NavigationCacheMode.Disabled)
				{
					// If there is more than one page of the same type (descriptor) in the PageStack,
					// it will be uncached even if one of the pages disables the CacheMode, even if the
					// other pages of the same type have the CacheMode set to Enabled/Required.
					// This is because content is cached per type and any number of pages with the
					// same type will have only one entry in the Cache.
					pFrame.RemovePageFromCache(m_descriptor);
				}
			}
		}
	}

	// MUX Reference Page_Partial.cpp, tag winui3/release/1.7.1
	private void RegisterAppBars()
	{
		var topBar = TopAppBar;
		var bottomBar = BottomAppBar;

		if (topBar is not null || bottomBar is not null)
		{
			var xamlRoot = XamlRoot.GetForElement(this);
			if (xamlRoot is null)
			{
				return;
			}

			var applicationBarService = xamlRoot.GetApplicationBarService();

			if (topBar is not null)
			{
				topBar.SetOwner(this);
				applicationBarService.RegisterApplicationBar(topBar, AppBarMode.Top);
			}

			if (bottomBar is not null)
			{
				bottomBar.SetOwner(this);
				applicationBarService.RegisterApplicationBar(bottomBar, AppBarMode.Bottom);
			}
		}

		m_shouldRegisterNewAppbars = true;
	}

	// MUX Reference Page_Partial.cpp, tag winui3/release/1.7.1
	private void UnregisterAppBars()
	{
		var topBar = TopAppBar;
		var bottomBar = BottomAppBar;

		if (topBar is not null || bottomBar is not null)
		{
			var xamlRoot = XamlRoot.GetForElement(this);
			if (xamlRoot is null)
			{
				return;
			}

			var applicationBarService = xamlRoot.GetApplicationBarService();

			if (topBar is not null)
			{
				applicationBarService.UnregisterApplicationBar(topBar);
				topBar.SetOwner(null);
			}

			if (bottomBar is not null)
			{
				applicationBarService.UnregisterApplicationBar(bottomBar);
				bottomBar.SetOwner(null);
			}
		}

		m_shouldRegisterNewAppbars = false;
	}

	// MUX Reference Page_Partial.cpp, tag winui3/release/1.7.1
	protected override Size MeasureOverride(Size availableSize)
	{
		var child = Content;

		if (child is null)
		{
			// Uno specific: WinUI's CUserControl::ApplyTemplate is a final no-op, so a Page there can
			// never have a templated child to fall back to. Uno expands Control templates normally, so
			// defer to Control.MeasureOverride, which measures the first child - and which also returns
			// 0x0 when there is genuinely no child, as Page_Partial.cpp does.
			return base.MeasureOverride(availableSize);
		}

		var availableBounds = new Rect(0, 0, availableSize.Width, availableSize.Height);

		// Get the new available bounds that can also applied the core window's layout bounds if the
		// current page is the same size of the full core window size.
		CalculateUpdatedBounds(ref availableBounds);

		availableSize.Width = availableBounds.Width;
		availableSize.Height = availableBounds.Height;

		var measuredSize = MeasureElement(child, availableSize);

		// Uno specific: mirrors Control.MeasureOverride - a Collapsed child would otherwise
		// never get layout storage, and callers read its slot before the next arrange.
		child.EnsureLayoutStorage();

		return measuredSize;
	}

	// MUX Reference Page_Partial.cpp, tag winui3/release/1.7.1
	protected override Size ArrangeOverride(Size arrangeSize)
	{
		var child = Content;

		if (child is not null)
		{
			var arrangeBounds = new Rect(0, 0, arrangeSize.Width, arrangeSize.Height);

			// Get the new arranged bounds that applied the core window's layout bounds if the current
			// page is the same size of the full core window size.
			CalculateUpdatedBounds(ref arrangeBounds);

			ArrangeElement(child, arrangeBounds);
		}
		else
		{
			// Uno specific: see MeasureOverride. Control.ArrangeOverride arranges the templated child
			// and returns finalSize, so this stays 1:1 with Page_Partial.cpp when there is no child.
			base.ArrangeOverride(arrangeSize);
		}

		return arrangeSize;
	}

	// MUX Reference Page_Partial.cpp, tag winui3/release/1.7.1
	internal void AppBarClosedSizeChanged()
	{
		InvalidateLayoutForAppBarSizeChange();
	}

	// MUX Reference Page_Partial.cpp, tag winui3/release/1.7.1
	private void InvalidateLayoutForAppBarSizeChange()
	{
		if (IsInLiveTree)
		{
			var boundsMode = QueryDesiredBoundsMode();
			if (ApplicationViewBoundsMode.UseVisible == boundsMode)
			{
				InvalidateMeasure();
				InvalidateArrange();
			}
		}
	}

	// Returns a default value of UseVisible unless this method can get a valid ApplicationView
	// object to request the real desired bounds mode value.
	// MUX Reference Page_Partial.cpp, tag winui3/release/1.7.1
	private ApplicationViewBoundsMode QueryDesiredBoundsMode()
	{
		// default bounds mode is UseVisible
		// TODO Uno: Uno has no per-window ApplicationView, and ApplicationView.GetForCurrentView is
		// banned inside the framework, so there is no DesiredBoundsMode to read. This takes the same
		// fallback WinUI takes when GetForCurrentView fails.
		return ApplicationViewBoundsMode.UseVisible;
	}

	// MUX Reference Page_Partial.cpp, tag winui3/release/1.7.1
	private static double GetAppBarClosedHeight(AppBar appBar)
	{
		// Visibility.Collapsed doesn't change ActualHeight, but for layout
		// purposes we need the height to be reported as 0.0.
		if (appBar.Visibility != Visibility.Collapsed)
		{
			return appBar.ActualHeight;
		}

		return 0.0;
	}

	// Calculates how much size needs to be subtracted from arrange bounds to account for
	// appbar occlusion. topHeight is added to the arrange bounds Y as an offset, totalHeight is
	// subtracted from the arrange bounds Height as a space consumed calculation.
	// WinUI returns a Rect whose X and Width are always 0, because both bars are assumed to be
	// edge-to-edge horizontal bars regardless of the device orientation - hence the two values here.
	// MUX Reference Page_Partial.cpp, tag winui3/release/1.7.1
	private (double topHeight, double totalHeight) CalculateAppBarOcclusionDimensions()
	{
		double topAppBarHeight = 0.0;
		double bottomAppBarHeight = 0.0;

		var topAppBar = TopAppBar;
		if (topAppBar is not null)
		{
			topAppBarHeight = GetAppBarClosedHeight(topAppBar);
		}

		var bottomAppBar = BottomAppBar;
		if (bottomAppBar is not null)
		{
			bottomAppBarHeight = GetAppBarClosedHeight(bottomAppBar);
		}

		return (topAppBarHeight, topAppBarHeight + bottomAppBarHeight);
	}

	// MUX Reference Page_Partial.cpp, tag winui3/release/1.7.1
	private void CalculateUpdatedBounds(ref Rect arrangedBounds)
	{
		var dxamlCore = DXamlCore.Current;

		var currentWindowBounds = dxamlCore.GetContentBoundsForElement(this);

		var boundsMode = QueryDesiredBoundsMode();

		// This flag is added for Xbox, and is false by default. On Xbox, VisibleBounds represent the
		// "Title-Safe" area - which excludes regions along the edges that some TV's cannot show ("overscan").
		//
		// When false (default value), Page.Content layout will use layout bounds as usual. This ensures
		// that content will be visible on all TV's, but may leave unused edges around it.
		// 3rd party apps will generally use this option so that they don't have to think about overscan issues.
		//
		// When true, Page.Content layout will use CoreWindow bounds, which means drawing in the overscan
		// region. 1st party Xbox apps will generally use this option, since it gives them greater flexibility
		// and screen real estate, though they have to take care to manually layout items in title-safe area as needed.
		//
		// The reason the property is separate from Windows.UI.ViewManagement.ApplicationView.DesiredBoundsMode
		// is popup-based Xaml Controls that do their own placement. Apps cannot easily ensure that critical popups appear
		// in the title-safe area. Thus, we need to separate bounds used in page layout from those used internally in popup placement.
		var layoutToWindowBounds = IsLaidOutToWindowBounds();

		// Applied the core window's layout bounds margin to the child if the current page size is the same
		// core window size.
		var isLayoutBoundsApplied = false;
		if (Math.Abs(currentWindowBounds.Width - arrangedBounds.Width) < PageApplyingLayoutBoundsTolerance &&
			Math.Abs(currentWindowBounds.Height - arrangedBounds.Height) < PageApplyingLayoutBoundsTolerance)
		{
			isLayoutBoundsApplied = true;

			if (ApplicationViewBoundsMode.UseVisible == boundsMode && !layoutToWindowBounds)
			{
				// Get the current window layout bounds which is smaller than Window.Bounds by the size
				// of the OS rendered chrome (tray/navigation bar).  We'll additionally need to reduce this
				// rectangle by the size of the page appbars.
				m_mostRecentLayoutBounds = dxamlCore.GetContentLayoutBoundsForElement(this);

				// TODO Uno: WinUI additionally overwrites currentWindowBounds with the layout bounds on
				// desktop, because the two values are read from CoreWindow in separate calls and can
				// disagree. Uno reads both from the same XamlRoot, so there is nothing to reconcile.

				m_mostRecentLayoutBounds.Width = Math.Min(m_mostRecentLayoutBounds.Width, currentWindowBounds.Width);
				m_mostRecentLayoutBounds.Height = Math.Min(m_mostRecentLayoutBounds.Height, currentWindowBounds.Height);

				// if flow direction is RTL use the left margin between bounds and layoutbounds
				// if flow direction is LTR use the right margin between bounds and layoutbounds
				var flowDirection = FlowDirection;
				var arrangeX = flowDirection == FlowDirection.LeftToRight
					? m_mostRecentLayoutBounds.X - currentWindowBounds.X
					: currentWindowBounds.Width - m_mostRecentLayoutBounds.Width - (m_mostRecentLayoutBounds.X - currentWindowBounds.X);
				var arrangeY = m_mostRecentLayoutBounds.Y - currentWindowBounds.Y;

				// get the arrange offsets to account for appbar occlusion
				var (topHeight, totalHeight) = CalculateAppBarOcclusionDimensions();

				// Assigned member by member rather than through the Rect constructor, which rejects a
				// negative extent - a pair of bars taller than the window produces one, and the layout
				// core clamps it to zero the same way WinUI's does.
				arrangedBounds.X = arrangeX;
				arrangedBounds.Y = arrangeY + topHeight;
				arrangedBounds.Width = m_mostRecentLayoutBounds.Width;
				arrangedBounds.Height = m_mostRecentLayoutBounds.Height - totalHeight;
			}
			else // ApplicationViewBoundsMode.UseCoreWindow == boundsMode
			{
				// In this case Window.Bounds and Window.LayoutBounds are the same by definition.  The page
				// content should be occluded by both the OS chrome as well as the appbars.  So there's
				// no need to include the appbar occlusion in the returned value.

				// remove positional information from window bounds to translate into window coordinates
				m_mostRecentLayoutBounds = currentWindowBounds;
				arrangedBounds = new Rect(0, 0, currentWindowBounds.Width, currentWindowBounds.Height);
			}
		}
		else if (ApplicationViewBoundsMode.UseVisible == boundsMode)
		{
			// if this page isn't the full size of the window it shouldn't attempt to avoid the
			// OS chrome but still should avoid its own appbars if boundsMode is UseVisible

			// get the arrange offsets to account for appbar occlusion
			var (topHeight, totalHeight) = CalculateAppBarOcclusionDimensions();

			arrangedBounds.Y += topHeight;
			arrangedBounds.Height -= totalHeight;
		}

		// Update the core window layout bounds changed event handler for add/remove event.
		UpdateWindowLayoutBoundsChangedEvent(isLayoutBoundsApplied);
	}

	// MUX Reference Page_Partial.cpp, tag winui3/release/1.7.1
	private void UpdateWindowLayoutBoundsChangedEvent(bool isLayoutBoundsApplied)
	{
		var xamlRoot = XamlRoot.GetForElement(this, createIfNotExist: false);
		if (xamlRoot is null)
		{
			return;
		}

		if (isLayoutBoundsApplied)
		{
			if (m_layoutBoundsChangedRegistration is null)
			{
				// TODO Uno: WinUI subscribes to the XamlRoot's LayoutBoundsHelper, which reports the
				// layout bounds separately from the content bounds. Uno has no such source yet (see
				// DXamlCore.GetContentLayoutBoundsForElement), so XamlRoot.Changed - which covers the
				// size changes those bounds are derived from - is the nearest equivalent.
				var weakThis = new WeakReference<Page>(this);

				void OnXamlRootChanged(XamlRoot sender, XamlRootChangedEventArgs args)
				{
					if (weakThis.TryGetTarget(out var page) && page.IsInLiveTree)
					{
						page.InvalidateMeasure();
						page.InvalidateArrange();
					}
				}

				xamlRoot.Changed += OnXamlRootChanged;
				m_layoutBoundsChangedRegistration = Disposable.Create(() => xamlRoot.Changed -= OnXamlRootChanged);
			}
		}
		else if (m_layoutBoundsChangedRegistration is not null)
		{
			m_layoutBoundsChangedRegistration.Dispose();
			m_layoutBoundsChangedRegistration = null;
		}
	}

	// Returns True when either:
	// - the current window's ShouldShrinkApplicationViewVisibleBounds() returns True for testing purposes
	// - the feature RuntimeEnabledFeature::ShrinkApplicationViewVisibleBounds is set for testing purposes
	// MUX Reference Page_Partial.cpp, tag winui3/release/1.7.1
	private bool IsLaidOutToWindowBounds()
	{
		// TODO Uno: Uno has neither the IXamlTestHooks ShrinkApplicationViewVisibleBounds hook nor the
		// ShrinkApplicationViewVisibleBounds runtime-enabled feature, so this stays at WinUI's default.
		return false;
	}
}
