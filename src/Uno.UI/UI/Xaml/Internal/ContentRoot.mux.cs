// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ContentRoot.h, ContentRoot.cpp

#nullable enable

using System;
using Uno.UI.Xaml.Input;
using Uno.UI.Xaml.Islands;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Uno.UI.Xaml.Core;

/// <summary>
/// Represents the content root of an application window.
/// </summary>
partial class ContentRoot
{
	/// <summary>
	/// Initializes a content root.
	/// </summary>
	/// <param name="rootElement">Root element.</param>
	public ContentRoot(ContentRootType type, Color backgroundColor, UIElement? rootElement, CoreServices coreServices)
	{
		_coreServices = coreServices ?? throw new ArgumentNullException(nameof(coreServices));
		_type = type;
		_akExport = new AccessKeyExport();
		_visualTree = new VisualTree(coreServices, backgroundColor, rootElement, this);
		_inputManager = new InputManager(this);
		_contentRootEventListener = new ContentRootEventListener(this);
		_focusManager = new(this);

		//_akExport.SetVisualTree(&_visualTree);
		//_akExport.SetFocusManager(&_focusManager);

		//TODO Uno: We may want to create a custom version of adapter and observer for Island vs CoreWindow.
		_focusAdapter = new FocusAdapter(this);
		FocusManager.SetFocusObserver(new FocusObserver(this));

		switch (type)
		{
			case ContentRootType.CoreWindow:
				MUX_ASSERT(coreServices.ContentRootCoordinator._unsafe_XamlIslandsIncompatible_CoreWindowContentRoot is null); // There should only be one of this per thread
				coreServices.ContentRootCoordinator._unsafe_XamlIslandsIncompatible_CoreWindowContentRoot = this;
				//_focusAdapter = std.make_unique<ContentRootAdapters.FocusManagerCoreWindowAdapter>(*this);
				//_focusManager.SetFocusObserver(std.make_unique<CoreWindowFocusObserver>(&coreServices, this));
				break;
			case ContentRootType.XamlIsland:
				//_focusAdapter = std.make_unique<ContentRootAdapters.FocusManagerXamlIslandAdapter>(*this);
				//_focusManager.SetFocusObserver(std.make_unique<FocusObserver>(&coreServices, this));
				break;
		}
	}

	//~ContentRoot()
	//{
	//	Close();

	//	if (Type == ContentRootType.CoreWindow)
	//	{
	//		_coreServices.ContentRootCoordinator._unsafe_XamlIslandsIncompatible_CoreWindowContentRoot = null;
	//	}

	//	_ref_count.end_destroying();
	//}

	internal XamlIsland? XamlIslandRoot
	{
		get => _xamlIslandRoot;
		set => _xamlIslandRoot = value;
	}

	internal void SetXamlIslandType(IslandType islandType)
	{
		if (_type != ContentRootType.XamlIsland)
		{
			throw new InvalidOperationException("XamlIslandType can only be set for XamlIsland content roots");
		}

		_islandType = islandType;
	}

	//private void OnStateChanged()
	//{
	//	switch (_type)
	//	{
	//		case ContentRootType.XamlIsland:
	//			_xamlIslandRoot.OnStateChanged();
	//			break;
	//		case ContentRootType.CoreWindow:
	//			// FxCallbacks.DxamlCore_OnCompositionContentStateChangedForUWP();
	//			break;
	//	}
	//}

	//_Check_return_ HRESULT ContentRoot.OnAutomationProviderRequested(
	//ixp.IContentIsland* content,
	//ixp.IContentIslandAutomationProviderRequestedEventArgs* args)
	//{
	//	if (_type == Type.XamlIsland)
	//	{
	//		IFC_RETURN(_xamlIslandRoot->OnContentAutomationProviderRequested(content, args));
	//	}

	//	return S_OK;
	//}

	//	_Check_return_ HRESULT ContentRoot.RegisterCompositionContent(_In_ ixp.IContentIsland* compositionContent)
	//{

	//	_compositionContent = compositionContent;

