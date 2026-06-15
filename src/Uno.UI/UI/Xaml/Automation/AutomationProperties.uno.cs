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

			// AutomationId is a test/automation identifier, not an accessible name source.
			// aria-label must be sourced from AutomationProperties.Name (peer name resolution),
			// not from AutomationId — otherwise assistive tech announces the dev-only id.

			var role = FindHtmlRole(uiElement);
			if (role != null)
			{
				uiElement.SetAttribute("role", role);
			}
			else
			{
				// FR-020 role-token normalization can now return null for non-ARIA control types.
				// Explicitly clear any previously-set role so stale tokens don't survive a normalization
				// change (or a control-type swap) that drops the role for this element.
				uiElement.RemoveAttribute("role");
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

	// FR-011: a runtime HeadingLevel change must reach assistive tech. The attached property is not
	// polled by RaiseAutomaticPropertyChanges, so we raise the change here; the accessibility router
	// then live-updates aria-level (the <hN> tag, clamped to <h6> at creation, is not re-created).
	private static void OnHeadingLevelChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
#if __SKIA__
		if (AutomationPeer.AutomationPeerListener?.ListenerExistsHelper(AutomationEvents.PropertyChanged) == true &&
			dependencyObject is UIElement element &&
			element.GetOrCreateAutomationPeer() is { } peer)
		{
			AutomationPeer.AutomationPeerListener.NotifyPropertyChangedEvent(peer, AutomationElementIdentifiers.HeadingLevelProperty, args.OldValue, args.NewValue);
		}
#endif
	}

	// FR-023: a runtime IsDataValidForForm change must reach assistive tech. The attached property is not
	// polled by RaiseAutomaticPropertyChanges, so we raise the change here; the accessibility router then
	// live-updates aria-invalid (inverted polarity — false means invalid).
	private static void OnIsDataValidForFormChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
#if __SKIA__
		if (AutomationPeer.AutomationPeerListener?.ListenerExistsHelper(AutomationEvents.PropertyChanged) == true &&
			dependencyObject is UIElement element &&
			element.GetOrCreateAutomationPeer() is { } peer)
		{
			AutomationPeer.AutomationPeerListener.NotifyPropertyChangedEvent(peer, AutomationElementIdentifiers.IsDataValidForFormProperty, args.OldValue, args.NewValue);
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
			return "img";
		}
		if (uIElement is HyperlinkButton)
		{
			return "link";
		}
		if (uIElement is PasswordBox)
		{
			return "textbox";
		}
		if (uIElement is RichEditBox)
		{
			return "textbox";
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
			return "switch";
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
			// "pane" is not a valid WAI-ARIA role; ScrollViewer carries no semantic role here.
			return null;
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
			return "tab";
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
				AutomationControlType.CheckBox => "checkbox",
				AutomationControlType.Edit => "textbox",
				AutomationControlType.Slider => "slider",
				AutomationControlType.Spinner => "spinbutton",
				AutomationControlType.StatusBar => "status",
				AutomationControlType.Tab => "tab",
				AutomationControlType.TabItem => "tab",
				// "label" is NOT a valid WAI-ARIA role. Screen readers (VoiceOver)
				// ignore it and may announce the element as "group" instead.
				// Text elements should use no explicit role — their text is
				// communicated via aria-label or text content.
				AutomationControlType.Text => null,
				AutomationControlType.ToolBar => "toolbar",
				AutomationControlType.ToolTip => "tooltip",
				AutomationControlType.Tree => "tree",
				AutomationControlType.TreeItem => "treeitem",
				AutomationControlType.Group => "group",
				AutomationControlType.DataGrid => "grid",
				AutomationControlType.DataItem => "dataitem",
				AutomationControlType.Document => "document",
				AutomationControlType.Header => "header",
				AutomationControlType.Table => "table",
				AutomationControlType.Separator => "separator",
				AutomationControlType.AppBar => "appbar",
				// The following UIA control types have no valid WAI-ARIA role.
				// Emitting them as a "role" attribute is rejected by the
				// accessibility tree, so map them to null (no role) instead.
				AutomationControlType.Calendar => null,
				AutomationControlType.Custom => null,
				AutomationControlType.Thumb => null,
				AutomationControlType.SplitButton => null,
				AutomationControlType.Window => null,
				AutomationControlType.Pane => null,
				AutomationControlType.HeaderItem => null,
				AutomationControlType.TitleBar => null,
				AutomationControlType.SemanticZoom => null,
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
