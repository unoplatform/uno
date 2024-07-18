using System;
using System.Collections.Generic;
using DirectUI;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Uno.Disposables;

namespace Microsoft.UI.Xaml.Controls;

partial class Frame
{
	private const int InitialTransientCacheSize = 10;

	/// <summary>
	/// Initializes a new instance of the Frame class.
	/// </summary>
	public Frame()
	{
	}

	// TODO:MZ: Avoid destructor
	~Frame()
	{
		m_tpNavigationHistory.Clear();
	}

	private void Initialize()
	{
		// base.Initialize();

		var spNavigationHistory = NavigationHistory.Create(this);
		m_tpNavigationHistory = spNavigationHistory;

		var pNavigationCache = NavigationCache.Create(this, InitialTransientCacheSize);
		m_upNavigationCache = pNavigationCache;
	}

	protected override void OnApplyTemplate()
	{
		//CheckThread();

		base.OnApplyTemplate();

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
		uint transientCacheSize = 0;
		bool canNavigate = false;

		base.OnPropertyChanged2(args);

		if (args.Property == Frame.SourcePageTypeProperty)
		{
			if (m_isNavigationFromMethod)
			{
				m_isNavigationFromMethod = false;
			}
			else
			{
				var sourcePageTypeName = SourcePageType;
				Navigate(sourcePageTypeName, null, canNavigate);
			}
		}
		else if (args.Property == Frame.CacheSizeProperty)
		{
			transientCacheSize = (uint)(args.NewValue);    // We use AsEnum for all uint egers
			m_upNavigationCache.ChangeTransientCacheSize(transientCacheSize);
		}
	}

	private void GetNavigationTransitionInfoOverride(
		out NavigationTransitionInfo definitionOverride,
		out bool isBackNavigation,
		out bool isInitialPage)
	{
		definitionOverride = m_tpNavigationTransitionInfo;
		isBackNavigation = m_isLastNavigationBack;
		isInitialPage = true;

		if (m_tpNavigationHistory is not null)
		{
			wrl.ComPtr<IVector<Navigation.PageStackEntry*>> backstack;
			m_tpNavigationHistory.Cast<NavigationHistory>().GetBackStack(&backstack);
			uint count = 0;
			if (backstack)
			{
				count = backstack.Size;
				*isInitialPage = count == 0;
			}
		}
	}

	private void SetNavigationTransitionInfoOverride(NavigationTransitionInfo definitionOverride)
	{
		PageStackEntry* pPageStackEntry = null;

		m_tpNavigationHistory.Cast<NavigationHistory>().GetCurrentPageStackEntry(&pPageStackEntry);
		IFCPTR(pPageStackEntry);
		pPageStackEntry.NavigationTransitionInfo = definitionOverride;

	Cleanup:
		pPageStackEntry = null;
		RRETURN(hr);
	}

	private IList<PageStackEntry> GetBackStack() => m_tpNavigationHistory.GetBackStack();

	private IList<PageStackEntry> GetForwardStack() => m_tpNavigationHistory.GetForwardStack();

	private void GoBackImpl() => GoBack(null);

	private void GoBackWithTransitionInfoImpl(NavigationThemeTransition transitionDefinition)
	{
		bool reentrancyDetected = false;

		CheckThread();

		// Prevent reentrancy caused by app navigating while being
		// notified of a previous navigate
		if (m_isInNavigate)
		{
			reentrancyDetected = true;
			goto Cleanup;
		}
		m_isInNavigate = true;

		IFCPTR(m_tpNavigationHistory);

		m_isNavigationFromMethod = true;

		// Update the NavigationTransitionInfo
		if (transitionDefinition)
		{
			m_tpNavigationTransitionInfo.Clear();
			SetPtrValueWithQI(m_tpNavigationTransitionInfo, transitionDefinition);
			SetNavigationTransitionInfoOverrideImpl(transitionDefinition);
		}

		m_tpNavigationHistory.Cast<NavigationHistory>().NavigatePrevious();

		StartNavigation();

		DirectUI.ElementSoundPlayerService.RequestInteractionSoundForElementStatic(ElementSoundKind_GoBack, this);

	Cleanup:
		if (!reentrancyDetected)
		{
			m_isNavigationFromMethod = false;
			m_isInNavigate = false;
		}
	}