	//    IFC_RETURN(_compositionContent->add_StateChanged(WRLHelper.MakeAgileCallback<ITypedEventHandler<
	//		ixp.ContentIsland*,
	//		ixp.ContentIslandStateChangedEventArgs*>>([&](
	//			ixp.IContentIsland* content,
	//			ixp.IContentIslandStateChangedEventArgs* args) -> HRESULT

	//			{
	//		IFC_RETURN(OnStateChanged());

	//		return S_OK;
	//	}).Get(),
	//            &_compositionContentStateChangedToken));

	//    // Accessibility
	//    IFC_RETURN(_compositionContent->add_AutomationProviderRequested(WRLHelper.MakeAgileCallback<ITypedEventHandler<
	//		ixp.ContentIsland*,
	//		ixp.ContentIslandAutomationProviderRequestedEventArgs*>>([&](
	//			ixp.IContentIsland* content,
	//			ixp.IContentIslandAutomationProviderRequestedEventArgs* args) -> HRESULT

	//			{
	//		IFC_RETURN(OnAutomationProviderRequested(content, args));

	//		return S_OK;
	//	}).Get(),
	//            &_automationProviderRequestedToken));

	//    return S_OK;
	//}

	//_Check_return_ HRESULT ContentRoot.ResetCompositionContent()
	//{
	//    if (_compositionContent)
	//    {
	//        if (_compositionContentStateChangedToken.value != 0)
	//        {
	//            IFC_RETURN(_compositionContent->remove_StateChanged(_compositionContentStateChangedToken));
	//_compositionContentStateChangedToken = { };
	//        }

	//        if (_automationProviderRequestedToken.value != 0)
	//{
	//	IFC_RETURN(_compositionContent->remove_AutomationProviderRequested(_automationProviderRequestedToken));
	//	_automationProviderRequestedToken = { };
	//}

	//_compositionContent.Reset();
	//    }

	//    return S_OK;
	//}

	//_Check_return_ HRESULT ContentRoot.SetContentIsland(_In_ ixp.IContentIsland* compositionContent)
	//{
	//    // ContentRoot is re-used through the life time of an application. Hence, CompositionContent can be set multiple time.
	//    // ResetCompositionContent will make sure to remove handlers and reset previous CompositionContent.
	//    IFC_RETURN(ResetCompositionContent());
	//IFC_RETURN(RegisterCompositionContent(compositionContent));

	//if (var rootScale = RootScale.GetRootScaleForContentRoot(this)) // Check that we still have an active tree

	//	{
	//	rootScale->SetContentIsland(compositionContent);
	//}

	//return S_OK;
	//}

	//bool ContentRoot.ShouldUseVisualRelativePixels()
	//{
	//	if (XamlOneCoreTransforms.IsEnabled())
	//	{
	//		return true;
	//	}

	//	return false;
	//}

	//// CONTENT-TODO: Lifted IXP doesn't support OneCoreTransforms UIA yet.
	//#if false
	//UINT64 ContentRoot.GetVisualIdentifier()
	//{
	//    UINT64 visualIdentifier = 0;

	//    if (_type == Type.XamlIsland)
	//    {
	//        if (_xamlIslandRoot)
	//        {
	//            visualIdentifier = _xamlIslandRoot->GetVisualIdentifier();
	//        }
	//    }
	//    else
	//    {
	//        visualIdentifier = _coreServices.GetCoreWindowCompositionIslandId();
	//    }

	//    return visualIdentifier;
	//}
	//#endif

	/// <summary>
	/// Represents the input manager associated with this content root.
	/// </summary>
	internal InputManager InputManager => _inputManager;

	/// <summary>
	/// Represents focus adapter.
	/// </summary>
	internal FocusAdapter FocusAdapter => _focusAdapter;

	//XUINT32 ContentRoot.AddRef()
	//{
	//	return _ref_count.AddRef();
	//}

	//XUINT32 ContentRoot.Release()
	//{
	//	auto refCount = _ref_count.Release();
	//	if (0 == refCount)
	//	{
	//		_ref_count.start_destroying();
	//		delete this;
	//	}
	//	return refCount;
	//}

