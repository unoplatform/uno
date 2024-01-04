// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// DXamlCore.h, DXamlCore.cpp

#nullable enable

using System;
using System.Collections.Generic;
using Uno.UI.Helpers.WinUI;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using static Microsoft.UI.Xaml.Controls._Tracing;
using Uno.Disposables;

namespace DirectUI;

internal partial class DXamlCore
{
	private static DXamlCore? _instance;

	private Dictionary<string, List<WeakReference<RadioButton>>>? _radioButtonGroupsByName;

	private BuildTreeService? _buildTreeService;
	private BudgetManager? _budgetManager;

	// UNO: This should **NOT** create the singleton!
	//		_but_ if we do return a 'null' the 'OnApplyTemplate' of the `CalendarView` will fail.
	//		As for now our implementation of the 'DXamlCore' is pretty light and stored as a basic singleton,
	//		we accept to create it even with the "NoCreate" overload.
	public static DXamlCore Current => _instance;

	public static DXamlCore GetCurrentNoCreate() => Current;

	public Uno.UI.Xaml.Core.CoreServices GetHandle() => Uno.UI.Xaml.Core.CoreServices.Instance;

	public Rect DipsToPhysicalPixels(float scale, Rect dipRect)
	{
		var physicalRect = dipRect;
		physicalRect.X = dipRect.X * scale;
		physicalRect.Y = dipRect.Y * scale;
		physicalRect.Width = dipRect.Width * scale;
		physicalRect.Height = dipRect.Height * scale;
		return physicalRect;
	}

	// TODO Uno: Application-wide bar is not supported yet.
	public ApplicationBarService? TryGetApplicationBarService() => null;

	public string GetLocalizedResourceString(string key)
		=> ResourceAccessor.GetLocalizedStringResource(key);

	public BuildTreeService GetBuildTreeService()
		=> _buildTreeService ??= new BuildTreeService();

	public BudgetManager GetBudgetManager()
		=> _budgetManager ??= new BudgetManager();

	public ElementSoundPlayerService GetElementSoundPlayerServiceNoRef()
		=> ElementSoundPlayerService.Instance;

	internal Dictionary<string, List<WeakReference<RadioButton>>>? GetRadioButtonGroupsByName(bool ensure)
	{
		if (_radioButtonGroupsByName == null && ensure)
		{
			_radioButtonGroupsByName = new Dictionary<string, List<WeakReference<RadioButton>>>();
		}

		return _radioButtonGroupsByName;
	}

	public static void Initialize(InitializationType initializationType)
	{
		var pCore = _instance;

		if (pCore is not null)
		{
			var state = pCore.GetState();

			// If there already is a DXamlCore instance for this thread then ensure it's initialized
			// or that we are idle and that's how we want to initialize.
			MUX_ASSERT(DXamlCore.State.Initialized == state || DXamlCore.State.Idle == state && InitializationType.FromIdle == initializationType);
		}
		else
		{
			pCore = new DXamlCore();
		}

		// Don't do anything if we are already initialized
		if (pCore.GetState() != DXamlCore.State.Initialized)
		{
			// if instance initialization failed, we need to undo what was done
			// Note that destructor too accesses the instance and must be available through
			// GetCurrent() - even though it was not successfully initialized
			var undoOnFailure = Disposable.Create(() =>
			{
				pCore = null;
				_instance = null;
			});

			try
			{

				// The ReferenceTrackerManager is a singleton that's shared between threads, so we need to hold a reference to
				// protect it from going away (in case another thread calls ReferenceTrackerManager.UnregisterCore before we call
				// RegisterCore).
				//ctl.ComPtr<ReferenceTrackerManager> keepAlive;
				//IFC_RETURN(ReferenceTrackerManager.EnsureInitialized(&keepAlive));

				// Note that during InitializeInstance, the instance must be available through
				// GetCurrent() - even though it isn't fully initialized yet. That's because
				// many code paths called from InitializeInstance use GetCurrent().
				// If initializing from an idle state, then we already have set the value in
				// DXamlInstanceStorage
				if (InitializationType.FromIdle != initializationType)
				{
					_instance = pCore;
				}

				if (initializationType == InitializationType.FromIdle)
				{
					throw new InvalidOperationException("From Idle initialization is not currently supported in Uno Platform");
					// pCore.InitializeInstanceFromIdle();
				}
				else if (initializationType != InitializationType.Xbf)
				{
					pCore.InitializeInstance(initializationType);
				}
				else
				{
					throw new InvalidOperationException("Xbf initialization is not currently supported in Uno Platform");
					// pCore.InitializeInstanceForXbf();
				}

				// Now that we have a good core, we can register with the ReferenceTrackerManager
				if (InitializationType.FromIdle != initializationType)
				{
					ReferenceTrackerManager.RegisterCore(pCore);
				}

				// Initialize simple property callbacks for CUIElement.  This needs to happen regardless of initialization type,
				// including from idle, since when the core is deinitialized to idle it destroys all simple properties and their callback tables.
				// UIElement.RegisterSimplePropertyCallbacks();

				// at this point we have a properly initialized instance
				MUX_ASSERT(DXamlCore.State.Initialized == pCore.GetState());

				// We can dismiss the guard now that we know we have successfully initialized
				undoOnFailure = null;
			}
			finally
			{
				undoOnFailure?.Dispose();
			}
		}
		else
		{
			// Tests shut down by calling DeinitializeInstanceToIdle on the DXamlCore, which will deinitialize the
			// DCompSurfaceFactoryManager. The intent is for the next test to pick up the same DXamlCore and call
			// InitializeInstanceFromIdle on it, which reinitializes the DCompSurfaceFactoryManager.
			//
			// WPF tests can can initialize the DXamlCore on a separate UI thread, which means they don't find the
			// previous DXamlCore to call InitializeInstanceFromIdle. They also don't go through DllMain again, so
			// make sure that the DCompSurfaceFactoryManager is initialized.
			DCompSurfaceFactoryManager.EnsureInitialized();
		}
	}

