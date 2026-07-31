#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Uno.UI;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Automation;

[Bindable]
public sealed partial class AutomationProperties
{
#if __SKIA__
	private static readonly ConditionalWeakTable<DependencyObject, RelationshipSubscriptions> _relationshipSubscriptions = new();
#endif

	private static IList<T> GetOrCreateRelationshipCollection<T>(DependencyObject owner, DependencyProperty property, AutomationProperty automationProperty)
		where T : DependencyObject
	{
		if (owner.GetValue(property) is IList<T> collection)
		{
			return collection;
		}

		collection = new AutomationRelationshipCollection<T>(owner, automationProperty);
		owner.SetValue(property, collection);
		return collection;
	}

	private static void OnControlledPeersChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args)
		=> OnRelationshipCollectionChanged<UIElement>(owner, ControlledPeersProperty, AutomationElementIdentifiers.ControlledPeersProperty, args);

	private static void OnDescribedByChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args)
		=> OnRelationshipCollectionChanged<DependencyObject>(owner, DescribedByProperty, AutomationElementIdentifiers.DescribedByProperty, args);

	private static void OnFlowsToChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args)
		=> OnRelationshipCollectionChanged<DependencyObject>(owner, FlowsToProperty, AutomationElementIdentifiers.FlowsToProperty, args);

	private static void OnFlowsFromChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args)
		=> OnRelationshipCollectionChanged<DependencyObject>(owner, FlowsFromProperty, AutomationElementIdentifiers.FlowsFromProperty, args);

	private static void OnRelationshipCollectionChanged<T>(DependencyObject owner, DependencyProperty property, AutomationProperty automationProperty, DependencyPropertyChangedEventArgs args)
		where T : DependencyObject
	{
#if __SKIA__
		var subscriptions = _relationshipSubscriptions.GetOrCreateValue(owner);
		subscriptions.Replace(property, ObserveRelationshipCollection<T>(owner, automationProperty, args.NewValue));

		if (args.OldValue is not null || args.NewValue is not ICollection<T> { Count: 0 })
		{
			NotifyAutomationPropertyChanged(owner, automationProperty, args.OldValue, args.NewValue);
		}
#endif
	}

#if __SKIA__
	private static Action? ObserveRelationshipCollection<T>(DependencyObject owner, AutomationProperty automationProperty, object? value)
		where T : DependencyObject
	{
		if (value is null || value.GetType().IsGenericType && value.GetType().GetGenericTypeDefinition() == typeof(AutomationRelationshipCollection<>))
		{
			return null;
		}

		var ownerReference = new WeakReference<DependencyObject>(owner);
		if (value is INotifyCollectionChanged notifyCollectionChanged)
		{
			NotifyCollectionChangedEventHandler? handler = null;
			handler = (_, _) =>
			{
				if (ownerReference.TryGetTarget(out var target))
				{
					NotifyAutomationPropertyChanged(target, automationProperty, null, value);
				}
				else
				{
					notifyCollectionChanged.CollectionChanged -= handler;
				}
			};
			notifyCollectionChanged.CollectionChanged += handler;
			return () => notifyCollectionChanged.CollectionChanged -= handler;
		}

		if (value is IObservableVector<T> observableVector)
		{
			VectorChangedEventHandler<T>? handler = null;
			handler = (_, _) =>
			{
				if (ownerReference.TryGetTarget(out var target))
				{
					NotifyAutomationPropertyChanged(target, automationProperty, null, value);
				}
				else
				{
					observableVector.VectorChanged -= handler;
				}
			};
			observableVector.VectorChanged += handler;
			return () => observableVector.VectorChanged -= handler;
		}

		return null;
	}
#endif

	private static void OnLabeledByChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args)
	{
#if __SKIA__
		NotifyAutomationPropertyChanged(owner, AutomationElementIdentifiers.LabeledByProperty, args.OldValue, args.NewValue);
#endif
	}

	private static void NotifyAutomationPropertyChanged(DependencyObject owner, AutomationProperty property, object? oldValue, object? newValue)
	{
#if __SKIA__
		if (AutomationPeer.AutomationPeerListener?.ListenerExistsHelper(AutomationEvents.PropertyChanged) == true &&
			owner is UIElement element &&
			element.GetOrCreateAutomationPeer() is { } peer)
		{
			AutomationPeer.AutomationPeerListener.NotifyPropertyChangedEvent(peer, property, oldValue!, newValue!);
		}
#endif
	}

	private sealed class AutomationRelationshipCollection<T> : DependencyObjectCollection<T>
		where T : DependencyObject
	{
		private readonly DependencyObject _owner;
		private readonly AutomationProperty _automationProperty;

		public AutomationRelationshipCollection(DependencyObject owner, AutomationProperty automationProperty)
			: base(owner, isAutoPropertyInheritanceEnabled: false)
		{
			_owner = owner;
			_automationProperty = automationProperty;
		}

		private protected override void OnAdded(T item)
		{
		}

		private protected override void OnRemoved(T item)
		{
		}

		private protected override void OnCollectionChanged()
		{
			base.OnCollectionChanged();
			NotifyAutomationPropertyChanged(_owner, _automationProperty, null, this);
		}
	}