	/// <summary>
	/// Represents the focus manager associated with this content root.
	/// </summary>
	internal FocusManager FocusManager => _focusManager;

	//	_Check_return_ HRESULT ContentRoot.Close()
	//{
	//    if (_lifetimeState == LifetimeState.Closing || _lifetimeState == LifetimeState.Closed)
	//    {
	//        return S_OK;
	//    }

	//// It is possible for the ContentRoot to be deleted while being closed. Hold a
	//// ref to keep the instance alive until the completion of Close
	//xref_ptr<ContentRoot> keepAlive(this);

	//_lifetimeState = LifetimeState.Closing;
	//    bool releasePopupRoot = false;

	//ResetCompositionContent();

	//IFC_RETURN(_visualTree.ResetRoots(&releasePopupRoot));

	//    if (releasePopupRoot)
	//    {
	//        // Clear popups from PopupRoot, otherwise they would only be cleared when the PopupRoot is disposed,
	//        // which would only occur after the managed peer check.  These include open popups with no direct
	//        // managed peer.  See bug 99901.
	//        _visualTree.GetPopupRoot()->CloseAllPopupsForTreeReset();
	//    }

	//    // Release resources held by FocusManager's FocusRectManager. These
	//    // elements are automatically created on CFrameworkManager.UpdateFocus()
	//    // and must be released before core releases its main render target on
	//    // shutdown. Exposed by fixing core leak RS1 bug #7300521.
	//    _focusManager.ReleaseFocusRectManagerResources();

	//IFC_RETURN(_visualTree.Shutdown());

	//_akExport.SetVisualTree(null);
	//_akExport.SetFocusManager(null);

	//ContentRootCoordinator* coordinator = _coreServices.GetContentRootCoordinator();
	//coordinator->RemoveContentRoot(this);

	//_lifetimeState = LifetimeState.Closed;

	//return S_OK;
	//}

	//VectorOfKACollectionAndRefCountPair & ContentRoot.GetAllLiveKeyboardAccelerators()
	//{
	//	return _allLiveKeyboardAccelerators;
	//}

	//void ContentRoot.PrepareToClose()
	//{
	//	if (_lifetimeState == LifetimeState.Normal)
	//	{
	//		_lifetimeState = LifetimeState.PreparingToClose;
	//	}
	//}

	internal bool IsShuttingDown()
	{
		return _lifetimeState == LifetimeState.Closing
			|| _lifetimeState == LifetimeState.Closed
			|| _lifetimeState == LifetimeState.PreparingToClose;
	}

	//// This overrides default compare function for vector.
	//struct KeyboardAcceleratorCollectionComparer
	//{
	//	KeyboardAcceleratorCollectionComparer(const xref.weakref_ptr<CKeyboardAcceleratorCollection> weakKACollection) : _weakKACollection(weakKACollection) { }

	//	bool operator () (const KACollectionAndRefCountPair& pair) const
	//    {
	//        return (pair.first == _weakKACollection);
	//    }

	//const xref.weakref_ptr<CKeyboardAcceleratorCollection> _weakKACollection;
	//};

	//void ContentRoot.AddToLiveKeyboardAccelerators(_In_ CKeyboardAcceleratorCollection * const pKACollection)
	//{
	//    auto weakCollection = xref.get_weakref(pKACollection);
	//auto it = std.find_if(
	//	_allLiveKeyboardAccelerators.begin(),
	//	_allLiveKeyboardAccelerators.end(),
	//	KeyboardAcceleratorCollectionComparer(weakCollection)
	//);

	//if (it != _allLiveKeyboardAccelerators.end())
	//{
	//	it->second++;
	//}
	//else
	//{
	//	_allLiveKeyboardAccelerators.push_back(std.make_pair(weakCollection, 1));
	//}
	//}