	private void GoForwardImpl()
	{
		bool reentrancyDetected = false;

		CheckThread();

		// Prevent reentrancy caused by app navigating while being
		// notified of a previous navigate
		if (m_isInNavigate)
		{
			reentrancyDetected = true;
			goto Cleanup;
		}
		m_isInNavigate = true;

		IFCPTR(m_tpNavigationHistory);

		m_isNavigationFromMethod = true;

		m_tpNavigationHistory.Cast<NavigationHistory>().NavigateNext();

		StartNavigation();

	Cleanup:
		if (!reentrancyDetected)
		{
			m_isNavigationFromMethod = false;
			m_isInNavigate = false;
		}
	}

	private bool NavigateImpl(Type sourcePageType) => NavigateImpl(sourcePageType, null);

	private bool NavigateImpl(Type sourcePageType, object parameter) => NavigateWithTransitionInfoImpl(sourcePageType, parameter, null);

	private bool NavigateWithTransitionInfoImpl(Type sourcePageType, object parameter, NavigationTransitionInfo infoOverride)
	{
		xruntime_string_ptr strDescriptor;
		bool reentrancyDetected = false;
		CClassInfo* pType = null;

		CheckThread();

		IFCPTR(pCanNavigate);
		*pCanNavigate = false;

		// Prevent reentrancy caused by app navigating while being
		// notified of a previous navigate
		if (m_isInNavigate)
		{
			reentrancyDetected = true;
			goto Cleanup;
		}
		m_isInNavigate = true;

		IFCPTR(sourcePageType.Name);
		IFCPTR(m_tpNavigationHistory);

		m_isNavigationFromMethod = true;

		MetadataAPI.GetClassInfoByTypeName(sourcePageType, &pType);
		pType.GetFullName().Promote(&strDescriptor);

		SetPtrValueWithQI(m_tpNavigationTransitionInfo, navigationTransitionInfo);
		m_tpNavigationHistory.Cast<NavigationHistory>().NavigateNew(strDescriptor.Getstring(), pobject, navigationTransitionInfo);
		hr = StartNavigation();
		*pCanNavigate = ((SUCCEEDED(hr)) ? true : false);

	Cleanup:
		if (!reentrancyDetected)
		{
			m_isNavigationFromMethod = false;
			m_isInNavigate = false;
		}
		RRETURN(hr);
	}

	private bool NavigateToTypeImpl(Type sourcePageType, object parameter, FrameNavigationOptions frameNavigationOptions)
	{
		using var cleanup = Disposable.Create(() => m_isNavigationStackEnabledForPage = true); // reseting it for the next navigation because the default is true if the user used the unspecified implementation.

		NavigationTransitionInfo transitionInfoOverride;

		if (frameNavigationOptions is not null)
		{
			m_isNavigationStackEnabledForPage = frameNavigationOptions.IsNavigationStackEnabled;
			transitionInfoOverride = frameNavigationOptions.TransitionInfoOverride;
		}

		NavigateWithTransitionInfoImpl(sourcePageType, pobject, transitionInfoOverride, pCanNavigate);

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
		Page spIPage;
		object spPageobject;
		object spParameterobject;
		INavigationTransitionInfo spNavigationTransitionInfo;
		string strDescriptor;
		PageStackEntry* pPageStackEntry = null;
		Navigation.NavigationMode navigationMode = Navigation.NavigationMode_New;

		IFCPTR(m_tpNavigationHistory);

		m_tpNavigationHistory.Cast<NavigationHistory>().GetPendingNavigationMode(&navigationMode);
		m_tpNavigationHistory.Cast<NavigationHistory>().GetPendingPageStackEntry(&pPageStackEntry);
		IFCPTR(pPageStackEntry);
		pPageStackEntry.GetDescriptor(strDescriptor.GetAddressOf());
		IFCPTR(strDescriptor);
		spParameterobject = pPageStackEntry.Parameter;
		spNavigationTransitionInfo = pPageStackEntry.NavigationTransitionInfo;

		RaiseNavigating(spParameterobject, spNavigationTransitionInfo, strDescriptor, navigationMode, &m_isCanceled);

		if (m_isCanceled)
		{
			RaiseNavigationStopped(null, spParameterobject, spNavigationTransitionInfo, strDescriptor, navigationMode);
			goto Cleanup;
		}

		get_Content(&spPageobject);

		spIPage = spPageobject.AsOrNull<IPage>();

		if (spIPage)
		{
			spIPage.Cast<Page>().InvokeOnNavigatingFrom(spParameterobject, spNavigationTransitionInfo, strDescriptor, navigationMode, &m_isCanceled);

			if (m_isCanceled)
			{
				RaiseNavigationStopped(null, spParameterobject, spNavigationTransitionInfo, strDescriptor, navigationMode);
			}
		}

	Cleanup:
		pPageStackEntry = null;
	}