#if __SKIA__
	private sealed class RelationshipSubscriptions
	{
		private readonly Dictionary<DependencyProperty, Action> _subscriptions = new();

		public void Replace(DependencyProperty property, Action? unsubscribe)
		{
			if (_subscriptions.Remove(property, out var previous))
			{
				previous();
			}

			if (unsubscribe is not null)
			{
				_subscriptions[property] = unsubscribe;
			}
		}
	}
#endif

	private static void OnAutomationIdChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
#if __SKIA__
		NotifyAutomationPropertyChanged(dependencyObject, AutomationElementIdentifiers.AutomationIdProperty, args.OldValue, args.NewValue);
#endif
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

	private static void OnLandmarkTypeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
#if __SKIA__
		NotifyAutomationPropertyChanged(dependencyObject, AutomationElementIdentifiers.LandmarkTypeProperty, args.OldValue, args.NewValue);
#endif
	}

	private static void OnLocalizedLandmarkTypeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
#if __SKIA__
		NotifyAutomationPropertyChanged(dependencyObject, AutomationElementIdentifiers.LocalizedLandmarkTypeProperty, args.OldValue, args.NewValue);
#endif
	}

	private static void OnFullDescriptionChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
#if __SKIA__
		NotifyAutomationPropertyChanged(dependencyObject, AutomationElementIdentifiers.FullDescriptionProperty, args.OldValue, args.NewValue);
#endif
	}

	private static void OnHelpTextChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
#if __SKIA__
		NotifyAutomationPropertyChanged(dependencyObject, AutomationElementIdentifiers.HelpTextProperty, args.OldValue, args.NewValue);
#endif
	}

	private static void OnAcceleratorKeyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		=> NotifyAutomationPropertyChanged(dependencyObject, AutomationElementIdentifiers.AcceleratorKeyProperty, args.OldValue, args.NewValue);

	private static void OnAccessKeyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		=> NotifyAutomationPropertyChanged(dependencyObject, AutomationElementIdentifiers.AccessKeyProperty, args.OldValue, args.NewValue);

	private static void OnCultureChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		=> NotifyAutomationPropertyChanged(dependencyObject, AutomationElementIdentifiers.CultureProperty, args.OldValue, args.NewValue);

	private static void OnIsDialogChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		=> NotifyAutomationPropertyChanged(dependencyObject, AutomationElementIdentifiers.IsDialogProperty, args.OldValue, args.NewValue);

	private static void OnItemStatusChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		=> NotifyAutomationPropertyChanged(dependencyObject, AutomationElementIdentifiers.ItemStatusProperty, args.OldValue, args.NewValue);

	private static void OnLevelChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		=> NotifyAutomationPropertyChanged(dependencyObject, AutomationElementIdentifiers.LevelProperty, args.OldValue, args.NewValue);

	private static void OnLiveSettingChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		=> NotifyAutomationPropertyChanged(dependencyObject, AutomationElementIdentifiers.LiveSettingProperty, args.OldValue, args.NewValue);

	private static void OnLocalizedControlTypeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		=> NotifyAutomationPropertyChanged(dependencyObject, AutomationElementIdentifiers.LocalizedControlTypeProperty, args.OldValue, args.NewValue);

	private static void OnPositionInSetChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		=> NotifyAutomationPropertyChanged(dependencyObject, AutomationElementIdentifiers.PositionInSetProperty, args.OldValue, args.NewValue);

	private static void OnSizeOfSetChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		=> NotifyAutomationPropertyChanged(dependencyObject, AutomationElementIdentifiers.SizeOfSetProperty, args.OldValue, args.NewValue);

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

	private static void OnIsRequiredForFormChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
#if __SKIA__
		if (AutomationPeer.AutomationPeerListener?.ListenerExistsHelper(AutomationEvents.PropertyChanged) == true &&
			dependencyObject is UIElement element &&
			element.GetOrCreateAutomationPeer() is { } peer)
		{
			AutomationPeer.AutomationPeerListener.NotifyPropertyChangedEvent(peer, AutomationElementIdentifiers.IsRequiredForFormProperty, args.OldValue, args.NewValue);
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
			new FrameworkPropertyMetadata(default(string), OnRoleOverrideChanged));

	private static void OnRoleOverrideChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args)
	{
#if __SKIA__
		if (AutomationPeer.AutomationPeerListener?.ListenerExistsHelper(AutomationEvents.PropertyChanged) == true &&
			owner is UIElement element &&
			element.GetOrCreateAutomationPeer() is { } peer)
		{
			AutomationPeer.AutomationPeerListener.NotifyInvalidatePeer(peer);
		}
#endif
	}

	public static void SetRoleOverride(UIElement element, string value) =>
		element.SetValue(RoleOverrideProperty, value);

	public static string GetRoleOverride(UIElement element) =>
		(string)element.GetValue(RoleOverrideProperty);
}
