using System;
using Android.Runtime;
using Android.Text;
using Android.Views.Accessibility;
using Uno;
using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Automation.Peers;

public partial class FrameworkElementAutomationPeer : AutomationPeer
{
	internal override void SendAccessibilityEvent([GeneratedEnum] EventTypes eventType)
	{
		// This is only called if View.Focusable and View.FocusableInTouchMode are set to true.

		base.SendAccessibilityEvent(eventType);

		switch (eventType)
		{
			case EventTypes.ViewClicked:
				InvokeAutomationPeer();
				break;
			default:
				break;
		}
	}

	internal override void OnInitializeAccessibilityNodeInfo(AccessibilityNodeInfo info)
	{
		if (!AutomationConfiguration.IsAccessibilityEnabled)
		{
			return;
		}

		var isAccessible = AutomationProperties.GetAccessibilityView(Owner) != AccessibilityView.Raw;
		if (isAccessible)
		{
			// TODO: Set View.Focusable and View.FocusableInTouchMode to true to ensure SendAccessibilityEvent gets called?
			info.Focusable = isAccessible;
			info.ContentDescription = GetName();
			info.Password = IsPassword();
			info.Enabled = IsEnabled();

			// TODO: Use GetAutomationControlType() and GetPattern() instead
			info.Clickable = GetClickable();
			info.Checked = GetChecked();
			info.Checkable = GetCheckable();
			info.Editable = GetEditable();
			info.InputType = GetInputType();
			info.MultiLine = GetMultiline();
			info.Selected = GetSelected();
			info.MaxTextLength = GetMaxTextLength();
			info.Scrollable = GetScrollable();
		}
	}

	private bool GetScrollable()
	{
		return this is IScrollProvider;
	}

	private int GetMaxTextLength()
	{
		return Owner is TextBox textBox ? textBox.MaxLength : 0;
	}

	private bool GetSelected()
	{
		// TODO: return this is ISelectionItemProvider selectionItemProvider && selectionItemProvider.IsSelected;
		return Owner is SelectorItem selectorItem && selectorItem.IsSelected;
	}

	private bool GetMultiline()
	{
		return Owner is TextBox textBox && textBox.AcceptsReturn;
	}

	private InputTypes GetInputType()
	{
		return Owner is TextBox textBox ? InputScopeHelper.ConvertInputScope(textBox.InputScope) : InputTypes.Null;
	}

	private bool GetEditable()
	{
		return Owner is TextBox
			|| Owner is PasswordBox;
	}

	private bool GetCheckable()
	{
		return this is IToggleProvider;
	}

	private bool GetChecked()
	{
		return this is IToggleProvider toggleProvider && toggleProvider.ToggleState == ToggleState.On;
	}

	private bool GetClickable()
	{
		return this is IInvokeProvider
			|| this is IToggleProvider
			|| this is ISelectionItemProvider;
	}
}
