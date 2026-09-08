// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls/dev/WebView2/WebView2.h, commit 3c9c168844f06c6ac000a97977f0bb3f4c90fd75

#nullable enable

using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Uno.Disposables;
using Uno.UI.Xaml.Controls;
using Windows.UI.ViewManagement;

namespace Microsoft.UI.Xaml.Controls;

partial class WebView2
{
	private struct XamlFocusChangeInfo
	{
		internal CoreWebView2MoveFocusReason m_storedMoveFocusReason;

		// Reason gets set in GettingFocus handler, but the focus change can still be cancelled or redirected
		internal bool m_isPending;
	}

#pragma warning disable CS0414 // Retain the state fields from WebView2.h.
	private bool m_isExplicitEnvironment;
	private bool m_isExplicitControllerOptions;
#pragma warning restore CS0414
	private CoreWebView2Environment? m_coreWebViewEnvironment;
#if HAS_UNO
	// The native controller is a projection adapter, not a MUX implementation.
	private INativeWebViewController? m_coreWebViewController;
#endif
	private CoreWebView2ControllerOptions? m_customCoreWebViewControllerOptions;
	private CoreWebView2? m_coreWebView;
	private string m_stopNavigateOnUriChanged = string.Empty;
	private bool m_canGoPropSetInternally;

	private readonly SerialDisposable m_gettingFocusRevoker = new();
	private readonly SerialDisposable m_gotFocusRevoker = new();
	private readonly SerialDisposable m_losingFocusRevoker = new();
	private readonly SerialDisposable m_loadedRevoker = new();
	private readonly SerialDisposable m_unloadedRevoker = new();
	private readonly SerialDisposable m_sizeChangedRevoker = new();
	private readonly SerialDisposable m_renderedRevoker = new();
	private readonly SerialDisposable m_xamlRootChangedRevoker = new();
	private long m_manipulationModeChangedToken;
	private long m_visibilityChangedToken;

	private readonly SerialDisposable m_coreNavigationStartingRevoker = new();
	private readonly SerialDisposable m_coreSourceChangedRevoker = new();
	private readonly SerialDisposable m_coreNavigationCompletedRevoker = new();
	private readonly SerialDisposable m_coreWebMessageReceivedRevoker = new();
	private readonly SerialDisposable m_coreProcessFailedRevoker = new();
	private readonly SerialDisposable m_coreMoveFocusRequestedRevoker = new();
	private AccessibilitySettings? m_accessibilitySettings;
	private readonly SerialDisposable m_textScaleChangedRevoker = new();

