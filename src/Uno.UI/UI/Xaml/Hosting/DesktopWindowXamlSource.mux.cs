// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\DesktopWindowXamlSource_Partial.cpp, tag winui3/release/1.4.3, commit 685d2bfa86d6169aa1998a7eaa2c38bfcf9f74bc

using System;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Islands;

namespace Microsoft.UI.Xaml.Hosting;

public partial class DesktopWindowXamlSource : IDisposable
{
	public void Initialize(WindowId windowId)
	{
		// TODO Uno specific: Original code works with HWND.
		AttachToWindow(Window.GetFromAppWindow(AppWindow.GetFromWindowId(windowId)));
	}

	// internal SiteBridget SiteBridge => _contentBridgeDW;

	/// <summary>
	/// Initializes a new instance of the DesktopWindowXamlSource class.
	/// </summary>
	public DesktopWindowXamlSource()
	{
#if HAS_UNO
		Initialize();
#endif
	}

	private void Initialize()
	{
		_spXamlCore = WindowsXamlManager.InitializeForCurrentThread();

		// CheckThread();

		// This will make sure that it doesn't get cleared off thread in WeakReferenceSourceNoThreadId::OnFinalReleaseOffThread()
		// as it has thread-local variables and needs to be disposed off by the same thread
		// AddToReferenceTrackingList();

		var frameworkApplication = Application.Current;
		_spXamlIsland = frameworkApplication.CreateIslandWithContentBridge(this, null);

		// Create and configure focus navigation controller
		var focusController = _spXamlIsland.FocusController;
		_spFocusController.GotFocus += OnFocusControllerGotFocus;
		_spFocusController.LosingFocus += OnFocusControllerLosingFocus;

		XamlIsland xamlIsland = _spXamlIsland;
		FocusManager focusManager = VisualTree.GetFocusManagerForElement(xamlIsland);
		focusManager.SetCanTabOutOfPlugin(true);

		var coreXamlIsland = xamlIsland;
		coreXamlIsland.SetHasTransparentBackground(true);

		//InstrumentUsage(false); // false -> adding
		_initializedCalled = true;
	}


	/// <summary>
	/// Gets or sets the Microsoft.UI.Xaml.UIElement object that you want to host in the application.
	/// </summary>
	public UIElement Content
	{
		get => _spXamlIsland.Content;
		set
		{
			//if (value is FrameworkElement fe)
			//{
			//	auto contentCoreDO = contentAsFE.Cast<FrameworkElement>()->GetHandle();

			//	if (contentCoreDO->IsPropertyDefault(contentCoreDO->GetPropertyByIndexInline(KnownPropertyIndex::FrameworkElement_FlowDirection)) &&
			//		(GetWindowLong(m_childHwnd, GWL_EXSTYLE) & WS_EX_LAYOUTRTL))
			//	{
			//		IFC_RETURN(contentAsFE->put_FlowDirection(xaml::FlowDirection_RightToLeft));
			//	}
			//}

			_spXamlIsland.Content = value;
		}
	}

	internal XamlIsland XamlIsland => _spXamlIsland;

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	public void Dispose()
	{
		//if (m_bClosed)
		//{
		//	return S_OK;
		//}

		//IFC_RETURN(CheckThread());

		//m_bClosed = true;

		//if (m_initializedCalled)
		//{
		//	InstrumentUsage(true); // true -> removing
		//}

		//if (m_systemBackdrop.Get() != nullptr)
		//{
		//	ctl::ComPtr<DirectUI::SystemBackdrop> systemBackdrop;
		//	IFC_RETURN(m_systemBackdrop.As(&systemBackdrop));
		//	IFC_RETURN(systemBackdrop->InvokeOnTargetDisconnected(this));
		//	systemBackdrop = nullptr;
		//	m_systemBackdrop = nullptr;
		//}

		//auto island = m_spXamlIsland;

		//IFC_RETURN(ReleaseFocusController());

		if (_spXamlIsland is not null)
		{
			// Remove the Xaml content before calling Dispose on the content bridge. Disposing the content bridge also
			// closes the entire visual tree under it. If we then go to unparent visuals we'll hit RO_E_CLOSED errors
			// everywhere.
			var frameworkApplication = Application.Current;
			frameworkApplication.RemoveIsland(_spXamlIsland);

			// Turn off any frame counters (if they are on) for the same reason we remove the content.  Also inform
			// the core that we have done this in case it needs to re-evaluate whether to display on a future frame
			// (and a different island).
			//DirectUI::XamlIsland* xamlIsland = m_spXamlIsland.Cast<XamlIsland>();
			//CXamlIslandRoot* pXamlIslandCore = static_cast<CXamlIslandRoot*>(xamlIsland->GetHandle());
			//if (pXamlIslandCore->GetDCompTreeHost())
			//{
			//	IFC_RETURN(pXamlIslandCore->GetDCompTreeHost()->UpdateDebugSettings(false /* isFrameRateCounterEnabled */));
			//}
			//IFC_RETURN(pXamlIslandCore->GetContext()->OnDebugSettingsChanged());
		}

		//if (m_contentBridgeDW)
		//{
		//	ctl::ComPtr<mu::IClosableNotifier> closableNotifier;
		//	IFCFAILFAST(m_contentBridgeDW.As(&closableNotifier));
		//	IGNOREHR(closableNotifier->remove_FrameworkClosed(m_bridgeClosedToken));
		//}

		//// Dispose of the content bridge
		//if (m_contentBridge && !m_bridgeClosed)
		//{
		//	ctl::ComPtr<wf::IClosable> spClosable;
		//	IFCFAILFAST(m_contentBridge.As(&spClosable));

		//	m_contentBridge = nullptr;
		//	IFCFAILFAST(spClosable->Close());
		//}
		//m_desktopBridge = nullptr;
		//m_contentBridgeDW = nullptr;

		//// Signal to the interop tool of the closure after the XamlIsland has been removed. This way
		//// the RuntimeObjectCache stays connected.
		//if (auto interop = Diagnostics::GetDiagnosticsInterop(false))
		//  {
		//	interop->SignalRootMutation(ctl::iinspectable_cast(this), VisualMutationType::Remove);
		//}

		//if (m_spXamlIsland.Get() != nullptr)
		//{
		//	ctl::ComPtr<XamlIsland> xamlIsland;
		//	IFC_RETURN(m_spXamlIsland.As(&xamlIsland));
		//	CXamlIslandRoot* pXamlIslandCore = static_cast<CXamlIslandRoot*>(xamlIsland->GetHandle());
		//	pXamlIslandCore->Dispose();
		//	m_spXamlIsland = nullptr;
		//}

		//if (m_spXamlCore.Get() != nullptr)
		//{
		//	ctl::ComPtr<DesktopWindowXamlSource> spThis(this); // Avoid deleting this
		//	ctl::ComPtr<wf::IClosable> spClosable;

		//	IFC_RETURN(m_spXamlCore.As(&spClosable));
		//	IFC_RETURN(spClosable->Close());
		//	spClosable = nullptr;
		//	m_spXamlCore = nullptr;
		//}

		//return S_OK;
	}
}
