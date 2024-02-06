// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\FrameworkApplication_Partial.cpp, tag winui3/release/1.4.3, commit 685d2bfa86d6169aa1998a7eaa2c38bfcf9f74bc

#nullable enable

using System;
using System.Threading;
using DirectUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Hosting;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Islands;
using static Uno.UI.Xaml.Internal.AppModel;

namespace Microsoft.UI.Xaml;

partial class Application
{
	private static ApplicationInitializationCallback? _applicationInitializationCallback;

	internal void StartOnCurrentThread(ApplicationInitializationCallback? pCallback)
	{
		_applicationInitializationCallback = pCallback;

		// if this thread wasn't already initialized in FrameworkApplication.StartDesktop, then init it now
		if (!DXamlCore.IsInitializedStatic)
		{
			DXamlCore.Initialize(InitializationType.IslandsOnly);
		}

		DXamlCore.Current.EnsureCoreApplicationInitialized();

		// ICoreWindow parameter is not created as part of the Desktop initialization path. It is only needed for UWP.
		DXamlCore.Current.ConfigureJupiterWindow(null);

		// TODO Uno: This currently happens in platform-specific code, probably should be
		// moved here to a shared location, but not clear on how to handle Activated vs. Launched yet.

		// call the OnLaunched method
		//NormalLaunchActivatedEventArgs > uwpLaunchActivatedEventArgs;
		//IFC_RETURN(NormalLaunchActivatedEventArgs::Create(uwpLaunchActivatedEventArgs.ReleaseAndGetAddressOf()));
		//IFC_RETURN(InvokeOnLaunchActivated(uwpLaunchActivatedEventArgs.Get()));
	}

	internal XamlIsland CreateIsland()
	{
		var dxamlCore = DXamlCore.Current;

		// http://osgvsowi/17333449 - (deliverable) This is a temporary trick to kick off XAML's normal applicaiton startup path.
		// we need to refactor islands and xaml::Window to reduce the complexity of the code as captured by the task below
		// https://microsoft.visualstudio.com/OS/_workitems/edit/37066232
		// var window = DXamlCore.Current.GetDummyWindow();
		// window.EnsureInitializedForIslands();

		// Create a new XamlIsland.
		var newIsland = new XamlIsland();

		// Notify core of new XamlIsland
		var coreServices = dxamlCore.GetHandle();
		coreServices.AddXamlIslandRoot(newIsland);
		return newIsland;
	}

	internal XamlIsland CreateIslandWithContentBridge(object owner, object contentBridge)
	{
		var dxamlCore = DXamlCore.Current;

		// http://osgvsowi/17333449 - (deliverable) This is a temporary trick to kick off XAML's normal application startup path.
		// we need to refactor islands and xaml::Window to reduce the complexity of the code as captured by the task below
		// https://microsoft.visualstudio.com/OS/_workitems/edit/37066232
		//var window = DXamlCore.Current.GetDummyWindow();
		//window.EnsureInitializedForIslands();

		// Create a new XamlIsland.
		var newIsland = new XamlIsland();

		// Notify core of new XamlIsland
		var coreServices = dxamlCore.GetHandle();
		coreServices.AddXamlIslandRoot(newIsland);

		newIsland.SetOwner(owner);
		return newIsland;
	}

	internal void RemoveIsland(XamlIsland value)
	{
		var xamlIslandRoot = value;

		if (xamlIslandRoot is not null)
		{
			var dxamlCore = DXamlCore.Current;
			var coreServices = dxamlCore.GetHandle();
			coreServices.RemoveXamlIslandRoot(xamlIslandRoot);
		}
	}