	private void PerformNavigation()
	{
		object spOldobject;
		object spNewobject;
		object spParameterobject;
		INavigationTransitionInfo spNavigationTransitionInfo;
		PageStackEntry* pPageStackEntry = null;

		if (m_isCanceled)
		{
			goto Cleanup;
		}

		IFCPTR(m_tpNavigationHistory);

		m_tpNavigationHistory.Cast<NavigationHistory>().GetPendingPageStackEntry(&pPageStackEntry);
		IFCPTR(pPageStackEntry);

		get_Content(&spOldobject);
		m_upNavigationCache.GetContent(pPageStackEntry, &spNewobject);
		spParameterobject = pPageStackEntry.Parameter;
		spNavigationTransitionInfo = pPageStackEntry.NavigationTransitionInfo;

		ChangeContent(spOldobject, spNewobject, spParameterobject, spNavigationTransitionInfo);

	Cleanup:
		pPageStackEntry = null;
		m_isNavigationFromMethod = false;
	}

	private void RemovePageFromCache(string descriptor)
	{
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
		object spContentobject;
		IPage spIPage;
		object spParameterobject;
		xaml_animation.INavigationTransitionInfo spNavigationTransitionInfo;
		string strDescriptor;
		PageStackEntry* pPageStackEntry = null;

		// Get current navigation entry
		if (!m_tpNavigationHistory)
		{
			goto Cleanup;
		}
		m_tpNavigationHistory.Cast<NavigationHistory>().GetCurrentPageStackEntry(&pPageStackEntry);
		if (!pPageStackEntry)
		{
			goto Cleanup;
		}

		// Get current page, param and page type
		get_Content(&spContentobject);
		spParameterobject = pPageStackEntry.Parameter;
		spNavigationTransitionInfo = pPageStackEntry.NavigationTransitionInfo;
		pPageStackEntry.GetDescriptor(strDescriptor.GetAddressOf());

		spIPage = spContentobject.AsOrNull<IPage>();
		IFCPTR(spIPage);

		if (navigationStateOperation == NavigationStateOperation_Get)
		{
			// Call Page.OnNavigatedFrom. Use Forward as navigation mode, which
			// is the best fit among available modes -- we are going forward from
			// the page to App's suspend mode.
			(spIPage.Cast<Page>().InvokeOnNavigatedFrom(spIPage, spParameterobject, spNavigationTransitionInfo, strDescriptor,
							Navigation.NavigationMode_Forward));
		}
		else
		{
			// Call Page.OnNavigatedTo. Use Back as navigation mode,which
			// is the best fit among available modes -- we are going back to the
			// page on App Resume.
			(spIPage.Cast<Page>().InvokeOnNavigatedTo(spIPage, spParameterobject, spNavigationTransitionInfo, strDescriptor,
							Navigation.NavigationMode_Back));
		}
	}