	private XamlFocusChangeInfo m_xamlFocusChangeInfo;
	private bool m_isVisible;
	private bool m_isHostVisible;
	private bool m_shouldShowMissingAnaheimWarning;
	private bool m_isCoreFailure_BrowserExited_State; // True after Edge ProcessFailed event w/ CORE_WEBVIEW2_PROCESS_FAILED_KIND_BROWSER_PROCESS_EXITED
	private bool m_isClosed; // True after WebView2::Close() has been called - no core objects can be created
	private bool m_isImplicitCreationInProgress; // True while we are creating CWV2 due to Source property being set
	private bool m_isExplicitCreationInProgress; // True while we are creating CWV2 due to EnsureCoreWebView2*Async() being called
	private TaskCompletionSource? m_creationInProgressAsync; // Awaitable object for any currently active creation. There should be only one active operation at a time.
	private float m_rasterizationScale;
	private UISettings? m_uiSettings;
}
// Retained native header follows.
#if !HAS_UNO
	// TODO Uno: Original native declarations, inline event args, and unsupported COM fields are retained without inventing managed equivalents.
	// // Copyright (c) Microsoft Corporation. All rights reserved.
	// // Licensed under the MIT License. See LICENSE in the project root for license information.
	//
	// #pragma once
	//
	// #include "pch.h"
	// #include "common.h"
	//
	// #include "CoreWebView2InitializedEventArgs.g.h"
	//
	// #include "DispatcherHelper.h"
	//
	// #include <dcomp.h>
	// #include <CppWinrtContentExternalOutputLinkHelper.h>
	// #include <Microsoft.UI.Content.h>
	//
	// #include "WebView2.g.h"
	// #include "WebView2.properties.h"
	// #pragma warning( push )
	// #pragma warning( disable : 28251)   // warning C28251: Inconsistent annotation for function: this instance has an error
	// #include <webview2.h>
	// #include <WebView2EnvironmentOptions.h>
	// #pragma warning( pop )
	// #include <WebView2Interop.h>
	//
	// MIDL_INTERFACE("2EE738F4-402C-4F6E-9F93-DEDD89F09B49")
	// IHwndComponentHost : public IUnknown
	// {
	// public:
	//     virtual HWND STDMETHODCALLTYPE GetComponentHwnd() = 0;
	// };
	//
	// // for making async/await possible
	// struct AsyncWebViewOperations final : public Awaitable
	// {
	//     AsyncWebViewOperations() = default;
	//
	//     void StopWaiting()
	//     {
	//         // async operation completed, stop all awaits
	//         CompleteAwaits();
	//     }
	// };
	//
	// class CoreWebView2InitializedEventArgs :
	//     public winrt::implementation::CoreWebView2InitializedEventArgsT<CoreWebView2InitializedEventArgs>
	// {
	// public:
	//     CoreWebView2InitializedEventArgs(winrt::hresult Exception) : m_exception(Exception) {};
	//
	//     void Exception(winrt::hresult exception) { m_exception = exception; };
	//     winrt::hresult Exception() { return m_exception; }
	// private:
	//     winrt::hresult m_exception{};
	// };
	//
	// // Derive from DeriveFromPanelHelper_base so that we get access to Children collection in Panel.
	// // (In case we have to show the "Missing Anaheim Warning").
	// class WebView2 :
	//     public ReferenceTracker<WebView2, DeriveFromPanelHelper_base, winrt::WebView2, winrt::IWebView22, IHwndComponentHost>,
	//     public WebView2Properties
	// {
	//
	// public:
	//
	//     WebView2();
	//     virtual ~WebView2();
	//
	//     // IUIElement
	//     winrt::AutomationPeer OnCreateAutomationPeer();
	//
	//     // FrameworkElement overrides
	//     winrt::Size MeasureOverride(winrt::Size const& availableSize);
	//     winrt::Size ArrangeOverride(winrt::Size const& finalSize);
	//
	//     winrt::IUnknown GetWebView2Provider();
	//     winrt::IUnknown GetProviderForHwnd(HWND hwnd);
	//
	//     void OnPropertyChanged(const winrt::DependencyPropertyChangedEventArgs& args);
	//     void OnLoaded(winrt::IInspectable const& sender, winrt::RoutedEventArgs const& args);
	//     void OnUnloaded(winrt::IInspectable const& sender, winrt::RoutedEventArgs const& args);
	//
	//     void UpdateCoreWebViewScale();
	//
	//     winrt::IAsyncAction EnsureCoreWebView2Async();
	//     winrt::IAsyncAction EnsureCoreWebView2Async(winrt::CoreWebView2Environment environment);
	//     winrt::IAsyncAction EnsureCoreWebView2Async(winrt::CoreWebView2Environment environment, winrt::CoreWebView2ControllerOptions controllerOptions);
	//     winrt::IAsyncOperation<winrt::hstring> ExecuteScriptAsync(winrt::hstring javascriptCode);
	//     void Reload();
	//     void NavigateToString(winrt::hstring htmlContent);
	//     void GoBack();
	//     void GoForward();
	//     void Close();
	//
	//     winrt::CoreWebView2 CoreWebView2();     // Getter for CoreWebView2 property (read-only)
	//
	//     // Default value on public DP - ok to be public
	//     static const winrt::Color sc_controllerDefaultBackgroundColor;
	//
	// private:
	//     bool ShouldNavigate(const winrt::Uri& uri);
	//     winrt::IAsyncAction OnSourceChanged(winrt::Uri providedUri);
	//     void OnXamlPointerMessage(UINT message, const winrt::PointerRoutedEventArgs& args) noexcept;
	//     void FillPointerPenInfo(const winrt::PointerPoint& inputPt, winrt::CoreWebView2PointerInfo outputPt);
	//     void FillPointerTouchInfo(const winrt::PointerPoint& inputPt, winrt::CoreWebView2PointerInfo outputPt);
	//     void FillPointerInfo(const winrt::PointerPoint& inputPt, winrt::CoreWebView2PointerInfo outputPt, const winrt::PointerRoutedEventArgs& args);
	//     uint32_t GetPointerFlags(const winrt::PointerPoint& inputPt);
	//     winrt::Rect ScaleRectToPhysicalPixels(winrt::Rect inputRect);
	//     winrt::Point ScalePointToPhysicalPixels(winrt::Point inputPoint);
	//
	//     void ResetMouseInputState();
	//     void OnManipulationModePropertyChanged(const winrt::DependencyObject& /*sender*/, const winrt::DependencyProperty& /*args*/);
	//     void OnVisibilityPropertyChanged(const winrt::DependencyObject& /*sender*/, const winrt::DependencyProperty& /*args*/);
	//
	//     void FireNavigationStarting(const winrt::CoreWebView2NavigationStartingEventArgs& args);
	//     void FireNavigationCompleted(const winrt::CoreWebView2NavigationCompletedEventArgs& args);
	//     void FireWebMessageReceived(const winrt::CoreWebView2WebMessageReceivedEventArgs& args);
	//     void UpdateSourceInternal();
	//     void FireCoreProcessFailedEvent(const winrt::CoreWebView2ProcessFailedEventArgs& args);
	//     void FireCoreWebView2Initialized(winrt::hresult exception);
	//     void MoveFocusIntoCoreWebView2(winrt::CoreWebView2MoveFocusReason reason);
	//     void UpdateDefaultVisualBackgroundColor();
	//
	//     HWND GetHostHwnd() noexcept;
	//     HWND EnsureTemporaryHostHwnd();
	//     void UpdateParentWindow(HWND newParentWindow);
	//
	//     winrt::IAsyncAction CreateCoreObjects(winrt::CoreWebView2Environment environment = nullptr, winrt::CoreWebView2ControllerOptions controllerOptions = nullptr);
	//     winrt::IAsyncOperation<winrt::CoreWebView2Environment> CreateDefaultCoreEnvironment() noexcept;
	//     winrt::IAsyncAction CreateCoreWebViewFromEnvironment(HWND hwndParent, winrt::CoreWebView2ControllerOptions controllerOptions = nullptr);
	//     void CreateMissingAnaheimWarning();
	//
	//     void RegisterXamlEventHandlers();
	//     void UnregisterXamlEventHandlers();
	//     void RegisterCoreEventHandlers();
	//     void UnregisterCoreEventHandlers();
	//     void RegisterDragStartingHandler() noexcept;
	//     void UnRegisterDragStartingHandler() noexcept;
	//     com_ptr<ICoreWebView2CompositionControllerInterop3> GetDragStartingInterop();
	//     void DragStartingCallback(ICoreWebView2DragStartingEventArgs* args) noexcept;
	//     void LogTelemetryDragStartingFailure(HRESULT hr, DWORD contentType) const;
	//
	//     void HandlePointerPressed(const winrt::Windows::Foundation::IInspectable&, const winrt::PointerRoutedEventArgs& args);
	//     void HandlePointerReleased(const winrt::Windows::Foundation::IInspectable&, const winrt::PointerRoutedEventArgs& args);
	//     void HandlePointerMoved(const winrt::Windows::Foundation::IInspectable&, const winrt::PointerRoutedEventArgs& args);
	//     void HandlePointerWheelChanged(const winrt::Windows::Foundation::IInspectable&, const winrt::PointerRoutedEventArgs& args);
	//     void HandlePointerExited(const winrt::Windows::Foundation::IInspectable&, const winrt::PointerRoutedEventArgs& args);
	//     void HandlePointerEntered(const winrt::Windows::Foundation::IInspectable&, const winrt::PointerRoutedEventArgs& args);
	//     void HandlePointerCanceled(const winrt::Windows::Foundation::IInspectable&, const winrt::PointerRoutedEventArgs& args);
	//     void HandlePointerCaptureLost(const winrt::Windows::Foundation::IInspectable&, const winrt::PointerRoutedEventArgs& args);
	//     void HandleGettingFocus(const winrt::Windows::Foundation::IInspectable&, const winrt::GettingFocusEventArgs& args) noexcept;
	//     void HandleGotFocus(const winrt::Windows::Foundation::IInspectable&, const winrt::RoutedEventArgs&) noexcept;
	//     void HandleLosingFocus(const winrt::Windows::Foundation::IInspectable&, const winrt::RoutedEventArgs&) noexcept;
	//     void HandleXamlRootChanged();
	//     void HandleSizeChanged(const winrt::IInspectable& /*sender*/, const winrt::SizeChangedEventArgs& args);
	//     void HandleRendered(const winrt::IInspectable& /*sender*/, const winrt::IInspectable& /*args*/);
	//
	//     void XamlRootChangedHelper(bool forceUpdate);
	//     void TryCompleteInitialization();
	//     void DisconnectFromRootVisualTarget();
	//     void CreateAndSetVisual();
	//
	//     void CheckAndUpdateWebViewPosition();
	//     void CheckAndUpdateWindowPosition();
	//     void CheckAndUpdateVisibility(bool force = false);
	//     void UpdateCoreWebViewVisibility();
	//     bool AreAllAncestorsVisible();
	//     void SetCoreWebViewAndVisualSize(const float width, const float height);
	//     void UpdateRenderedSubscriptionAndVisibility();
	//
	//     void SetControllerDefaultBackgroundColor();
	//     void SetCanGoForward(bool canGoForward);
	//     void SetCanGoBack(bool canGoBack);
	//     void ResetProperties();
	//     void CloseInternal(bool inShutdownPath);
	//
	//     winrt::AccessibilitySettings GetAccessibilitySettings();
	//     winrt::Color GetThemeBackgroundColor();
	//
	//     // IHwndComponentHost
	//     HWND STDMETHODCALLTYPE GetComponentHwnd();
	//
	//     winrt::UISettings GetUISettings();
	//
	//     winrt::DataPackageOperation DataPackageOperationFromDropEffect(DWORD dropEffect);
	//     void ConvertXamlArgsToOle32Args(const winrt::DragEventArgs& args, DWORD& keyState, POINT& cursorPosition, DWORD& dropEffect, IDataObject** dataObject);
	//     void OnDragEnter(const winrt::IInspectable& sender, const winrt::DragEventArgs& args);
	//     void OnDragOver(const winrt::IInspectable& sender, const winrt::DragEventArgs& args);
	//     void OnDragLeave(const winrt::IInspectable& sender, const winrt::DragEventArgs& args);
	//     void OnDrop(const winrt::IInspectable& sender, const winrt::DragEventArgs& args);
	//
	//     // Helpers to gracefully handle ERROR_INVALID_STATE from CWV2 objects that have been internally closed
	//     // (In particular, this occurs when host window is closed via 'x' button and CWV2 processes closure before WebView2)
	//     bool CoreWebView2HasInvalidState();
	//     template <typename Delegate> void CoreWebView2RunIgnoreInvalidStateSync(Delegate delegate);
	//
	// private:
	//     struct XamlFocusChangeInfo
	//     {
	//         winrt::CoreWebView2MoveFocusReason m_storedMoveFocusReason{};
	//
	//         // Reason gets set in GettingFocus handler, but the focus change can still be cancelled or redirected
	//         bool m_isPending{};
	//     };
	//
	//     bool m_isExplicitEnvironment = false;
	//     bool m_isExplicitControllerOptions = false;
	//     winrt::CoreWebView2Environment m_coreWebViewEnvironment{ nullptr };
	//     winrt::CoreWebView2Controller m_coreWebViewController{ nullptr };
	//     winrt::CoreWebView2ControllerOptions m_customCoreWebViewControllerOptions{ nullptr };
	//     winrt::CoreWebView2CompositionController m_coreWebViewCompositionController{ nullptr };
	//     winrt::CoreWebView2 m_coreWebView{ nullptr };
	//
	//     bool m_isLeftMouseButtonPressed{};
	//     bool m_isMiddleMouseButtonPressed{};
	//     bool m_isRightMouseButtonPressed{};
	//     bool m_isXButton1Pressed{};
	//     bool m_isXButton2Pressed{};
	//     bool m_isMouseLeaveNeeded{};
	//     bool m_hasMouseCapture{};
	//     std::map<int32_t, bool> m_hasTouchCapture{};
	//     bool m_hasPenCapture{};
	//     HWND m_xamlHostHwnd{ nullptr };
	//     HWND m_tempHostHwnd{ nullptr };
	//     HWND m_inputWindowHwnd{ nullptr };
	//     winrt::hstring m_stopNavigateOnUriChanged{};
	//     bool m_canGoPropSetInternally{};
	//     ContentExternalLinkHelper::OutputLink m_systemVisualBridge;
	//
	//     winrt::UIElement::GettingFocus_revoker m_gettingFocusRevoker;
	//     winrt::UIElement::GotFocus_revoker m_gotFocusRevoker;
	//     winrt::UIElement::LosingFocus_revoker m_losingFocusRevoker;
	//     winrt::UIElement::PointerPressed_revoker m_pointerPressedRevoker;
	//     winrt::UIElement::PointerReleased_revoker m_pointerReleasedRevoker;
	//     winrt::UIElement::PointerMoved_revoker m_pointerMovedRevoker;
	//     winrt::UIElement::PointerWheelChanged_revoker m_pointerWheelChangedRevoker;
	//     winrt::UIElement::PointerExited_revoker m_pointerExitedRevoker;
	//     winrt::UIElement::PointerEntered_revoker m_pointerEnteredRevoker;
	//     winrt::UIElement::PointerCanceled_revoker m_pointerCanceledRevoker;
	//     winrt::UIElement::PointerCaptureLost_revoker m_pointerCaptureLostRevoker;
	//     winrt::UIElement::KeyDown_revoker m_keyDownRevoker;
	//     winrt::CoreDispatcher::AcceleratorKeyActivated_revoker m_acceleratorKeyActivatedRevoker;
	//     winrt::FrameworkElement::Loaded_revoker m_loadedRevoker{};
	//     winrt::FrameworkElement::Unloaded_revoker m_unloadedRevoker{};
	//     winrt::FrameworkElement::SizeChanged_revoker m_sizeChangedRevoker;
	//     winrt::Microsoft::UI::Xaml::Media::CompositionTarget::Rendered_revoker m_renderedRevoker{};
	//     winrt::XamlRoot::Changed_revoker m_xamlRootChangedRevoker{};
	//     winrt::event_token m_manipulationModeChangedToken{};
	//     winrt::event_token m_visibilityChangedToken{};
	//
	//     winrt::CoreWebView2::NavigationStarting_revoker m_coreNavigationStartingRevoker;
	//     winrt::CoreWebView2::SourceChanged_revoker m_coreSourceChangedRevoker;
	//     winrt::CoreWebView2::NavigationCompleted_revoker m_coreNavigationCompletedRevoker;
	//     winrt::CoreWebView2::WebMessageReceived_revoker m_coreWebMessageReceivedRevoker;
	//     winrt::CoreWebView2::ProcessFailed_revoker m_coreProcessFailedRevoker;
	//     winrt::CoreWebView2Controller::MoveFocusRequested_revoker m_coreMoveFocusRequestedRevoker;
	//     winrt::CoreWebView2CompositionController::CursorChanged_revoker m_cursorChangedRevoker;
	//
	//     winrt::FrameworkElement::ActualThemeChanged_revoker m_actualThemeChangedRevoker{};
	//     winrt::AccessibilitySettings m_accessibilitySettings;
	//     winrt::AccessibilitySettings::HighContrastChanged_revoker m_highContrastChangedRevoker{};
	//     winrt::UISettings::TextScaleFactorChanged_revoker m_textScaleChangedRevoker{};
	//
	//     winrt::UIElement::DragEnter_revoker m_dragEnterRevoker{};
	//     winrt::UIElement::DragOver_revoker m_dragOverRevoker{};
	//     winrt::UIElement::DragLeave_revoker m_dragLeaveRevoker{};
	//     winrt::UIElement::Drop_revoker m_dropRevoker{};
	//     Microsoft::WRL::ComPtr<ICoreWebView2DragStartingEventHandler> m_dragStartingHandler;
	//     EventRegistrationToken m_dragStartingToken{};
	//     winrt::Microsoft::UI::Input::PointerPoint m_lastPointerPoint{ nullptr };
	//
	//     void OnAppWindowPositionChanged(const winrt::Microsoft::UI::Windowing::AppWindow& sender, const winrt::Microsoft::UI::Windowing::AppWindowChangedEventArgs& args);
	//     winrt::event_token m_appWindowPositionChangedToken{};
	//     winrt::Microsoft::UI::Windowing::AppWindow m_appWindow{ nullptr };
	//
	//     void ResetPointerHelper(const winrt::PointerRoutedEventArgs& args);
	//     void SendMouseLeave();
	//     XamlFocusChangeInfo m_xamlFocusChangeInfo{};
	//
	//     bool m_isVisible{};
	//     bool m_isHostVisible{};
	//     bool m_shouldShowMissingAnaheimWarning{};
	//
	//     bool m_isCoreFailure_BrowserExited_State{};    // True after Edge ProcessFailed event w/ CORE_WEBVIEW2_PROCESS_FAILED_KIND_BROWSER_PROCESS_EXITED
	//     bool m_isClosed{};    // True after WebView2::Close() has been called - no core objects can be created
	//
	//     bool m_isImplicitCreationInProgress{};    // True while we are creating CWV2 due to Source property being set
	//     bool m_isExplicitCreationInProgress{};    // True while we are creating CWV2 due to EnsureCoreWebView2*Async() being called
	//     std::unique_ptr<AsyncWebViewOperations> m_creationInProgressAsync{ nullptr }; // Awaitable object for any currently active creation. There should be only one active operation at a time.
	//
	//     float m_rasterizationScale{};
	//     // The last known WebView rect position, scaled for DPI
	//     winrt::Point m_webViewScaledPosition{};
	//     // Last known WebView rect size, scaled for DPI
	//     winrt::Point m_webViewScaledSize{};
	//     // Last known position of the host window
	//     POINT m_hostWindowPosition{};
	//
	//     DispatcherHelper m_dispatcherHelper{ *this };
	//
	//     winrt::UISettings m_uiSettings;
	// };
#endif