	/// <summary>
	/// Provides the entry point and requests initialization of the application. 
	/// Use the callback to instantiate the Application class.
	/// </summary>
	/// <param name="callback">The callback that should be invoked during the initialization sequence.</param>
	public static
#if __WASM__
		async
#endif
		void Start(ApplicationInitializationCallback callback)
	{
#if __WASM__ || __SKIA__
		try
		{
#if __WASM__
			await BeforeStartAsync();
#endif
#if __SKIA__
			BeforeStart();
#endif
		}
		catch (Exception ex)
		{
			if (typeof(Application).Log().IsEnabled(LogLevel.Error))
			{
				typeof(Application).Log().LogError("Application initialization failed.", ex);
			}
		}
#endif
		_applicationInitializationCallback = callback;

		// Determine which AppPolicyWindowingModel the application is using.
		//
		//     AppPolicyWindowingModel_None
		//     AppPolicyWindowingModel_Universal (WinUI UWP)
		//     AppPolicyWindowingModel_ClassicDesktop (WinUI Desktop)
		//     AppPolicyWindowingModel_ClassicPhone
		//
		var policy = AppPolicyGetWindowingModel(Thread.CurrentThread);

		if (policy == AppPolicyWindowingModel.ClassicDesktop)
		{
			StartDesktop();
		}
		else if (policy == AppPolicyWindowingModel.Universal)
		{
			StartUWP(callback);
		}
	}

	private static void StartDesktop()
	{
		DispatcherQueue.CreateOnCurrentThread();

		// init Jupiter for this thread
		DXamlCore.Initialize(InitializationType.IslandsOnly);

		//  Invoke the ApplicationInitialization callback set by Start
		if (_applicationInitializationCallback is not null)
		{
			// Invoke the callback, usually to create a custom Application object instance
			_applicationInitializationCallback.Invoke(new ApplicationInitializationCallbackParams());
			_applicationInitializationCallback = null;
		}

		// Create WindowsXamlManager (WindowsXamlManager will call StartOnCurrentThread()
		WindowsXamlManager.InitializeForCurrentThread();

		// We must have an XAML application instance at this point
		if (Current is null)
		{
			throw new InvalidOperationException("No XAML application instance was created by the ApplicationInitialization callback");
		}

		//  Start the main WinUI Desktop message loop
		// RunDesktopWindowMessageLoop();

		// Note we hold a reference to windowsXamlManager here, and we don't close it, because we want Xaml to shut down
		// during DispatcherQueue.ShutdownQueue (in the DispatcherQueue.FrameworkShutdownStarting event handler).  See
		// xaml-shutdown.md for more detail about the shutdown process.

		//IFCFAILFAST(dispatcherQueueController2->ShutdownQueue());
	}

	private static void StartUWP(ApplicationInitializationCallback pCallback)
	{
		// return RunInActivationMode();
	}

	private void Initialize()
	{
		// TODO Uno: Use CApplicationLock
		if (Current is not null)
		{
			throw new InvalidOperationException("Application was already created");
		}

		Current = this;

		//IFC_RETURN(ctl::ComObject < DirectUI::DebugSettings >::CreateInstance(&m_pDebugSettings));
		//IFCEXPECT_RETURN(m_pDebugSettings);   // Should never fail
		//m_pDebugSettings->UpdatePeg(true);

		//// Set up the metadata resetter. This object clears out the process-wide metadata when it is safe to do so (before
		//// DLLs are getting unloaded, but after we're done shutting down the visual tree).
		//m_metadataRef = std::make_shared<MetadataResetter>();

		// Determine which AppPolicyWindowingModel is being used. Use Application::GetAppPolicyWindowingModel() to
		// get the current Windowing model.
		//
		//     AppPolicyWindowingModel_None
		//     AppPolicyWindowingModel_Universal (WinUI UWP)
		//     AppPolicyWindowingModel_ClassicDesktop (WinUI Desktop)
		//     AppPolicyWindowingModel_ClassicPhone
		//
		_appPolicyWindowingModel = AppPolicyGetWindowingModel(Thread.CurrentThread);

		//  Register for CoreApplication's 'BackgroundActivated' event if not running as WinUI Desktop
		//if (AppPolicyWindowingModel_ClassicDesktop != m_appPolicyWindowingModel)
		//{
		//	// 'BackgroundActivated' is fired when in-process background tasks are invoked, after FrameworkView is created and
		//	// initialized, and the App constructor is called.
		//	IFC_RETURN(HookBackgroundActivationEvents(true));
		//}

		//IFC_RETURN(FrameworkApplicationGenerated::Initialize());

		FocusVisualKind = FocusVisualKind.HighVisibility;
	}
}
