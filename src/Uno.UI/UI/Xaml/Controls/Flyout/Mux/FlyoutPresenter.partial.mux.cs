// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\FlyoutPresenter_partial.cpp, tag winui3/release/1.4.3, commit 685d2bf

#nullable enable

using Uno.UI.DataBinding;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Displays the content of a Flyout.
/// </summary>
partial class FlyoutPresenter
{
	/// <summary>
	/// Initializes a new instance of the FlyoutPresenter class.
	/// </summary>
	public FlyoutPresenter()
	{
		DefaultStyleKey = typeof(FlyoutPresenter);
	}

	//TODO:MZ: Move to Unloaded probably
	~FlyoutPresenter()
	{
		m_tpInnerScrollViewer = null;
	}

	internal FlyoutBase Flyout
	{
		set
		{
			MUX_ASSERT(m_wrFlyout is null);

			m_wrFlyout = WeakReferencePool.RentWeakReference(this, value);
		}
	}

	// Create FlyoutPresenterAutomationPeer to represent the FlyoutPresenter.
	protected override AutomationPeer OnCreateAutomationPeer() => FlyoutPresenterAutomationPeer.CreateInstanceWithOwner(this);

	protected override void OnApplyTemplate()
	{
		CleanupTemplateParts();

		base.OnApplyTemplate();

		var spInnerScrollViewer = GetTemplateChild<ScrollViewer>("ScrollViewer");
		//
		// Block calling SetPtrValue() temporary by failing DRT ImmersiveColorMediumCppFlyout.
		//
		//IFC(SetPtrValue(m_tpInnerScrollViewer, spInnerScrollViewer));
		//if (m_tpInnerScrollViewer is not null)
		//{
		//	m_tpInnerScrollViewer.Cast<ScrollViewer>()->m_isFocusableOnFlyoutScrollViewer = TRUE;
		//}
		//
		// The below code must be removed when SetPtrValue is called.
		//
		if (spInnerScrollViewer is not null)
		{
			spInnerScrollViewer.m_isFocusableOnFlyoutScrollViewer = true;
		}

		// Apply a shadow
		// Allow LTEs to target Popup.Child when Popup is windowed
		// ThemeTransition can't be applied to child of a windowed popup, we are targeting grandchild in FlyoutBase_partial.
		// Shadows need to be on the same element to work right, so we are applying shadows to grandchild too.
		var spChild = VisualTreeHelper.GetChild(this, 0);
		var spChildAsUIE = spChild as UIElement;

		bool isDefaultShadowEnabled = IsDefaultShadowEnabled;
		if (isDefaultShadowEnabled && spChildAsUIE is not null)
		{
			ApplyElevationEffect(spChildAsUIE);
		}
	}

	private string GetPlainText()
	{
		//    ctl::ComPtr<IDependencyObject> ownerFlyout;
		//HSTRING automationName = nullptr;

		//IFC_RETURN(m_wrFlyout.As(&ownerFlyout));

		//    if (ownerFlyout)
		//    {
		//        // If an automation name is set on the owner flyout, we'll use that as our plain text.
		//        // Otherwise, we'll report the default plain text.
		//        IFC_RETURN(DirectUI::AutomationProperties::GetNameStatic(ownerFlyout.Get(), &automationName));
		//    }

		//    if (automationName != nullptr)
		//{
		//	*strPlainText = automationName;
		//}
		//else
		//{
		//	// If we have no title, we'll fall back to the default implementation,
		//	// which retrieves our content as plain text (e.g., if our content is a string,
		//	// it returns that; if our content is a TextBlock, it returns its Text value, etc.)
		//	IFC_RETURN(base.GetPlainText(strPlainText));

		//	// If we get the plain text from the content, then we want to truncate it,
		//	// in case the resulting automation name is very long.
		//	IFC_RETURN(Popup::TruncateAutomationName(strPlainText));
		//}
	}

	private void CleanupTemplateParts()
	{
		if (m_tpInnerScrollViewer is not null)
		{
			m_tpInnerScrollViewer.m_isFocusableOnFlyoutScrollViewer = false;
		}

		m_tpInnerScrollViewer = null;
	}

	private FlyoutBase? GetOwnerFlyout()
	{
		//TODO:MZ: Verify this works
		if (!m_wrFlyout.IsDisposed)
		{
			return m_wrFlyout.Target;
		}
		return null;
	}

	internal string? GetOwnerName()
	{
		var ownerFlyout = GetOwnerFlyout();
		if (ownerFlyout != null)
		{
			//return ownerFlyout.
			//ctl::ComPtr<IInspectable> spName;

			//IFC(spOwnerFlyout->GetValue(
			//	MetadataAPI::GetDependencyPropertyByIndex(KnownPropertyIndex::DependencyObject_Name),
			//	&spName));

			//IFC(ctl::do_get_value(*pName, spName.Get()));
			//TODO:MZ: A "Name" dependency property should be used?
			return ownerFlyout.GetType().Name;
		}

		return null;
	}

	private DependencyObject? GetTargetIfOpenedAsTransientStatic(DependencyObject nativeControl)
	{
		ctl::ComPtr<DependencyObject> flyoutPresenterAsDO;
		ctl::ComPtr<FlyoutPresenter> flyoutPresenter;
		ctl::ComPtr<DependencyObject> target;

		IFC_RETURN(DXamlCore::GetCurrent()->GetPeer(nativeControl, &flyoutPresenterAsDO));
		IFC_RETURN(flyoutPresenterAsDO.As(&flyoutPresenter));

		IFC_RETURN(flyoutPresenter->GetTargetIfOpenedAsTransient(&target));

		*nativeTarget = target ? target->GetHandle() : nullptr;
		return S_OK;
	}

	private DependencyObject? GetTargetIfOpenedAsTransient()
	{
		FlyoutBase ownerFlyout = GetOwnerFlyout();

		if (ownerFlyout == null)
		{
			return null;
		}

		FlyoutShowMode showMode = ownerFlyout.ShowMode;

		if (showMode == FlyoutShowMode.Auto ||
			showMode == FlyoutShowMode.Standard)
		{
			return null;
		}

		return ownerFlyout.Target;
	}
}