	//
	// This is the normal xaml runtime initialization code used by designer, shell, immersive apps.
	//
	// NOTE:
	// If you are modifying this method, please consider if it needs to be added to the
	// InitializeInstanceForXbf() method for the XBF core initialization path.
	//
	private void InitializeInstance(InitializationType initializationType)
	{

		CoreServices? pCore = null;
		SuspendingEventHandler* pSuspendingEventHandler = NULL;
		IEventHandler<IInspectable*>* pResumingEventHandler = NULL;

	#if DBG
		WaitForDebuggerIfNeeded();
	#endif

		TraceInitializeCoreBegin();

		// Start TIP test
		var initDxamlCoreTest = tip.start_and_watch_errors<DXamlInitializeCoreTest>();

		// Log the mux version
		initDxamlCoreTest->muxVersion = TipTestHelper.GetMuxVersion();

		m_state = DXamlCore.Initializing;

		// Initialize the lock used to access the peer table.
		InitializeSRWLock(&m_peerReferenceLock);

		// Take a reference on the FinalUnhandledErrorDetected event registration.
		// This reference will be released when this DXamlCore instance is deinitialized.
		IFC(ErrorHelper.GetFinalUnhandledErrorDetectedRegistration()->AddRefRegistration());

		// Create a PLM handler for this core, if this process is packaged -- and if we're not using
		// "IslandsOnly" initialization, which means we're running in a win32 context.
		if (gps->IsProcessPackaged() && (initializationType != InitializationType.IslandsOnly))
		{
			initDxamlCoreTest.set_flag(TIP_reason(DXamlInitializeCoreTest.reason.packaged_process));
			IFC(XAML.PLM.PLMHandler.CreateForASTA(this, &m_pPLMHandler));
		}

		if (initializationType != InitializationType.IslandsOnly)
		{
			initDxamlCoreTest.set_flag(TIP_reason(DXamlInitializeCoreTest.reason.init_type_uwp));
			// When running in UWP, we must make sure there's a DispatcherQueueController on the thread, since XAML
			// requires one to be running.  Since we don't support UWP, we don't bother to shutdown the DQC, this is
			// just to keep XAML tests running.
			DispatcherQueueControllerStatics> dispatcherQueueControllerStatics;

		IFCFAILFAST(GetActivationFactory(
			wrl.Wrappers.HStringReference(RuntimeClass_Microsoft_UI_Dispatching_DispatcherQueueController).Get(),
					&dispatcherQueueControllerStatics));
				IFCFAILFAST(dispatcherQueueControllerStatics->CreateOnCurrentThread(&m_dispatcherQueueController));
			}

			if (initializationType == InitializationType.MainView)
		{
			initDxamlCoreTest.set_flag(TIP_reason(DXamlInitializeCoreTest.reason.init_type_main_view));
			// We allow this temporarily only for UWP because we're not supporting it for foward-compat yet.
			//  Task 29643834: Remove use of textinputproducerinternal.h before we open-source and before we fully-support UWP
			//                 (ITextInputConsumer, ITextInputProducer, ITextInputProducerInternal)
			TextInputProducerHelper.SetAllowCallsToPrivateWindowsFunctions(true);
		}

		XamlOneCoreTransforms.EnsureInitialized(XamlOneCoreTransforms.InitMode.Normal);

		IFC(JupiterControl.Create(&m_pControl));

		pCore = m_pControl->GetCoreServices();
		IFCEXPECT(pCore);

		IFC(pCore->SetCurrentApplication(NULL));

		// Keep a reference from the core context to the DXamlCore
		IFC(CoreImports.SetFrameworkContext(pCore, this));
		ReplaceInterface(m_hCore, pCore);
		pCore = NULL;


		// TSF3 rely on CoreDispathcer. TextServicesManager checks for a CoreDispatcher to determine UIThread and fails in desktop/ island without CoreWindow, hence
		// in Island mode force to use TSF1
		if (initializationType == InitializationType.IslandsOnly)
		{
			initDxamlCoreTest.set_flag(TIP_reason(DXamlInitializeCoreTest.reason.init_type_islands_only));
			m_hCore->ForceDisableTSF3();
			initDxamlCoreTest.set_flag(TIP_reason(DXamlInitializeCoreTest.reason.disabled_tsf3));
		}

		// initialize the XAML dispatcher
		IFC(ctl.ComObject < DispatcherImpl >.CreateInstance(m_spDispatcherImpl.ReleaseAndGetAddressOf()));
		IFC(m_spDispatcherImpl->Connect(this));
		initDxamlCoreTest.set_flag(TIP_reason(DXamlInitializeCoreTest.reason.initialized_dispatcher));

		// Disable the legacy IME since the legacy IMEs aren't designed for the immersive environment.
		//
		// We only want to do this when not in design mode, since the design-mode process should use the legacy IMEs.
		ImmDisableLegacyIME();

		m_hCore->SetInitializationType(initializationType);

		m_pDragDrop = NULL;

		m_staticStoreGuard = StaticStore.GetInstance();

		// Initialize the default control styles cache
		m_pDefaultStyles = new DefaultStyles();

		// Force the theming object to update system color brushes as well as
		// query initial system theme.
		IFC(m_hCore->GetFrameworkTheming()->OnThemeChanged());

		IFC(EnsureEventArgs());

		IFC(Window.Create(this, &m_uwpWindowNoRef));
		initDxamlCoreTest.set_flag(TIP_reason(DXamlInitializeCoreTest.reason.created_uwp_window));

		// The Window needs to be pegged because it doesn't have an entry in the PeerTable,
		// and its members can potentially be GCed.
		m_uwpWindowNoRef->UpdatePeg(true);

		IFC(ctl.make<UIAffinityReleaseQueue>(&m_spReleaseQueue));

		// Initialize Visual Diagnostics
		IFC(RegisterVisualDiagnosticsPort());

		m_state = DXamlCore.Initialized;

		// Now that DXamlCore is initialized, listen to the UISettings' AnimationsEnabledChanged event for future updates
		// and immediately refresh the m_isAnimationEnabled field.
		IFC(AddAnimationsEnabledChangedHandler());
		// m_animationsEnabledChangedToken will be zero if AddAnimationsEnabledChangedHandler() fails to QI IUISettings6
		if (m_animationsEnabledChangedToken.value != 0)
		{
			IFC(UpdateAnimationsEnabled());
		}

		Cleanup:
		if (FAILED(hr))
		{
			m_state = DXamlCore.InitializationFailed;
			initDxamlCoreTest.set_flag(TIP_reason(DXamlInitializeCoreTest.reason.failed_dxamlcore_init));
			if (m_uwpWindowNoRef != nullptr)
			{
				m_uwpWindowNoRef->SetDXamlCore(nullptr);
				m_uwpWindowNoRef->UpdatePeg(false);
			}
		}

		ReleaseInterface(pCore);
		ReleaseInterface(pSuspendingEventHandler);
		ReleaseInterface(pResumingEventHandler);

		TraceInitializeCoreEnd();

		// End Tip Test - if cleanup is called
		initDxamlCoreTest.complete();
	}
}
