// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\FrameworkApplication_Partial.cpp, tag winui3/release/1.4.3, commit 685d2bfa86d6169aa1998a7eaa2c38bfcf9f74bc

using DirectUI;
using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml;

partial class Application
{
	internal static void StartOnCurrentThread()
	{
		_spApplicationInitializationCallback = pCallback;

		// if this thread wasn't already initialized in FrameworkApplication.StartDesktop, then init it now
		if (!DXamlCore.IsInitializedStatic())
		{
			DXamlCore.Initialize(InitializationType.IslandsOnly);
		}

		DXamlCore.Current.EnsureCoreApplicationInitialized();

		// ICoreWindow parameter is not created as part of the Desktop initialization path. It is only needed for UWP.
		DXamlCore.Current.ConfigureJupiterWindow(null);

		// call the OnLaunchedProtected method
		ctl::ComPtr<NormalLaunchActivatedEventArgs> uwpLaunchActivatedEventArgs;
		IFC_RETURN(NormalLaunchActivatedEventArgs::Create(uwpLaunchActivatedEventArgs.ReleaseAndGetAddressOf()));
		IFC_RETURN(InvokeOnLaunchActivated(uwpLaunchActivatedEventArgs.Get()));
	}

	// Shared startup for UWP and desktop apps
	// See startup-overview.md for details
	_Check_return_ HRESULT FrameworkApplicationFactory::StartImpl(_In_ xaml::IApplicationInitializationCallback* pCallback)
	{

		g_spApplicationInitializationCallback = pCallback;

		// Determine which AppPolicyWindowingModel the application is using.
		//
		//     AppPolicyWindowingModel_None
		//     AppPolicyWindowingModel_Universal (WinUI UWP)
		//     AppPolicyWindowingModel_ClassicDesktop (WinUI Desktop)
		//     AppPolicyWindowingModel_ClassicPhone
		//
		AppPolicyWindowingModel policy = AppPolicyWindowingModel_None;
		LONG status = AppPolicyGetWindowingModel(GetCurrentThreadEffectiveToken(), &policy);
		if (status != ERROR_SUCCESS)
		{
			IFC_RETURN(E_FAIL);
	}

		if (policy == AppPolicyWindowingModel_ClassicDesktop)
		{
			return FrameworkApplication::StartDesktop();
		}

		else if (policy == AppPolicyWindowingModel_Universal)
	{
		return FrameworkApplication::StartUWP(pCallback);
	}

	return E_FAIL;
	}

	/* static */ _Check_return_ HRESULT FrameworkApplication::StartDesktop()
	{
		ctl::ComPtr<ApplicationInitializationCallbackParams> pParams;
	ctl::ComPtr<xaml::Hosting::IWindowsXamlManagerStatics> windowsXamlManagerFactory;
	ctl::ComPtr<xaml::Hosting::IWindowsXamlManager> windowsXamlManager;

	wrl::ComPtr<msy::IDispatcherQueueControllerStatics> dispatcherQueueControllerStatics;
	wrl::ComPtr<msy::IDispatcherQueueController> dispatcherQueueController;
	wrl::ComPtr<msy::IDispatcherQueueController2> dispatcherQueueController2;

	IFCFAILFAST(wf::GetActivationFactory(
		wrl::Wrappers::HStringReference(RuntimeClass_Microsoft_UI_Dispatching_DispatcherQueueController).Get(),
		&dispatcherQueueControllerStatics));
	IFCFAILFAST(dispatcherQueueControllerStatics->CreateOnCurrentThread(&dispatcherQueueController));
	IFCFAILFAST(dispatcherQueueController.As(&dispatcherQueueController2));

	// init Jupiter for this thread
	IFC_RETURN(DXamlCore::Initialize(InitializationType::IslandsOnly));

	//  Invoke the ApplicationInitialization callback set by FrameworkApplication::StartImpl
	if (g_spApplicationInitializationCallback)
	{
		// Invoke the callback, usually to create a custom Application object instance
		IFCFAILFAST(ctl::ComObject < ApplicationInitializationCallbackParams >::CreateInstance(&pParams));
		IFCFAILFAST(g_spApplicationInitializationCallback->Invoke(pParams.Get()));
		g_spApplicationInitializationCallback.Reset();
	}

	// Create WindowsXamlManager (WindowsXamlManager::Initialize will call FrameworkApplication::StartOnCurrentThread())
	IFCFAILFAST(ctl::GetActivationFactory(wrl_wrappers::HStringReference(RuntimeClass_Microsoft_UI_Xaml_Hosting_WindowsXamlManager).Get(), &windowsXamlManagerFactory));
	IFCFAILFAST(windowsXamlManagerFactory->InitializeForCurrentThread(&windowsXamlManager));

	// We must have an XAML application instance at this point
	if (FrameworkApplication::GetCurrentNoRef() == nullptr)
	{
		XAML_FAIL_FAST();
	}

	//  Start the main WinUI Desktop message loop
	FrameworkApplication::RunDesktopWindowMessageLoop();

	// Note we hold a reference to windowsXamlManager here, and we don't close it, because we want Xaml to shut down
	// during DispatcherQueue.ShutdownQueue (in the DispatcherQueue.FrameworkShutdownStarting event handler).  See
	// xaml-shutdown.md for more detail about the shutdown process.

	IFCFAILFAST(dispatcherQueueController2->ShutdownQueue());

	return S_OK;
	}

	/* static */ _Check_return_ HRESULT FrameworkApplication::StartUWP(_In_ xaml::IApplicationInitializationCallback* pCallback)
	{
		return RunInActivationMode();
	}
}