	//void ContentRoot.RemoveFromLiveKeyboardAccelerators(_In_ CKeyboardAcceleratorCollection * const pKACollection)
	//{
	//    auto weakCollection = xref.get_weakref(pKACollection);
	//auto it = std.find_if(
	//	_allLiveKeyboardAccelerators.begin(),
	//	_allLiveKeyboardAccelerators.end(),
	//	KeyboardAcceleratorCollectionComparer(weakCollection)
	//);

	//if (it != _allLiveKeyboardAccelerators.end())
	//{
	//	int refCount = --it->second;
	//	if (refCount == 0)
	//	{
	//		_allLiveKeyboardAccelerators.erase(it);
	//	}
	//}
	//}

	internal XamlRoot GetOrCreateXamlRoot() => _visualTree.GetOrCreateXamlRoot();

	internal XamlRoot? XamlRoot => VisualTree.XamlRoot;

	private void RaiseXamlRootChanged(ChangeType changeType)
	{
		AddPendingXamlRootChangedEvent(changeType);
		bool shouldRaiseWindowChangedEvent = (changeType != ChangeType.Content) ? true : false;
		RaisePendingXamlRootChangedEventIfNeeded(shouldRaiseWindowChangedEvent);
	}

	private void RaisePendingXamlRootChangedEventIfNeeded(bool shouldRaiseWindowChangedEvent)
	{
		if (_hasPendingChangedEvent)
		{
			_hasPendingChangedEvent = false;

			if (XamlRoot is { } xamlRoot)
			{
				FxCallbacks.XamlRoot_RaiseChanged(xamlRoot);
			}

			// TODO: Implement in Uno
			//if (shouldRaiseWindowChangedEvent)
			//{
			//	var size = _visualTree.Size;
			//	.visualTree.GetQualifierContext()->OnWindowChanged(
			//		static_cast<XUINT32>(lround(size.Width)),
			//		static_cast<XUINT32>(lround(size.Height)));
			//}
		}
	}

	//void ContentRoot.RaiseXamlRootInputActivationChanged()
	//{
	//	if (auto xamlRoot = GetXamlRootNoRef())
	//    {
	//		FxCallbacks.XamlRoot_RaiseInputActivationChanged(xamlRoot);
	//	}
	//}

	//private GetOwnerWindow()
	//{
	//	if (_type == ContentRootType.CoreWindow)
	//	{
	//		var ownerWindow = static_cast<HWND>(_coreServices.GetHostSite()->GetXcpControlWindow());
	//		return ownerWindow;
	//	}

	//	else if (_type == ContentRootType.XamlIsland)
	//	{
	//		if (_xamlIslandRoot)
	//		{
	//			var hWndInputSite = _xamlIslandRoot->GetInputHWND();
	//			ASSERT(hWndInputSite != null);
	//			return hWndInputSite;
	//		}
	//	}
	//	else
	//	{
	//		XAML_FAIL_FAST();
	//	}
	//	return null;
	//}

	//// Return true if and only if the contentRoot's top-level window is currently in the
	//// activated state.
	//bool ContentRoot.IsWindowActivated() const
	//{
	//    const HWND topLevelWindow = .GetAncestor(GetOwnerWindow(), GA_ROOT);
	//if (topLevelWindow != NULL)
	//{
	//	return (topLevelWindow == .GetForegroundWindow());
	//}
	//return false;
	//}

	//void ContentRoot.SetIsInputActive(bool isInputActive)
	//{
	//	_isInputActive = isInputActive;
	//}

	//bool ContentRoot.GetIsInputActive() const
	//{
	//    return _isInputActive;
	//}

	//wrl.ComPtr<ixp.IContentIslandEnvironment> ContentRoot.GetContentIslandEnvironment() const
	//{
	//    // We only support islands right now.  Always return null for UWP.
	//    if (_type == Type.XamlIsland && _xamlIslandRoot)
	//    {
	//        return _xamlIslandRoot->GetContentIslandEnvironment();
	//    }    
	//    return null;
	//}

	//TODO Uno: Initialize properly when Access Keys are supported (see #3219)
	/// <summary>
	/// Access key export.
	/// </summary>
	internal AccessKeyExport AccessKeyExport => _akExport;
}