	private void ChangeContent(object oldContent, object newContent, object parameter, INavigationTransitionInfo transitionInfo)
	{
		IPage spOldIPage;
		IPage spNewIPage;
		string strDescriptor;
		bool isHandled = false;
		bool wasContentChanged = false;
		PageStackEntry* pPageStackEntry = null;
		Navigation.NavigationMode navigationMode = Navigation.NavigationMode_New;

		bool isNavigationStackEnabled = false;
		get_IsNavigationStackEnabled(&isNavigationStackEnabled);

		IFCPTR(pNewobject);
		IFCPTR(m_tpNavigationHistory);

		m_tpNavigationHistory.Cast<NavigationHistory>().GetPendingNavigationMode(&navigationMode);
		m_tpNavigationHistory.Cast<NavigationHistory>().GetPendingPageStackEntry(&pPageStackEntry);
		IFCPTR(pPageStackEntry);
		pPageStackEntry.GetDescriptor(strDescriptor.GetAddressOf());
		IFCPTR(strDescriptor);

		// If this is a back navigation, cache the navigation mode
		// and override the NavigationTransitionInfo with the
		// transition that took place upon navigation from that page.
		m_isLastNavigationBack = false;
		if (navigationMode == Navigation.NavigationMode_Back)
		{
			PageStackEntry* pCurrentPageStackEntry = null;
			xaml_animation.INavigationTransitionInfo spNavigationTransitionInfo;

			m_tpNavigationHistory.Cast<NavigationHistory>().GetCurrentPageStackEntry(&pCurrentPageStackEntry);

			if (pCurrentPageStackEntry)
			{
				spNavigationTransitionInfo = pCurrentPageStackEntry.NavigationTransitionInfo;

				m_isLastNavigationBack = true;
				SetPtrValueWithQI(m_tpNavigationTransitionInfo, spNavigationTransitionInfo);
			}
		}

		spOldIPage = ctl.query_interface_cast<IPage>(pOldobject);
		spNewIPage = ctl.query_interface_cast<IPage>(pNewobject);
		IFCPTR(spNewIPage);

		put_Content(pNewobject);
		wasContentChanged = true;

		if (isNavigationStackEnabled && m_isNavigationStackEnabledForPage)
		{
			m_tpNavigationHistory.Cast<NavigationHistory>().CommitNavigation();
		}

		RaiseNavigated(spNewIPage, pParameterobject, pTransitionInfo, strDescriptor, navigationMode);

		if (spOldIPage)
		{
			spOldIPage.Cast<Page>().InvokeOnNavigatedFrom(spNewIPage, pParameterobject, pTransitionInfo, strDescriptor, navigationMode);
		}

		spNewIPage.Cast<Page>().InvokeOnNavigatedTo(spNewIPage, pParameterobject, pTransitionInfo, strDescriptor, navigationMode);

	Cleanup:
		if (/*FAILED*/(hr))
		{
			IGNOREHR(RaiseNavigationFailed(strDescriptor, hr, &isHandled));

			if (!isHandled)
			{
				IGNOREHR(RaiseUnhandledException(E_UNEXPECTED, TEXT_FRAME_NAVIGATION_FAILED_UNHANDLED));
			}

			if (wasContentChanged)
			{
				IGNOREHR(put_Content(pOldobject));
			}
		}
	}

	private void RaiseUnhandledException(int errorCode, int resourceStringId)
	{
		xstring_ptr strMessage;
		HRESULT hrToReport;
		bool fIsHandled = false;

		ErrorHelper.MapHresult(errorCode, &hrToReport);
		ErrorHelper.GetNonLocalizedErrorString(resourceStringID, &strMessage);

		ErrorHelper.RaiseUnhandledExceptionEvent(hrToReport, strMessage, &fIsHandled);
	}

	private string GetNavigationStateImpl()
	{
		NotifyGetOrSetNavigationState(NavigationStateOperation.Get);

		IFCPTR(m_tpNavigationHistory);
		m_tpNavigationHistory.Cast<NavigationHistory>().GetNavigationState(pNavigationState);
	}

	private void SetNavigationStateImpl(string navigationState) => SetNavigationStateWithNavigationControlImpl(navigationState, false);

