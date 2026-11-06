using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Input;
using Uno.UI.DataBinding;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class SelectorItem
{
	// Whether or not this item is a data virtualized placeholder.
	internal bool m_isPlaceholder;

	// Whether or not this item is showing a scrolling placeholder (ShowsScrollingPlaceholders).
	// This almost matches the data placeholder state (m_isPlaceholder) but not quite
	// because it is set by the ListViewBase container phase rather than by data virtualization.
	private bool m_isUIPlaceholder;

	internal bool GetIsPlaceholder() => m_isPlaceholder;

	internal void SetIsUIPlaceholder(bool isPlaceholder) => m_isUIPlaceholder = isPlaceholder;

	internal bool GetIsUIPlaceholder() => m_isUIPlaceholder;

	// ChangeVisualState is private protected on Control; ListViewBase (a C++ friend) needs to reach it.
	internal void ChangeVisualStateFromOwner(bool useTransitions) => ChangeVisualState(useTransitions);

	// If this item is unfocused, sets focus on the SelectorItem.
	// Otherwise, sets focus to whichever element currently has focus
	// (so focusState can be propagated).
	internal void FocusSelfOrChild(
		FocusState focusState,
		bool animateIfBringIntoView,
		out bool pFocused,
		FocusNavigationDirection focusNavigationDirection,
		Uno.UI.Xaml.Input.InputActivationBehavior inputActivationBehavior)
	{
		bool isItemAlreadyFocused = false;
		DependencyObject spItemToFocus = null;

		pFocused = false;

		isItemAlreadyFocused = HasFocus();
		if (isItemAlreadyFocused)
		{
			// Re-focus the currently focused item to propagate focusState (the item might be focused
			// under a different FocusState value).
			spItemToFocus = this.GetFocusedElement();
		}
		else
		{
			spItemToFocus = this;
		}

		if (spItemToFocus is not null)
		{
			bool forceBringIntoView = false;
			pFocused = this.SetFocusedElementWithDirection(spItemToFocus, focusState, animateIfBringIntoView, focusNavigationDirection, forceBringIntoView, inputActivationBehavior);
		}
	}

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Returns a plain text string to provide a default AutomationProperties.Name
	//      in the absence of an explicitly defined one
	//
	//---------------------------------------------------------------------------
	internal override string GetPlainText()
	{
		string strPlainText = null;

		var contentTemplateRoot = ContentTemplateRoot;

		if (contentTemplateRoot is DependencyObject doContentTemplateRoot)
		{
			// we have the first child of the content. Check whether it has an automation name

			strPlainText = AutomationProperties.GetName(doContentTemplateRoot);

			// fallback: use getplain text on it
			if (string.IsNullOrEmpty(strPlainText))
			{
				var contentTemplateRootAsIFE = contentTemplateRoot as FrameworkElement;

				strPlainText = null;

				if (contentTemplateRootAsIFE is not null)
				{
					strPlainText = contentTemplateRootAsIFE.GetPlainText();
				}
			}

			// fallback, use GetPlainText on the contentpresenter, who has some special logic to account for old templates
			if (string.IsNullOrEmpty(strPlainText))
			{
				var contentTemplateRootAsIFE = contentTemplateRoot as FrameworkElement;

				strPlainText = null;

				if (contentTemplateRootAsIFE is not null)
				{
					var pParent = contentTemplateRootAsIFE.Parent;
					if (pParent is ContentPresenter cp)
					{
						strPlainText = cp.GetTextBlockText();
					}
				}
			}
		}

		// Fallback is to call the ancestor's GetPlainText. SelectorItemGenerated doesn't have a GetPlainText
		// implementation, so it would find something in the parent. As of this writing, it should be the
		// ContentControl.
		if (string.IsNullOrEmpty(strPlainText))
		{
			return base.GetPlainText();
		}

		return strPlainText;
	}
}
