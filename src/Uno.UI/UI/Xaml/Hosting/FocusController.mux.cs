// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\UIBridgeFocus\FocusController.cpp, tag winui3/release/1.5.1, commit 3d10001ba8e12336cad392846b1030ba691b784a
#nullable enable

using System;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Hosting;
using Uno.Disposables;
using Uno.UI.Xaml.Input;
using Windows.Foundation;
using Windows.UI.Core;

using Microsoft.UI.Dispatching;

namespace DirectUI;

internal partial class FocusController
{
	//public FocusController(CoreComponentFocusable pFocusable)
	//{
	//	_coreComponentFocusable = pFocusable;
	//}

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
		//if (_coreComponentFocusable != null)
		//{
		//	_coreComponentFocusable.GotFocus += OnCoreInputGotFocus;
		//	_gotFocusToken.Disposable = Disposable.Create(() => _coreComponentFocusable.GotFocus -= OnCoreInputGotFocus);
		//}
		//else
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
			_inputObjectFocusable!.GotFocus += OnInputControllerGotFocus;
			_gotFocusToken.Disposable = Disposable.Create(() => _inputObjectFocusable.GotFocus -= OnInputControllerGotFocus);
		}
	}

	//public void DeInit()
	//{
	//	_gotFocusToken.Disposable = null;
	//}

	//public static FocusController Create(CoreComponentFocusable pFocusable)
	//{
	//	var focusController = new FocusController(pFocusable);
	//	focusController.Init();
	//	return focusController;
	//}

	public static FocusController Create(InputFocusController focusable)
	{
		var focusController = new FocusController(focusable);
		focusController.Init();
		return focusController;
	}

	public XamlSourceFocusNavigationResult NavigateFocus(XamlSourceFocusNavigationRequest request, FocusObserver focusObserver)
	{
		_currentRequest = request;
		using var scopeExit = Disposable.Create(() =>
		{
			_currentRequest = null;
		});

		bool isHandled = focusObserver.ProcessNavigateFocusRequest(request);

		var result = new NavigateFocusResult(isHandled);
		return result;
	}

	internal event TypedEventHandler<object, object>? FocusDeparting;

	internal event TypedEventHandler<object, object>? GotFocus;

	public void DepartFocus(XamlSourceFocusNavigationRequest request)
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

		_inputObjectFocusable!.DepartFocus(ixpRequest);

		// WinUI may wish to respond to the result (Moved/NotMoved/NoFocusableElements) here in some way
	}

	internal void OnCoreInputGotFocus(object sender, CoreWindowEventArgs e) => OnGotFocusCommon();

	private void OnGotFocusCommon()
	{
		var dispatcherQueue = DispatcherQueue.GetForCurrentThread();

		var currentRequest = _currentRequest;
		_currentRequest = null;

		var callback = new DispatcherQueueHandler(() =>
		{
			FireGotFocus(currentRequest);
		});

		dispatcherQueue.TryEnqueue(callback);
	}

	public bool HasFocus =>
		//_coreComponentFocusable != null ? _coreComponentFocusable.HasFocus : 
		_inputObjectFocusable!.HasFocus;

	private void FireGotFocus(XamlSourceFocusNavigationRequest? currentRequest)
	{
		var request = currentRequest;
		if (request is null)
		{
			request = new XamlSourceFocusNavigationRequest(XamlSourceFocusNavigationReason.Restore);
		}

		var args = new NavigationGotFocusEventArgs(request);
		var gotFocusEventArgs = args as DesktopWindowXamlSourceGotFocusEventArgs;

		GotFocus?.Invoke(this, gotFocusEventArgs);
	}
}
