// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\Frame_Partial.cpp, tag winui3/release/1.5.5, commit fd8e26f1d

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Loader;
using DirectUI;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml.Controls;

partial class Frame
{
	private const int InitialTransientCacheSize = 10;

	private void CtorWinUI()
	{
		DefaultStyleKey = typeof(Frame);
		Initialize();
	}

	// TODO:MZ: Avoid destructor
	~Frame()
	{
		m_tpNavigationHistory = null;
	}

	private void Initialize()
	{
		// base.Initialize();

		var spNavigationHistory = NavigationHistory.Create(this);
		m_tpNavigationHistory = spNavigationHistory;
		BackStack = GetBackStack();
		ForwardStack = GetForwardStack();

		var pNavigationCache = NavigationCache.Create(this, InitialTransientCacheSize);
		m_upNavigationCache = pNavigationCache;
	}

	protected override void OnApplyTemplate()
	{
		//// CheckThread();

		base.OnApplyTemplate();

#if HAS_UNO && (__ANDROID__ || __IOS__)
		// It is not possible to use the WinUI behavior with a NativeFramePresenter.
		// We have two such presenters - on internal in Uno, another in Uno.Toolkit.
		if (_useWinUIBehavior && this.TemplatedRoot?.GetType().Name?.Contains("NativeFramePresenter", StringComparison.Ordinal) == true)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().LogError("WinUI Frame behavior is not compatible with NativeFramePresenter. Set the Frame.Style to '{StaticResource XamlDefaultFrame}' instead.");
			}
		}
#endif

		if (m_tpNext is not null)
		{
			m_nextClick.Disposable = null;
		}

		if (m_tpPrevious is not null)
		{
			m_previousClick.Disposable = null;
		}

		m_tpNext = null;
		m_tpPrevious = null;

		var spNextIDependencyObject = GetTemplateChild("ForwardButton");
		var spPreviousIDependencyObject = GetTemplateChild("BackButton");

		m_tpNext = spNextIDependencyObject as ButtonBase;
		m_tpPrevious = spPreviousIDependencyObject as ButtonBase;

		if (m_tpNext is not null || m_tpPrevious is not null)
		{
			if (m_tpNext is not null)
			{
				m_tpNext.Click += ClickHandler;
				m_nextClick.Disposable = Disposable.Create(() => m_tpNext.Click -= ClickHandler);
			}

			if (m_tpPrevious is not null)
			{
				m_tpPrevious.Click += ClickHandler;
				m_previousClick.Disposable = Disposable.Create(() => m_tpPrevious.Click -= ClickHandler);
			}
		}
	}

	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		base.OnPropertyChanged2(args);

#if HAS_UNO // Make sure we don't overrule legacy behavior if required
		if (!_useWinUIBehavior)
		{
			return;
		}
