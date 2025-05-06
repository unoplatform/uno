// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
#nullable enable



using System;
using DirectUI;
using Uno.Disposables;
using Uno.UI;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.Extensions;
using static Microsoft.UI.Xaml.Controls._Tracing;
using Uno.UI.Xaml.Input;
using Popup = Microsoft.UI.Xaml.Controls.Primitives.Popup;
using Windows.System;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation;
using Uno.UI.Controls;
using Uno.UI.Xaml.Core;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

using Microsoft.UI.Input;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class AppBar : ContentControl
	{


		private void UnregisterEvents()
		{
			m_contentRootSizeChangedEventHandler.Disposable = null;
			m_expandButtonClickEventHandler.Disposable = null;
			m_displayModeStateChangedEventHandler.Disposable = null;
			m_overlayElementPointerPressedEventHandler.Disposable = null;

			m_tpLayoutRoot = null;
			m_tpContentRoot = null;
			m_tpExpandButton = null;
			m_tpDisplayModesStateGroupRef = null;

			m_overlayClosingStoryboard = null;
			m_overlayOpeningStoryboard = null;
		}

		private protected override void OnUnloaded()
		{
			LayoutUpdated -= OnLayoutUpdated;
			SizeChanged -= OnSizeChanged;
			if (m_isInOverlayState)
			{
				TeardownOverlayState();
			}

			UnregisterEvents();

			base.OnUnloaded();

		}

		

#if HAS_NATIVE_COMMANDBAR
		bool ICustomClippingElement.AllowClippingToLayoutSlot => !_isNativeTemplate;
		bool ICustomClippingElement.ForceClippingToLayoutSlot => false;
#endif
		
		

		

		protected virtual void GetVerticalOffsetNeededToOpenUp(out double neededOffset, out bool opensWindowed)
		{
			double verticalDelta = 0d;
			var templateSettings = TemplateSettings;

			var closedDisplayMode = ClosedDisplayMode;

			verticalDelta = closedDisplayMode switch
			{
				AppBarClosedDisplayMode.Compact => templateSettings.CompactVerticalDelta,
				AppBarClosedDisplayMode.Minimal => templateSettings.MinimalVerticalDelta,
				_ => templateSettings.HiddenVerticalDelta,
			};

			neededOffset = -verticalDelta;
			opensWindowed = false;
		}

		private void SetupOverlayState()
		{
			MUX_ASSERT(m_Mode == AppBarMode.Inline);
			MUX_ASSERT(!m_isInOverlayState);
			// The approach used to achieve light-dismiss is to create a 1x1 element that is added
			// as the first child of our layout root panel.  Adding it as the first child ensures that
			// it is below our actual content and will therefore not affect the content area's hit-testing.
			// We then use a scale transform to scale up an LTE targeted to the element to match the
			// dimensions of our window.  Finally, we translate that same LTE to make sure it's upper-left
			// corner aligns with the window's upper left corner, causing it to cover the entire window.
			// A pointer pressed handler is attached to the element to intercept any pointer
			// input that is not directed at the actual content.  The value of AppBar.IsSticky controls
			// whether the light-dismiss element is hit-testable (IsSticky=True -> hit-testable=False).
			// The pointer pressed handler simply closes the appbar and marks the routed event args
			// message as handled.
			if (m_tpLayoutRoot is { })
			{
				// Create our overlay element if necessary.
				if (m_overlayElement == null)
				{
					var rectangle = new Rectangle();
					rectangle.Width = 1;
					rectangle.Height = 1;
					rectangle.UseLayoutRounding = false;

					var isSticky = IsSticky;
					rectangle.IsHitTestVisible = !isSticky;

					rectangle.PointerPressed += OnOverlayElementPointerPressed;
					m_overlayElementPointerPressedEventHandler.Disposable = Disposable.Create(() => rectangle.PointerPressed -= OnOverlayElementPointerPressed);

					m_overlayElement = rectangle;

					UpdateOverlayElementBrush();
				}

				// Add our overlay element to our layout root panel.
				m_tpLayoutRoot.Children.Insert(0, m_overlayElement);
			}

			//CreateLTEs();

			// Update the animations to target the newly created overlay element LTE.
			if (m_isOverlayVisible)
			{
				UpdateTargetForOverlayAnimations();
			}

			m_isInOverlayState = true;
		}

		private void TeardownOverlayState()
		{
			MUX_ASSERT(m_Mode == AppBarMode.Inline);
			MUX_ASSERT(m_isInOverlayState);

			//DestroyLTEs();

			// Remove our light-dismiss element from our layout root panel.
			if (m_tpLayoutRoot is { })
			{
				var indexOfOverlayElement = m_tpLayoutRoot.Children.IndexOf(m_overlayElement);
				MUX_ASSERT(indexOfOverlayElement != -1);

				if (indexOfOverlayElement != -1)
				{
					m_tpLayoutRoot.Children.RemoveAt(indexOfOverlayElement);
				}
			}

			m_isInOverlayState = false;
		}

		//AppBar::CreateLTEs()
		//{
		//	ASSERT(!m_layoutTransitionElement);
		//		ASSERT(!m_overlayLayoutTransitionElement);
		//		ASSERT(!m_parentElementForLTEs);

		//	// If we're under the PopupRoot or FullWindowMediaRoot, then we'll explicitly set
		//	// our LTE's parent to make sure the LTE doesn't get placed under the TransitionRoot,
		//	// which is lower in z-order than these other roots.
		//	if (ShouldUseParentedLTE())
		//	{
		//		ctl::ComPtr<xaml::IDependencyObject> parent;
		//		IFC_RETURN(VisualTreeHelper::GetParentStatic(this, &parent));
		//		IFCEXPECT_RETURN(parent);

		//		IFC_RETURN(SetPtrValueWithQI(m_parentElementForLTEs, parent.Get()));
		//	}

		//	if (m_overlayElement)
		//	{
		//		// Create an LTE for our overlay element.
		//IFC_RETURN(LayoutTransitionElement_Create(
		//	DXamlCore::GetCurrent()->GetHandle(),
		//	m_overlayElement.Cast<FrameworkElement>()->GetHandle(),
		//	m_parentElementForLTEs? m_parentElementForLTEs.Cast<UIElement>()->GetHandle() : nullptr,
		//	false /*isAbsolutelyPositioned*/,
		//	m_overlayLayoutTransitionElement.ReleaseAndGetAddressOf()
		//));

		//		// Configure the overlay LTE.
		//		{
		//			ctl::ComPtr<DependencyObject> overlayLTEPeer;
		//	IFC_RETURN(DXamlCore::GetCurrent()->GetPeer(m_overlayLayoutTransitionElement.get(), &overlayLTEPeer));

		//			wf::Rect windowBounds = { };
		//	IFC_RETURN(DXamlCore::GetCurrent()->GetContentBoundsForElement(GetHandle(), &windowBounds));

		//			ctl::ComPtr<CompositeTransform> compositeTransform;
		//	IFC_RETURN(ctl::make(&compositeTransform));

		//			IFC_RETURN(compositeTransform->put_ScaleX(windowBounds.Width));
		//	IFC_RETURN(compositeTransform->put_ScaleY(windowBounds.Height));

		//	IFC_RETURN(overlayLTEPeer.Cast<UIElement>()->put_RenderTransform(compositeTransform.Get()));

		//			ctl::ComPtr<xaml_media::IGeneralTransform> transformToVisual;
		//	IFC_RETURN(m_overlayElement.Cast<FrameworkElement>()->TransformToVisual(nullptr, &transformToVisual));

		//			wf::Point offsetFromRoot = { };
		//	IFC_RETURN(transformToVisual->TransformPoint({ 0, 0 }, &offsetFromRoot));

		//			auto flowDirection = xaml::FlowDirection_LeftToRight;
		//	IFC_RETURN(get_FlowDirection(&flowDirection));

		//			// Translate the light-dismiss layer so that it is positioned at the top-left corner of the window (for LTR cases)
		//			// or the top-right corner of the window (for RTL cases).
		//			// TransformToVisual(nullptr) will return an offset relative to the top-left corner of the window regardless of
		//			// flow direction, so for RTL cases subtract the window width from the returned offset.x value to make it relative
		//			// to the right edge of the window.
		//			IFC_RETURN(compositeTransform->put_TranslateX(flowDirection == xaml::FlowDirection_LeftToRight? -offsetFromRoot.X : offsetFromRoot.X - windowBounds.Width));
		//			IFC_RETURN(compositeTransform->put_TranslateY(-offsetFromRoot.Y));
		//		}
		//	}

		//	IFC_RETURN(LayoutTransitionElement_Create(
		//		DXamlCore::GetCurrent()->GetHandle(),
		//		GetHandle(),
		//		m_parentElementForLTEs ? m_parentElementForLTEs.Cast<UIElement>()->GetHandle() : nullptr,
		//		false /*isAbsolutelyPositioned*/,
		//		m_layoutTransitionElement.ReleaseAndGetAddressOf()
		//	));

		//// Forward our control's opacity to the LTE since it doesn't happen automatically.
		//{
		//	double opacity = 0.0;
		//	IFC_RETURN(get_Opacity(&opacity));
		//	IFC_RETURN(m_layoutTransitionElement->SetValueByKnownIndex(KnownPropertyIndex::UIElement_Opacity, static_cast<float>(opacity)));
		//}

		//IFC_RETURN(PositionLTEs());

		//return S_OK;
		//}

		//_Check_return_ HRESULT
		//AppBar::PositionLTEs()
		//{
		//	ASSERT(m_layoutTransitionElement);

		//	ctl::ComPtr<xaml::IDependencyObject> parentDO;
		//	ctl::ComPtr<xaml::IUIElement> parent;

		//	IFC_RETURN(VisualTreeHelper::GetParentStatic(this, &parentDO));

		//	// If we don't have a parent, then there's nothing for us to do.
		//	if (parentDO)
		//	{
		//		IFC_RETURN(parentDO.As(&parent));

		//		ctl::ComPtr<xaml_media::IGeneralTransform> transform;
		//		IFC_RETURN(TransformToVisual(parent.Cast<UIElement>(), &transform));

		//		wf::Point offset = { };
		//		IFC_RETURN(transform->TransformPoint({ 0, 0 }, &offset));

		//		IFC_RETURN(LayoutTransitionElement_SetDestinationOffset(m_layoutTransitionElement, offset.X, offset.Y));
		//	}

		//	return S_OK;
		//}

		//_Check_return_ HRESULT
		//AppBar::DestroyLTEs()
		//{
		//	if (m_layoutTransitionElement)
		//	{
		//		IFC_RETURN(LayoutTransitionElement_Destroy(
		//			DXamlCore::GetCurrent()->GetHandle(),
		//			GetHandle(),
		//			m_parentElementForLTEs ? m_parentElementForLTEs.Cast<UIElement>()->GetHandle() : nullptr,
		//			m_layoutTransitionElement.get()
		//		));

		//		m_layoutTransitionElement.reset();
		//	}

		//	if (m_overlayLayoutTransitionElement)
		//	{
		//		// Destroy our light-dismiss element's LTE.
		//		IFC_RETURN(LayoutTransitionElement_Destroy(
		//			DXamlCore::GetCurrent()->GetHandle(),
		//			m_overlayElement.Cast<FrameworkElement>()->GetHandle(),
		//			m_parentElementForLTEs ? m_parentElementForLTEs.Cast<UIElement>()->GetHandle() : nullptr,
		//			m_overlayLayoutTransitionElement.get()
		//			));

		//		m_overlayLayoutTransitionElement.reset();
		//	}

		//	m_parentElementForLTEs.Clear();

		//	return S_OK;
		//}


		private void OnOverlayElementPointerPressed(object sender, PointerRoutedEventArgs e)
		{
			MUX_ASSERT(m_Mode == AppBarMode.Inline);

			TryDismissInlineAppBar();
			e.Handled = true;
		}

		private void TryQueryDisplayModesStatesGroup()
		{
			if (m_tpDisplayModesStateGroupRef == null)
			{
				GetTemplatePart<VisualStateGroup>("DisplayModeStates", out var displayModesStateGroup);

				m_tpDisplayModesStateGroupRef?.SetTarget(displayModesStateGroup);

				VisualStateGroup? group = null;
				if (m_tpDisplayModesStateGroupRef?.TryGetTarget(out group) ?? false)
				{
					if (group != null)
					{
						group.CurrentStateChanged += OnDisplayModesStateChanged;
						m_displayModeStateChangedEventHandler.Disposable = Disposable.Create(() => group.CurrentStateChanged -= OnDisplayModesStateChanged);
					}
				}
			}
		}

		//_Check_return_ HRESULT
		//AppBar::OnBackButtonPressedImpl(_Out_ BOOLEAN* pHandled)
		//{

		//	BOOLEAN isOpen = FALSE;
		//		BOOLEAN isSticky = FALSE;

		//		IFCPTR_RETURN(pHandled);

		//		IFC_RETURN(get_IsOpen(&isOpen));
		//	IFC_RETURN(get_IsSticky(&isSticky));
		//	if (isOpen && !isSticky)
		//	{
		//		IFC_RETURN(put_IsOpen(FALSE));
		//		*pHandled = TRUE;

		//		if (m_Mode != AppBarMode_Inline)
		//		{
		//			ctl::ComPtr<IApplicationBarService> spApplicationBarService;
		//		IFC_RETURN(DXamlCore::GetCurrent()->GetApplicationBarService(spApplicationBarService));
		//		IFC_RETURN(spApplicationBarService->CloseAllNonStickyAppBars());
		//	}
		//}

		//return S_OK;
		//}

		private void ReevaluateIsOverlayVisible()
		{
			bool isOverlayVisible = false;
			var overlayMode = LightDismissOverlayMode;

			if (overlayMode == LightDismissOverlayMode.Auto)
			{
				isOverlayVisible = SharedHelpers.IsOnXbox();
			}
			else
			{
				isOverlayVisible = overlayMode == LightDismissOverlayMode.On;
			}

			// Only inline app bars can enable their overlays.  Top/Bottom/Floating will use
			// the overlay from the ApplicationBarService.
			isOverlayVisible = isOverlayVisible && (m_Mode == AppBarMode.Inline);

			if (isOverlayVisible != m_isOverlayVisible)
			{
				m_isOverlayVisible = isOverlayVisible;

				if (m_isOverlayVisible)
				{
					if (m_isInOverlayState)
					{
						UpdateTargetForOverlayAnimations();
					}
				}
				else
				{
					// Make sure we've stopped our animations.
					if (m_overlayOpeningStoryboard is { })
					{
						m_overlayOpeningStoryboard.Stop();
					}

					if (m_overlayClosingStoryboard is { })
					{
						m_overlayClosingStoryboard.Stop();
					}
				}

				if (m_overlayElement is { })
				{
					UpdateOverlayElementBrush();
				}
			}
		}

		private void UpdateOverlayElementBrush()
		{
			MUX_ASSERT(m_overlayElement is { });

			if (m_isOverlayVisible)
			{
				// Create a theme resource for the overlay brush.
				//auto core = DXamlCore::GetCurrent()->GetHandle();
				//auto dictionary = core->GetThemeResources();

				//xstring_ptr themeBrush;
				//IFC_RETURN(xstring_ptr::CloneBuffer(L"AppBarLightDismissOverlayBackground", &themeBrush));

				//CDependencyObject* initialValueNoRef = nullptr;
				//IFC_RETURN(dictionary->GetKeyNoRef(themeBrush, &initialValueNoRef));

				//CREATEPARAMETERS cp(core);
				//xref_ptr<CThemeResourceExtension> themeResourceExtension;
				//IFC_RETURN(CThemeResourceExtension::Create(
				//	reinterpret_cast<CDependencyObject**>(themeResourceExtension.ReleaseAndGetAddressOf()),
				//	&cp));

				//themeResourceExtension->m_strResourceKey = themeBrush;

				//IFC_RETURN(themeResourceExtension->SetInitialValueAndTargetDictionary(initialValueNoRef, dictionary));

				//IFC_RETURN(themeResourceExtension->SetThemeResourceBinding(
				//	m_overlayElement.Cast<FrameworkElement>()->GetHandle(),
				//	DirectUI::MetadataAPI::GetPropertyByIndex(KnownPropertyIndex::Shape_Fill))
				//	);

				var oBrush = ResourceResolver.ResolveTopLevelResource("AppBarLightDismissOverlayBackground");
				if (oBrush is Brush brush)
				{
					if (m_overlayElement is Rectangle rectangle)
					{
						rectangle.Fill = brush;
					}
				}
			}
			else
			{
				var transparentBrush = SolidColorBrushHelper.Transparent;

				if (m_overlayElement is Rectangle rectangle)
				{
					rectangle.Fill = transparentBrush;
				}
			}
		}

	}
}
