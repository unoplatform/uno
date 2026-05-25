using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
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
		// Uno-specific: allow explicit role override via AutomationPropertiesExtensions.Role
		// (defined in Uno.UI.Toolkit). The provider is registered via RoleOverrideProvider.
		var roleOverride = GetRoleOverride(uIElement);
		if (!string.IsNullOrEmpty(roleOverride))
		{
			return roleOverride;
		}

		// Direct type checks for common controls (fast path, avoids peer creation)
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
		if (__LinkerHints.Is_Microsoft_UI_Xaml_Controls_TextBox_Available && uIElement is TextBox)
		{
			return "textbox";
		}
		if (__LinkerHints.Is_Microsoft_UI_Xaml_Controls_Slider_Available && uIElement is Slider)
		{
			return "slider";
		}
		if (uIElement is Image)
		{
			return "image";
		}
		if (uIElement is HyperlinkButton)
		{
			return "link";
		}
		if (uIElement is PasswordBox)
		{
			return "edit";
		}
		if (uIElement is RichEditBox)
		{
			return "edit";
		}
		if (uIElement is ComboBox)
		{
			return "combobox";
		}
		if (uIElement is ProgressBar)
		{
			return "progressbar";
		}
		if (uIElement is ProgressRing)
		{
			return "progressbar";
		}
		if (uIElement is ToggleSwitch)
		{
			return "checkbox";
		}
		if (uIElement is ListView or ListBox)
		{
			return "listbox";
		}
		if (uIElement is ListViewItem or ListBoxItem)
		{
			return "option";
		}
		if (uIElement is ScrollViewer)
		{
			return "pane";
		}
		if (uIElement is MenuBar)
		{
			return "menubar";
		}
		if (uIElement is MenuBarItem or MenuFlyoutItem)
		{
			return "menuitem";
		}
		if (uIElement is ToolTip)
		{
			return "tooltip";
		}
		if (uIElement is TreeView)
		{
			return "tree";
		}
		if (uIElement is TreeViewItem)
		{
			return "treeitem";
		}
		if (uIElement is Pivot)
		{
			return "tab";
		}
		if (uIElement is PivotItem)
		{
			return "tabitem";
		}
		if (uIElement is AppBar or CommandBar)
		{
			return "appbar";
		}
		if (uIElement is AppBarButton)
		{
			return "button";
		}

		var peer = uIElement.GetOrCreateAutomationPeer();
		if (peer?.GetAutomationControlType() is { } type)
		{
			return type switch
			{
				AutomationControlType.Button => "button",
				AutomationControlType.Calendar => "calendar",
				AutomationControlType.CheckBox => "checkbox",
				AutomationControlType.Edit => "textbox",
				AutomationControlType.Slider => "slider",
				AutomationControlType.Spinner => "spinner",
				AutomationControlType.StatusBar => "statusbar",
				AutomationControlType.Tab => "tab",
				AutomationControlType.TabItem => "tabitem",
				// "label" is NOT a valid WAI-ARIA role. Screen readers (VoiceOver)
				// ignore it and may announce the element as "group" instead.
				// Text elements should use no explicit role — their text is
				// communicated via aria-label or text content.
				AutomationControlType.Text => null,
				AutomationControlType.ToolBar => "toolbar",
				AutomationControlType.ToolTip => "tooltip",
				AutomationControlType.Tree => "tree",
				AutomationControlType.TreeItem => "treeitem",
				AutomationControlType.Custom => "custom",
				AutomationControlType.Group => "group",
				AutomationControlType.Thumb => "thumb",
				AutomationControlType.DataGrid => "datagrid",
				AutomationControlType.DataItem => "dataitem",
				AutomationControlType.Document => "document",
				AutomationControlType.SplitButton => "splitbutton",
				AutomationControlType.Window => "window",
				AutomationControlType.Pane => "pane",
				AutomationControlType.Header => "header",
				AutomationControlType.HeaderItem => "headeritem",
				AutomationControlType.Table => "table",
				AutomationControlType.TitleBar => "titlebar",
				AutomationControlType.Separator => "separator",
				AutomationControlType.SemanticZoom => "semanticzoom",
				AutomationControlType.AppBar => "appbar",
				_ => null,
			};
		}

		return null;
	}
#endif

	/// <summary>
	/// Attached property allowing role override to be supplied by external assemblies (e.g. Uno.UI.Toolkit).
	/// This avoids the need for a delegate/provider and simplifies lookups.
	/// </summary>
	public static DependencyProperty RoleOverrideProperty { get; } =
		DependencyProperty.RegisterAttached(
			"RoleOverride",
			typeof(string),
			typeof(AutomationProperties),
			new FrameworkPropertyMetadata(default(string)));

	public static void SetRoleOverride(UIElement element, string value) =>
		element.SetValue(RoleOverrideProperty, value);

	public static string GetRoleOverride(UIElement element) =>
		(string)element.GetValue(RoleOverrideProperty);
}
