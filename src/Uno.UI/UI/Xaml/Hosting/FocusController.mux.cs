// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\UIBridgeFocus\FocusController.cpp, tag winui3/release/1.5.1, commit 3d10001ba8e12336cad392846b1030ba691b784a

using System;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Hosting;
using Uno.Disposables;
using Windows.Foundation;
using Windows.UI.Core;

namespace DirectUI;

internal partial class FocusController
{
	public FocusController(CoreComponentFocusable pFocusable)
	{
		_coreComponentFocusable = pFocusable;
	}

	public FocusController(InputFocusController pFocusable)
	{
		_inputObjectFocusable = pFocusable;
	}

	//~FocusController()
	//{
	//	// This can return E_CLOSED, which is safe to ignore.
	//	DeInit();
	//}

	public void Init()
	{
		if (_coreComponentFocusable != null)
		{
			_coreComponentFocusable.GotFocus += OnCoreInputGotFocus;
			_gotFocusToken.Disposable = Disposable.Create(() => _coreComponentFocusable.GotFocus -= OnCoreInputGotFocus);
		}
		else
		{
			var wrThis = new WeakReference<FocusController>(this);
			void OnInputControllerGotFocus(object sender, FocusChangedEventArgs args)
			{
				if (wrThis.TryGetTarget(out var instance) && instance is FocusController focusController)
				{
					args.Handled = true;
					focusController.OnGotFocusCommon();
				}
			}
			_inputObjectFocusable.GotFocus += OnInputControllerGotFocus;
			_gotFocusToken.Disposable = Disposable.Create(() => _inputObjectFocusable.GotFocus -= OnInputControllerGotFocus);
		}
	}

	//public void DeInit()
	//{
	//	_gotFocusToken.Disposable = null;
	//}

	public static IAsyncOperation<FocusController> CreateAsync(CoreComponentFocusable pFocusable)
	{
		var focusController = new FocusController(pFocusable);
		focusController.Init();
		return AsyncOperation.FromTask<FocusController>(() => focusController);
	}

	public static IAsyncOperation<FocusController> CreateAsync(InputFocusController pFocusable)
	{
		var focusController = new FocusController(pFocusable);
		focusController.Init();
		return AsyncOperation.FromTask<FocusController>(() => focusController);
	}

	public IAsyncOperation<XamlSourceFocusNavigationResult> NavigateFocusAsync(XamlSourceFocusNavigationRequest pRequest, FocusObserver pFocusObserver)
	{
		_currentRequest = pRequest;
		var scopeExit = new Deferral(() =>
		{
			_currentRequest = null;
		});

		bool isHandled = false;
		pFocusObserver.ProcessNavigateFocusRequest(pRequest, out isHandled);

		var result = new NavigateFocusResult(isHandled);
		return AsyncOperation.FromTask<XamlSourceFocusNavigationResult>(() => result);
	}

	internal event TypedEventHandler<object, object> FocusDeparting;

	internal event TypedEventHandler<object, object> GotFocus;

	public FocusNavigationResult DepartFocus(XamlSourceFocusNavigationRequest request)
	{
		var spLosingFocusRequest = new NavigationLosingFocusEventArgs(request);
		var departingEventArgs = spLosingFocusRequest as DesktopWindowXamlSourceTakeFocusRequestedEventArgs;

		FocusDeparting?.Invoke(this, departingEventArgs);

		// Trigger IXP focus navigation
		var ixpReason = FocusNavigationReason.First;
		var xamlReason = request.Reason;

		switch (xamlReason)
		{
			case XamlSourceFocusNavigationReason.Programmatic:
				ixpReason = FocusNavigationReason.Programmatic;
				break;
			case XamlSourceFocusNavigationReason.Restore:
				ixpReason = FocusNavigationReason.Restore;
				break;
			case XamlSourceFocusNavigationReason.First:
				ixpReason = FocusNavigationReason.First;
				break;
			case XamlSourceFocusNavigationReason.Last:
				ixpReason = FocusNavigationReason.Last;
				break;
			case XamlSourceFocusNavigationReason.Up:
				ixpReason = FocusNavigationReason.Up;
				break;
			case XamlSourceFocusNavigationReason.Down:
				ixpReason = FocusNavigationReason.Down;
				break;
			case XamlSourceFocusNavigationReason.Left:
				ixpReason = FocusNavigationReason.Left;
				break;
			case XamlSourceFocusNavigationReason.Right:
				ixpReason = FocusNavigationReason.Right;
				break;
		}

		var hintRect = request.HintRect;
		var correlationId = request.CorrelationId;
		var ixpRequest = FocusNavigationRequest.Create(ixpReason, hintRect, correlationId);

		var inputFocusController2 = _inputObjectFocusable as InputFocusController;
		var result = inputFocusController2.DepartFocusAsync(ixpRequest);

		// WinUI may wish to respond to the result (Moved/NotMoved/NoFocusableElements) here in some way

		return AsyncOperation.FromTask<Windows.UI.Xaml.Input.FocusNavigationResult>(() => result);
	}

	private IAsyncAction OnCoreInputGotFocus(object sender, CoreWindowEventArgs e)
	{
		return OnGotFocusCommon().AsAsyncAction();
	}

	private IAsyncAction OnGotFocusCommon()
	{
		var spDispatcherQueueStatics = DispatcherQueue.GetForCurrentThread();
		var spDispatcherQueue = spDispatcherQueueStatics as DispatcherQueue;

		var currentRequest = _currentRequest;
		_currentRequest = null;

		var callback = new DispatcherQueueHandler(() =>
		{
			FireGotFocus(currentRequest);
		});

		spDispatcherQueue.TryEnqueue(callback);
	}

	public bool HasFocus => _coreComponentFocusable != null ? _coreComponentFocusable.HasFocus : _inputObjectFocusable.HasFocus;

	private void FireGotFocus(XamlSourceFocusNavigationRequest pCurrentRequest)
	{
		var spRequest = pCurrentRequest ?? CreateActivationFactory_XamlSourceFocusNavigationRequest().CreateInstance(XamlSourceFocusNavigationReason.Restore);

		var spArgs = new NavigationGotFocusEventArgs(spRequest);
		var gotFocusEventArgs = spArgs as DesktopWindowXamlSourceGotFocusEventArgs;

		_gotFocusEvent.InvokeAll(this, gotFocusEventArgs);
	}

	private IActivationFactory CreateActivationFactory_XamlSourceFocusNavigationRequest()
	{
		throw new NotImplementedException();
	}
}