	private void SetNavigationStateWithNavigationControlImpl(string navigationState, bool suppressNavigate)
	{
		m_tpNavigationHistory.Cast<NavigationHistory>().SetNavigationState(navigationState, suppressNavigate);

		if (suppressNavigate)
		{
			put_Content(null);
		}
		else
		{
			object spContentobject;
			PageStackEntry* pPageStackEntry = null;

			// Create or get page corresponding to current navigation entry and set it as
			// frame's content. NavigationCache.GetContent will create the current page.
			m_tpNavigationHistory.Cast<NavigationHistory>().GetCurrentPageStackEntry(&pPageStackEntry);
			if (pPageStackEntry)
			{
				m_isNavigationFromMethod = true;

				m_upNavigationCache.GetContent(pPageStackEntry, &spContentobject);
				put_Content(spContentobject);
			}

			// Commit SetNavigationState of navigation history
			m_tpNavigationHistory.Cast<NavigationHistory>().CommitSetNavigationState(m_upNavigationCache);
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
		NavigatedEventSourceType* pEventSource = null;
		INavigationEventArgs spINavigationEventArgs;

		IFCPTR(descriptor);
		IFCPTR(pContentobject);

		NavigationHelpers.CreateINavigationEventArgs(pContentobject, pParameterobject, pTransitionInfo, descriptor, navigationMode, &spINavigationEventArgs);
		IFCPTR(spINavigationEventArgs);

		GetNavigatedEventSourceNoRef(&pEventSource);
		pEventSource.Raise(ctl.as_iinspectable(this), spINavigationEventArgs);

		TraceFrameNavigatedInfo(WindowsGetStringRawBuffer(descriptor, null), (unsigned char)(navigationMode));
	}

	private void RaiseNavigating(
		object pParameterobject,
		NavigationTransitionInfo pTransitionInfo,
		string descriptor,
		NavigationMode navigationMode)
	{
		NavigatingEventSourceType* pEventSource = null;
		INavigatingCancelEventArgs spINavigatingCancelEventArgs;

		IFCPTR(pIsCanceled);
		*pIsCanceled = null;

		IFCPTR(descriptor);

		NavigationHelpers.CreateINavigatingCancelEventArgs(pParameterobject, pTransitionInfo, descriptor, navigationMode, &spINavigatingCancelEventArgs);
		IFCPTR(spINavigatingCancelEventArgs);

		GetNavigatingEventSourceNoRef(&pEventSource);
		pEventSource.Raise(ctl.as_iinspectable(this), spINavigatingCancelEventArgs);

		spINavigatingCancelEventArgs.get_Cancel(pIsCanceled);

		TraceFrameNavigatingInfo(WindowsGetStringRawBuffer(descriptor, null), (unsigned char)(navigationMode));
	}

	private void RaiseNavigationFailed(string descriptor, Exception errorResult, out bool isCanceled) 
	{
		NavigationFailedEventSourceType* pEventSource = null;
		NavigationFailedEventArgs spNavigationFailedEventArgs;
		wxaml_interop.TypeName sourcePageType = default;

		IFCPTR(pIsCanceled);
		*pIsCanceled = null;

		IFCPTR(descriptor);

		spNavigationFailedEventArgs = new NavigationFailedEventArgs();
		IFCPTR(spNavigationFailedEventArgs);

		MetadataAPI.GetTypeNameByFullName(XSTRING_PTR_EPHEMERAL_FROM_string(descriptor), &sourcePageType);
		spNavigationFailedEventArgs.SourcePageType = sourcePageType;
		spNavigationFailedEventArgs.Exception = errorResult;

		GetNavigationFailedEventSourceNoRef(&pEventSource);

		pEventSource.Raise(ctl.as_iinspectable(this), spNavigationFailedEventArgs);

		spNavigationFailedEventArgs.get_Handled(pIsCanceled);

	Cleanup:
		DELETE_STRING(sourcePageType.Name);
	}

	private void RaiseNavigationStopped(
		object pContentobject,
		object pParameterobject,
		NavigationTransitionInfo pTransitionInfo,
		string descriptor,
		NavigationMode navigationMode)
	{
		NavigationStoppedEventSourceType* pEventSource = null;
		INavigationEventArgs spINavigationEventArgs;

		IFCPTR(descriptor);

		NavigationHelpers.CreateINavigationEventArgs(pContentobject, pParameterobject, pTransitionInfo, descriptor, navigationMode, &spINavigationEventArgs);
		IFCPTR(spINavigationEventArgs);

		GetNavigationStoppedEventSourceNoRef(&pEventSource);
		pEventSource.Raise(ctl.as_iinspectable(this), spINavigationEventArgs);
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

	private NavigationMode GetCurrentNavigationMode() => m_tpNavigationHistory.GetCurrentNavigationMode(pNavigationMode);
}
