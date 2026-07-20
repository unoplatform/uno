#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Uno.UI;

namespace Microsoft.UI.Xaml.Automation;

[Bindable]
public sealed partial class AutomationProperties
{
	private static readonly ConditionalWeakTable<DependencyObject, HashSet<DependencyProperty>> _initializingCollections = new();

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
				// Use safe cast + trim + remove-when-empty so we never throw on a null NewValue
				// or persist a stale xamlautomationid="" attribute in the DOM. Matches the WASM
				// Skia ``setXamlAutomationId`` and ``setAriaStringAttribute`` contracts.
				var automationId = (args.NewValue as string)?.Trim();
				if (!string.IsNullOrEmpty(automationId))
				{
					uiElement.SetAttribute("xamlautomationid", automationId);
				}
				else
				{
					uiElement.RemoveAttribute("xamlautomationid");
				}
			}

			// AutomationId is a test/automation identifier, not an accessible name source.
			// aria-label must be sourced from AutomationProperties.Name (peer name resolution),
			// not from AutomationId — otherwise assistive tech announces the dev-only id.

			var role = FindHtmlRole(uiElement);
			if (!string.IsNullOrEmpty(role))
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
		NotifyAutomationPropertyChanged(
			dependencyObject,
			args,
			AutomationElementIdentifiers.AutomationIdProperty);
	}

	private static void OnAutomationPropertyChanged(
		DependencyObject dependencyObject,
		DependencyPropertyChangedEventArgs args)
	{
		if (_initializingCollections.TryGetValue(dependencyObject, out var properties) &&
			properties.Contains(args.Property))
		{
			return;
		}

		var automationProperty = GetAutomationProperty(args.Property);

		if (automationProperty is not null)
		{
			NotifyAutomationPropertyChanged(dependencyObject, args, automationProperty);
		}
	}

	private static AutomationProperty? GetAutomationProperty(DependencyProperty property)
		=> property == AcceleratorKeyProperty ? AutomationElementIdentifiers.AcceleratorKeyProperty :
			property == AccessKeyProperty ? AutomationElementIdentifiers.AccessKeyProperty :
			property == AnnotationsProperty ? AutomationElementIdentifiers.AnnotationsProperty :
			property == ControlledPeersProperty ? AutomationElementIdentifiers.ControlledPeersProperty :
			property == CultureProperty ? AutomationElementIdentifiers.CultureProperty :
			property == DescribedByProperty ? AutomationElementIdentifiers.DescribedByProperty :
			property == FlowsFromProperty ? AutomationElementIdentifiers.FlowsFromProperty :
			property == FlowsToProperty ? AutomationElementIdentifiers.FlowsToProperty :
			property == FullDescriptionProperty ? AutomationElementIdentifiers.FullDescriptionProperty :
			property == HelpTextProperty ? AutomationElementIdentifiers.HelpTextProperty :
			property == IsDialogProperty ? AutomationElementIdentifiers.IsDialogProperty :
			property == IsPeripheralProperty ? AutomationElementIdentifiers.IsPeripheralProperty :
			property == IsRequiredForFormProperty ? AutomationElementIdentifiers.IsRequiredForFormProperty :
			property == ItemStatusProperty ? AutomationElementIdentifiers.ItemStatusProperty :
			property == ItemTypeProperty ? AutomationElementIdentifiers.ItemTypeProperty :
			property == LabeledByProperty ? AutomationElementIdentifiers.LabeledByProperty :
			property == LandmarkTypeProperty ? AutomationElementIdentifiers.LandmarkTypeProperty :
			property == LevelProperty ? AutomationElementIdentifiers.LevelProperty :
			property == LiveSettingProperty ? AutomationElementIdentifiers.LiveSettingProperty :
			property == LocalizedControlTypeProperty ? AutomationElementIdentifiers.LocalizedControlTypeProperty :
			property == LocalizedLandmarkTypeProperty ? AutomationElementIdentifiers.LocalizedLandmarkTypeProperty :
			property == PositionInSetProperty ? AutomationElementIdentifiers.PositionInSetProperty :
			property == SizeOfSetProperty ? AutomationElementIdentifiers.SizeOfSetProperty :
			property == AutomationControlTypeProperty ? AutomationElementIdentifiers.ControlTypeProperty :
			null;

	private static IList<T> GetOrCreateAutomationCollection<T>(
		DependencyObject element,
		DependencyProperty dependencyProperty,
		AutomationProperty automationProperty)
	{
		if (element.GetValue(dependencyProperty) is IList<T> collection)
		{
			return collection;
		}

		collection = new AutomationPropertyCollection<T>(
			element,
			dependencyProperty,
			automationProperty);
		var initializingProperties = _initializingCollections.GetOrCreateValue(element);
		initializingProperties.Add(dependencyProperty);
		try
		{
			element.SetValue(dependencyProperty, collection);
		}
		finally
		{
			initializingProperties.Remove(dependencyProperty);
		}

		return collection;
	}

	private static void NotifyAutomationCollectionChanged(
		DependencyObject dependencyObject,
		AutomationProperty automationProperty,
		object oldValue,
		object newValue)
	{
#if __SKIA__
		if (AutomationPeer.AutomationPeerListener?.ListenerExistsHelper(AutomationEvents.PropertyChanged) == true &&
			dependencyObject is UIElement element &&
			element.GetOrCreateAutomationPeer() is { } peer)
		{
			AutomationPeer.AutomationPeerListener.NotifyPropertyChangedEvent(
				peer,
				automationProperty,
				oldValue,
				newValue);
		}
#endif
	}

	private sealed class AutomationPropertyCollection<T> : Collection<T>
	{
		private readonly WeakReference<DependencyObject> _owner;
		private readonly DependencyProperty _dependencyProperty;
		private readonly AutomationProperty _automationProperty;

		internal AutomationPropertyCollection(
			DependencyObject owner,
			DependencyProperty dependencyProperty,
			AutomationProperty automationProperty)
		{
			_owner = new(owner);
			_dependencyProperty = dependencyProperty;
			_automationProperty = automationProperty;
		}

		protected override void ClearItems()
		{
			if (Count == 0)
			{
				return;
			}

			var oldValue = CaptureValueForNotification();
			base.ClearItems();
			NotifyChanged(oldValue);
		}

		protected override void InsertItem(int index, T item)
		{
			var oldValue = CaptureValueForNotification();
			base.InsertItem(index, item);
			NotifyChanged(oldValue);
		}

		protected override void RemoveItem(int index)
		{
			var oldValue = CaptureValueForNotification();
			base.RemoveItem(index);
			NotifyChanged(oldValue);
		}

		protected override void SetItem(int index, T item)
		{
			var oldValue = CaptureValueForNotification();
			base.SetItem(index, item);
			NotifyChanged(oldValue);
		}

		private T[]? CaptureValueForNotification()
		{
#if __SKIA__
			if (AutomationPeer.AutomationPeerListener?.ListenerExistsHelper(AutomationEvents.PropertyChanged) == true &&
				_owner.TryGetTarget(out var owner) &&
				ReferenceEquals(owner.GetValue(_dependencyProperty), this))
			{
				return this.ToArray();
			}
#endif
			return null;
		}

		private void NotifyChanged(T[]? oldValue)
		{
			if (oldValue is not null &&
				_owner.TryGetTarget(out var owner) &&
				ReferenceEquals(owner.GetValue(_dependencyProperty), this))
			{
				NotifyAutomationCollectionChanged(
					owner,
					_automationProperty,
					oldValue,
					this.ToArray());
			}
		}
	}

	private static void OnAccessibilityViewChanged(
		DependencyObject dependencyObject,
		DependencyPropertyChangedEventArgs args)
	{
#if __SKIA__
		if (AutomationPeer.AutomationPeerListener?.ListenerExistsHelper(AutomationEvents.StructureChanged) == true &&
			dependencyObject is UIElement element &&
			element.GetOrCreateAutomationPeer() is { } peer)
		{
			AutomationPeer.AutomationPeerListener.NotifyAutomationEvent(peer, AutomationEvents.StructureChanged);
		}
#endif
	}

	private static void NotifyAutomationPropertyChanged(
		DependencyObject dependencyObject,
		DependencyPropertyChangedEventArgs args,
		AutomationProperty automationProperty)
	{
#if __SKIA__
		if (AutomationPeer.AutomationPeerListener?.ListenerExistsHelper(AutomationEvents.PropertyChanged) == true &&
			dependencyObject is UIElement element &&
			element.GetOrCreateAutomationPeer() is { } peer)
		{
			AutomationPeer.AutomationPeerListener.NotifyPropertyChangedEvent(
				peer,
				automationProperty,
				args.OldValue,
				args.NewValue);
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
	internal static string? FindHtmlRole(UIElement uIElement)
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
			// "appbar" is not a valid WAI-ARIA role token (UWP/macOS terminology).
			// CommandBar is closest to a toolbar; emit null here and let the caller
			// decide (the AutomationControlType.AppBar arm below also returns null).
			return null;
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
				// "dataitem", "header", and "appbar" are NOT valid WAI-ARIA role tokens.
				// DataItem outside a grid has no native ARIA equivalent; Header maps to HTML
				// <header>'s implicit role (banner/generic) not a literal "header" token;
				// AppBar is UWP/macOS terminology. Emit null so we don't push rejected tokens
				// into the accessibility tree.
				AutomationControlType.DataItem => null,
				AutomationControlType.Document => "document",
				AutomationControlType.Header => null,
				AutomationControlType.Table => "table",
				AutomationControlType.Separator => "separator",
				AutomationControlType.AppBar => null,
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
