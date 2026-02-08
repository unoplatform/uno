using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Uno.UI;

namespace Microsoft.UI.Xaml.Automation;

[Bindable]
public sealed partial class AutomationProperties
{
	private static void OnAutomationIdChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
#if __APPLE_UIKIT__
		if (FrameworkElementHelper.IsUiAutomationMappingEnabled && dependencyObject is UIKit.UIView view)
		{
			view.AccessibilityIdentifier = (string)args.NewValue;
		}
#elif __ANDROID__
			if (FrameworkElementHelper.IsUiAutomationMappingEnabled && dependencyObject is AView view)
			{
				view.ContentDescription = (string)args.NewValue;
			}
#elif __WASM__
			if (dependencyObject is UIElement uiElement)
			{
				if (FrameworkElementHelper.IsUiAutomationMappingEnabled)
				{
					uiElement.SetAttribute("xamlautomationid", (string)args.NewValue);
				}

				var role = FindHtmlRole(uiElement);
				if (role != null)
				{
					uiElement.SetAttribute(
						("aria-label", (string)args.NewValue),
						("role", role));
				}
			}
#endif
	}

	private static void OnNamePropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
#if __SKIA__
		if (AutomationPeer.AutomationPeerListener?.ListenerExistsHelper(AutomationEvents.PropertyChanged) == true &&
			dependencyObject is UIElement element && // TODO: Adjust when TextElement's automation peers are supported.
			element.GetOrCreateAutomationPeer() is { } peer)
		{
			AutomationPeer.AutomationPeerListener.NotifyPropertyChangedEvent(peer, AutomationElementIdentifiers.NameProperty, args.OldValue, args.NewValue);
		}
#endif
	}

#if __WASM__ || __SKIA__
	internal static string FindHtmlRole(UIElement uIElement)
	{
		if (__LinkerHints.Is_Microsoft_UI_Xaml_Controls_Button_Available && uIElement is Button)
		{
			return "button";
		}
		if (__LinkerHints.Is_Microsoft_UI_Xaml_Controls_RadioButton_Available && uIElement is RadioButton)
		{
			return "radio";
		}
		if (__LinkerHints.Is_Microsoft_UI_Xaml_Controls_CheckBox_Available && uIElement is CheckBox)
		{
			return "checkbox";
		}
		if (__LinkerHints.Is_Microsoft_UI_Xaml_Controls_TextBlock_Available && uIElement is TextBlock)
		{
			return "label";
		}
		if (__LinkerHints.Is_Microsoft_UI_Xaml_Controls_TextBox_Available && uIElement is TextBox)
		{
			return "textbox";
		}
		if (__LinkerHints.Is_Microsoft_UI_Xaml_Controls_Slider_Available && uIElement is Slider)
		{
			return "slider";
		}

		var peer = uIElement.GetOrCreateAutomationPeer();
		if (peer?.GetAutomationControlType() is { } type)
		{
			return type switch
			{
				AutomationControlType.Button => "button",
				AutomationControlType.RadioButton => "radio",
				AutomationControlType.CheckBox => "checkbox",
				AutomationControlType.Text => "label",
				AutomationControlType.Edit => "label",
				AutomationControlType.Slider => "slider",
				_ => null,
			};
		}

		return null;
	}
#endif
}
