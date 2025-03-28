// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Page_Partial.cpp

#nullable enable

using System;
using System.Runtime.CompilerServices;
using DirectUI;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;

namespace Windows.UI.Xaml.Controls;

public partial class Page
{
	private string m_descriptor;

	private protected override void OnLoaded()
	{
		base.OnLoaded();

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

		if (args.Property == NavigationCacheModeProperty)
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
}