#endif

		if (args.Property == SourcePageTypeProperty)
		{
			if (m_isNavigationFromMethod)
			{
				m_isNavigationFromMethod = false;
			}
			else
			{
				var sourcePageTypeName = SourcePageType;
				Navigate(sourcePageTypeName, null);
			}
		}
		else if (args.Property == CacheSizeProperty)
		{
			var transientCacheSize = (int)args.NewValue;
			m_upNavigationCache.ChangeTransientCacheSize(transientCacheSize);
		}
	}

	internal void GetNavigationTransitionInfoOverride(
		out NavigationTransitionInfo definitionOverride,
		out bool isBackNavigation,
		out bool isInitialPage)
	{
		definitionOverride = m_tpNavigationTransitionInfo;
		isBackNavigation = m_isLastNavigationBack;
		isInitialPage = true;

		if (m_tpNavigationHistory is not null)
		{
			var backStack = m_tpNavigationHistory.GetBackStack();
			if (backStack is not null)
			{
				var count = backStack.Count;
				isInitialPage = count == 0;
			}
		}
	}

	internal void SetNavigationTransitionInfoOverride(NavigationTransitionInfo definitionOverride)
	{
		var pageStackEntry = m_tpNavigationHistory.GetCurrentPageStackEntry();
		if (pageStackEntry is null)
		{
			throw new InvalidOperationException("Current page stack entry is null");
		}

		pageStackEntry.NavigationTransitionInfo = definitionOverride;
	}

	private IList<PageStackEntry> GetBackStack() => m_tpNavigationHistory.GetBackStack();

	private IList<PageStackEntry> GetForwardStack() => m_tpNavigationHistory.GetForwardStack();

	private void GoBackImpl() => GoBack(null);

	private void GoBackWithTransitionInfoImpl(NavigationTransitionInfo transitionDefinition)
	{
		bool reentrancyDetected = false;
		try
		{

			// CheckThread();

			// Prevent reentrancy caused by app navigating while being
			// notified of a previous navigate
			if (m_isInNavigate)
			{
				reentrancyDetected = true;
				goto Cleanup;
			}
			m_isInNavigate = true;

			if (m_tpNavigationHistory is null)
			{
				throw new InvalidOperationException("Navigation history is null");
			}

			m_isNavigationFromMethod = true;

			// Update the NavigationTransitionInfo
			if (transitionDefinition is not null)
			{
				m_tpNavigationTransitionInfo = null;
				m_tpNavigationTransitionInfo = transitionDefinition;
				SetNavigationTransitionInfoOverride(transitionDefinition);
			}

			m_tpNavigationHistory.NavigatePrevious();

			StartNavigation();

			ElementSoundPlayerService.RequestInteractionSoundForElementStatic(ElementSoundKind.GoBack, this);
		Cleanup:
			;
		}
		finally
		{
			if (!reentrancyDetected)
			{
				m_isNavigationFromMethod = false;
				m_isInNavigate = false;
			}
		}
	}

	private void GoForwardImpl()
	{
		bool reentrancyDetected = false;
		try
		{
			// CheckThread();

			// Prevent reentrancy caused by app navigating while being
			// notified of a previous navigate
			if (m_isInNavigate)
			{
				reentrancyDetected = true;
				goto Cleanup;
			}
			m_isInNavigate = true;

			if (m_tpNavigationHistory is null)
			{
				throw new InvalidOperationException("Navigation history is null");
			}

			m_isNavigationFromMethod = true;

			m_tpNavigationHistory.NavigateNext();

			StartNavigation();

		Cleanup:
			;
		}
		finally
		{
			if (!reentrancyDetected)
			{
				m_isNavigationFromMethod = false;
				m_isInNavigate = false;
			}
		}
	}

	private bool NavigateImpl(Type sourcePageType) => NavigateImpl(sourcePageType, null);

	private bool NavigateImpl(Type sourcePageType, object parameter) => NavigateWithTransitionInfoImpl(sourcePageType, parameter, null);

	private bool NavigateWithTransitionInfoImpl(Type sourcePageType, object parameter, NavigationTransitionInfo navigationTransitionInfo)
	{
		bool reentrancyDetected = false;
		var pCanNavigate = false;

		try
		{
			// CheckThread();


			// Prevent reentrancy caused by app navigating while being
			// notified of a previous navigate
			if (m_isInNavigate)
			{
				reentrancyDetected = true;
				goto Cleanup;
			}
			m_isInNavigate = true;

			if (sourcePageType is null)
			{
				throw new ArgumentNullException(nameof(sourcePageType));
			}

			if (m_tpNavigationHistory is null)
			{
				throw new InvalidOperationException("Navigation history is null");
			}

			m_isNavigationFromMethod = true;

			var strDescriptor = Navigation.PageStackEntry.BuildDescriptor(sourcePageType);

			m_tpNavigationTransitionInfo = navigationTransitionInfo;
			m_tpNavigationHistory.NavigateNew(strDescriptor, parameter, navigationTransitionInfo);
			try
			{
				StartNavigation();
				pCanNavigate = true;
			}
			catch
			{
				pCanNavigate = false;
				throw;
			}
		Cleanup:
			;
		}
		finally
		{
			if (!reentrancyDetected)
			{
				m_isNavigationFromMethod = false;
				m_isInNavigate = false;
			}
		}

		return pCanNavigate;
	}

	private bool NavigateToTypeImpl(Type sourcePageType, object parameter, FrameNavigationOptions frameNavigationOptions)
	{
		using var cleanup = Disposable.Create(() => m_isNavigationStackEnabledForPage = true); // reseting it for the next navigation because the default is true if the user used the unspecified implementation.

		NavigationTransitionInfo transitionInfoOverride = null;

		if (frameNavigationOptions is not null)
		{
			m_isNavigationStackEnabledForPage = frameNavigationOptions.IsNavigationStackEnabled;
			transitionInfoOverride = frameNavigationOptions.TransitionInfoOverride;
		}

		return NavigateWithTransitionInfoImpl(sourcePageType, parameter, transitionInfoOverride);
	}

	private void StartNavigation()
	{
		NotifyNavigation();

		if (!m_isCanceled)
		{
			PerformNavigation();
		}
	}

	private void NotifyNavigation()
	{
		if (m_tpNavigationHistory is null)
		{
			throw new InvalidOperationException("Navigation history is null");
		}

		var navigationMode = m_tpNavigationHistory.GetPendingNavigationMode();
		var pPageStackEntry = m_tpNavigationHistory.GetPendingPageStackEntry();
		if (pPageStackEntry is null)
		{
			throw new InvalidOperationException("Pending page stack entry is null");
		}

		var strDescriptor = pPageStackEntry.GetDescriptor();
		if (strDescriptor is null)
		{
			throw new InvalidOperationException("Descriptor is null");
		}

		var spParameterobject = pPageStackEntry.Parameter;
		var spNavigationTransitionInfo = pPageStackEntry.NavigationTransitionInfo;

		RaiseNavigating(spParameterobject, spNavigationTransitionInfo, strDescriptor, navigationMode, out m_isCanceled);

		if (m_isCanceled)
		{
			RaiseNavigationStopped(null, spParameterobject, spNavigationTransitionInfo, strDescriptor, navigationMode);
			return;
		}

		var spPageobject = Content;

		if (spPageobject is Page spIPage)
		{
			spIPage.InvokeOnNavigatingFrom(spParameterobject, spNavigationTransitionInfo, strDescriptor, navigationMode, out m_isCanceled);

			if (m_isCanceled)
			{
				RaiseNavigationStopped(null, spParameterobject, spNavigationTransitionInfo, strDescriptor, navigationMode);
			}
		}
	}

	private void PerformNavigation()
	{
		try
		{
			if (m_isCanceled)
			{
				goto Cleanup;
			}

			if (m_tpNavigationHistory is null)
			{
				throw new InvalidOperationException("Navigation history is null");
			}

			var pPageStackEntry = m_tpNavigationHistory.GetPendingPageStackEntry();
			if (pPageStackEntry is null)
			{
				throw new InvalidOperationException("Pending page stack entry is null");
			}

			var spOldobject = Content;
			var spNewobject = m_upNavigationCache.GetContent(pPageStackEntry);
			var spParameterobject = pPageStackEntry.Parameter;
			var spNavigationTransitionInfo = pPageStackEntry.NavigationTransitionInfo;

			ChangeContent(spOldobject, spNewobject, spParameterobject, spNavigationTransitionInfo);

		Cleanup:
			;
		}
		finally
		{
			m_isNavigationFromMethod = false;
		}
	}

	internal void RemovePageFromCache(string descriptor)
	{
#if HAS_UNO // Do not use this method when legacy behavior is preferred
		if (!_useWinUIBehavior)
		{
			return;
		}
#endif

		m_upNavigationCache.UncachePageContent(descriptor);
	}

	//------------------------------------------------------------------------
	//
	//  Method: NotifyGetOrSetNavigationState
	//
	//  Synopsis:
	//     Notify current page of Get/SetNavigationState.
	//  Frame.GetNavigationState is typically called when the App is suspended
	//  Frame.SetNavigationState is typically called when the App is resumed
	//
	//  This notification is to give the current page the opportunity to:
	//  1. Serialize page content in Page.OnNavigatedFrom.
	//  2. Deserialize page content and get navigation param in
	//     Page.OnNavigatedTo.
	//
	//  This allows the current page to serialize/de-serialize like any other
	//  page in the navstack. If this notification is not done, the current page
	//  would not get Page.OnNavigatedFrom when the App is suspended or
	//  Page.OnNavigatedTo when the App is resumed. Other pages in the navstack
	//  get Page.OnNavigatedFrom when the page is navigated away from, and
	//  Page.OnNavigatedTo when the page is navigated to.
	//
	//------------------------------------------------------------------------

	private void NotifyGetOrSetNavigationState(NavigationStateOperation navigationStateOperation)
	{
		// Get current navigation entry
		if (m_tpNavigationHistory is null)
		{
			return;
		}
		var pPageStackEntry = m_tpNavigationHistory.GetCurrentPageStackEntry();
		if (pPageStackEntry is null)
		{
			return;
		}

		// Get current page, param and page type
		var spContentobject = Content;
		var spParameterobject = pPageStackEntry.Parameter;
		var spNavigationTransitionInfo = pPageStackEntry.NavigationTransitionInfo;
		var strDescriptor = pPageStackEntry.GetDescriptor();

		var spIPage = spContentobject as Page;
		if (spIPage is null)
		{
			throw new InvalidOperationException("Content is not a Page");
		}

		if (navigationStateOperation == NavigationStateOperation.Get)
		{
			// Call Page.OnNavigatedFrom. Use Forward as navigation mode, which
			// is the best fit among available modes -- we are going forward from
			// the page to App's suspend mode.
			spIPage.InvokeOnNavigatedFrom(spIPage, spParameterobject, spNavigationTransitionInfo, strDescriptor, NavigationMode.Forward);
		}
		else
		{
			// Call Page.OnNavigatedTo. Use Back as navigation mode,which
			// is the best fit among available modes -- we are going back to the
			// page on App Resume.
			spIPage.InvokeOnNavigatedTo(spIPage, spParameterobject, spNavigationTransitionInfo, strDescriptor, NavigationMode.Back);
		}
	}

	private void ChangeContent(object oldObject, object newObject, object parameter, NavigationTransitionInfo transitionInfo)
	{
		bool wasContentChanged = false;
		string strDescriptor = null;
		try
		{
			bool isNavigationStackEnabled = IsNavigationStackEnabled;

			if (newObject is null)
			{
				throw new InvalidOperationException("New content is null");
			}

			if (m_tpNavigationHistory is null)
			{
				throw new InvalidOperationException("Navigation history is null");
			}

			var navigationMode = m_tpNavigationHistory.GetPendingNavigationMode();
			var pPageStackEntry = m_tpNavigationHistory.GetPendingPageStackEntry();
			if (pPageStackEntry is null)
			{
				throw new InvalidOperationException("Pending page stack entry is null");
			}
			strDescriptor = pPageStackEntry.GetDescriptor();
			if (strDescriptor is null)
			{
				throw new InvalidOperationException("Descriptor is null");
			}

			// If this is a back navigation, cache the navigation mode
			// and override the NavigationTransitionInfo with the
			// transition that took place upon navigation from that page.
			m_isLastNavigationBack = false;
			if (navigationMode == NavigationMode.Back)
			{
				PageStackEntry pCurrentPageStackEntry = null;
				NavigationTransitionInfo spNavigationTransitionInfo;

				pCurrentPageStackEntry = m_tpNavigationHistory.GetCurrentPageStackEntry();

				if (pCurrentPageStackEntry is not null)
				{
					spNavigationTransitionInfo = pCurrentPageStackEntry.NavigationTransitionInfo;

					m_isLastNavigationBack = true;
					m_tpNavigationTransitionInfo = spNavigationTransitionInfo;
				}
			}

			var spOldIPage = oldObject as Page;
			var spNewIPage = newObject as Page;
			if (spNewIPage is null)
			{
				throw new InvalidOperationException("New content is not a Page");
			}

			SetContent(newObject);
			wasContentChanged = true;

			if (isNavigationStackEnabled && m_isNavigationStackEnabledForPage)
			{
				m_tpNavigationHistory.CommitNavigation();
			}

			RaiseNavigated(spNewIPage, parameter, transitionInfo, strDescriptor, navigationMode);

			if (spOldIPage is not null)
			{
				spOldIPage.InvokeOnNavigatedFrom(spNewIPage, parameter, transitionInfo, strDescriptor, navigationMode);
			}

			spNewIPage.InvokeOnNavigatedTo(spNewIPage, parameter, transitionInfo, strDescriptor, navigationMode);
		}
		catch (Exception ex)
		{
			RaiseNavigationFailed(strDescriptor, ex, out var isHandled);

			if (!isHandled)
			{
				RaiseUnhandledException(ex);
			}

			if (wasContentChanged)
			{
				Content = oldObject;
			}

			throw;
		}
	}

	private void RaiseUnhandledException(Exception ex)
	{
		//xstring_ptr strMessage;
		//HRESULT hrToReport;
		//bool fIsHandled = false;

		//ErrorHelper.MapHresult(errorCode, &hrToReport);
		//ErrorHelper.GetNonLocalizedErrorString(resourceStringID, &strMessage);

		//ErrorHelper.RaiseUnhandledExceptionEvent(hrToReport, strMessage, &fIsHandled);

		Application.Current.RaiseRecoverableUnhandledException(ex);
	}

	private string GetNavigationStateImpl()
	{
		NotifyGetOrSetNavigationState(NavigationStateOperation.Get);

		if (m_tpNavigationHistory is null)
		{
			throw new InvalidOperationException("Navigation history is null");
		}

		return m_tpNavigationHistory.GetNavigationState();
	}

	private void SetNavigationStateImpl(string navigationState) => SetNavigationStateWithNavigationControlImpl(navigationState, false);

	private void SetNavigationStateWithNavigationControlImpl(string navigationState, bool suppressNavigate)
	{
		m_tpNavigationHistory.SetNavigationState(navigationState, suppressNavigate);

		if (suppressNavigate)
		{
			SetContent(null);
		}
		else
		{
			// Create or get page corresponding to current navigation entry and set it as
			// frame's content. NavigationCache.GetContent will create the current page.
			var pPageStackEntry = m_tpNavigationHistory.GetCurrentPageStackEntry();
			if (pPageStackEntry is not null)
			{
				m_isNavigationFromMethod = true;

				var spContentobject = m_upNavigationCache.GetContent(pPageStackEntry);
				SetContent(spContentobject);
			}

			// Commit SetNavigationState of navigation history
			m_tpNavigationHistory.CommitSetNavigationState(m_upNavigationCache);
		}

		NotifyGetOrSetNavigationState(NavigationStateOperation.Set);
	}

	private void ClickHandler(object sender, RoutedEventArgs e)
	{
		var spSenderIButtonBase = sender as ButtonBase;

		if (m_tpNext is not null && m_tpNext == spSenderIButtonBase)
		{
			GoForward();
		}
		else if (m_tpPrevious is not null && m_tpPrevious == spSenderIButtonBase)
		{
			GoBack();
		}
	}

	private void RaiseNavigated(
		object pContentobject,
		object pParameterobject,
		NavigationTransitionInfo pTransitionInfo,
		string descriptor,
		NavigationMode navigationMode)
	{
		if (descriptor is null)
		{
			throw new ArgumentNullException(nameof(descriptor));
		}

		if (pContentobject is null)
		{
			throw new ArgumentNullException(nameof(pContentobject));
		}

		var spINavigationEventArgs = NavigationHelpers.CreateINavigationEventArgs(pContentobject, pParameterobject, pTransitionInfo, descriptor, navigationMode);

		Navigated?.Invoke(this, spINavigationEventArgs);

		//TraceFrameNavigatedInfo(WindowsGetStringRawBuffer(descriptor, null), (unsigned char)(navigationMode));
	}

	private void RaiseNavigating(
		object pParameterobject,
		NavigationTransitionInfo pTransitionInfo,
		string descriptor,
		NavigationMode navigationMode,
		out bool isCanceled)
	{
		if (descriptor is null)
		{
			throw new ArgumentNullException(nameof(descriptor));
		}

		var spINavigatingCancelEventArgs = NavigationHelpers.CreateINavigatingCancelEventArgs(pParameterobject, pTransitionInfo, descriptor, navigationMode);

		Navigating?.Invoke(this, spINavigatingCancelEventArgs);

		isCanceled = spINavigatingCancelEventArgs.Cancel;

		//TraceFrameNavigatingInfo(WindowsGetStringRawBuffer(descriptor, null), (unsigned char)(navigationMode));
	}

	[UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "normal flow of operations")]
	private void RaiseNavigationFailed(string descriptor, Exception errorResult, out bool isCanceled)
	{
		if (descriptor is null)
		{
			throw new ArgumentNullException(nameof(descriptor));
		}

		var sourcePageType = PageStackEntry.ResolveDescriptor(descriptor);
		var spNavigationFailedEventArgs = new NavigationFailedEventArgs(sourcePageType, errorResult);

		NavigationFailed?.Invoke(this, spNavigationFailedEventArgs);

		isCanceled = spNavigationFailedEventArgs.Handled;
	}

	private void RaiseNavigationStopped(
		object pContentobject,
		object pParameterobject,
		NavigationTransitionInfo pTransitionInfo,
		string descriptor,
		NavigationMode navigationMode)
	{
		if (descriptor is null)
		{
			throw new ArgumentNullException(nameof(descriptor));
		}

		var spINavigationEventArgs = NavigationHelpers.CreateINavigationEventArgs(pContentobject, pParameterobject, pTransitionInfo, descriptor, navigationMode);

		NavigationStopped?.Invoke(this, spINavigationEventArgs);
	}

	//private void OnReferenceTrackerWalk(INT walkType) // override
	//{
	//	// Walk field references

	//	// TODO: Change this into a TrackerPtr
	//	if (m_upNavigationCache)
	//	{
	//		m_upNavigationCache.ReferenceTrackerWalk((EReferenceTrackerWalkType)(walkType));
	//	}

	//	// Walk remaining references

	//	FrameGenerated.OnReferenceTrackerWalk(walkType);
	//}

	// private NavigationMode GetCurrentNavigationMode() => m_tpNavigationHistory.GetCurrentNavigationMode();
}
