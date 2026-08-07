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
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Controls;

public partial class Page
{
	private const float PageApplyingLayoutBoundsTolerance = 0.1f;

	private string m_descriptor;
	private bool m_shouldRegisterNewAppbars;

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

	// MUX Reference Page_Partial.cpp, tag winui3/release/1.7.1
	// Calculates how much size needs to be subtracted from arrange bounds to account for
	// appbar occlusion. topHeight is the offset for the top bar, totalHeight is the total height consumed.
#pragma warning disable IDE0051 // Remove unused private members - prepared for future layout bounds integration
	private (double topHeight, double totalHeight) CalculateAppBarOcclusionDimensions()
#pragma warning restore IDE0051
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
}
