// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference AutomationPeer_Partial.cpp, tag winui3/release/1.8.2

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Automation.Text;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Automation.Peers;

partial class AutomationPeer
{
	/// <summary>
	/// Gets the pattern provider for the specified pattern interface.
	/// </summary>
	/// <param name="patternInterface">The pattern interface to get.</param>
	/// <returns>The pattern provider, or null if not supported.</returns>
	protected virtual object? GetPatternCore(PatternInterface patternInterface) => null;

	/// <summary>
	/// Gets the collection of child automation peers.
	/// </summary>
	/// <returns>A list of child automation peers.</returns>
	public IList<AutomationPeer>? GetChildren()
	{
		var children = GetChildrenCore();
		if (children is null)
		{
			return null;
		}

		var count = children.Count;

		// Defining a set of nodes as children implies that all the children must target this node as their parent.
		// We ensure that relationship here for managed peer objects.
		for (int i = 0; i < count; i++)
		{
			var child = children[i];
			child?.SetParent(this);
		}

		return children;
	}

	/// <summary>
	/// Gets the collection of peers that are controlled by this peer.
	/// </summary>
	/// <returns>A read-only list of controlled peers.</returns>
	public IReadOnlyList<AutomationPeer>? GetControlledPeers() => GetControlledPeersCore();

	/// <summary>
	/// Shows the context menu for this element.
	/// </summary>
	public void ShowContextMenu() => ShowContextMenuCore();

	/// <summary>
	/// Gets the automation peer from the specified point.
	/// </summary>
	/// <param name="point">The point in screen coordinates.</param>
	/// <returns>The automation peer at the specified point.</returns>
	public AutomationPeer? GetPeerFromPoint(Point point) => GetPeerFromPointCore(point);

	/// <summary>
	/// Gets the accelerator key string.
	/// </summary>
	/// <returns>The accelerator key string.</returns>
	protected virtual string GetAcceleratorKeyCore() => string.Empty;

	/// <summary>
	/// Gets the access key string.
	/// </summary>
	/// <returns>The access key string.</returns>
	protected virtual string GetAccessKeyCore() => string.Empty;

	/// <summary>
	/// Gets the automation control type.
	/// </summary>
	/// <returns>The automation control type.</returns>
	protected virtual AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;

	/// <summary>
	/// Gets the automation ID.
	/// </summary>
	/// <returns>The automation ID string.</returns>
	protected virtual string GetAutomationIdCore() => string.Empty;

	/// <summary>
	/// Gets the bounding rectangle in screen coordinates.
	/// </summary>
	/// <returns>The bounding rectangle.</returns>
	protected virtual Rect GetBoundingRectangleCore() => default;

	/// <summary>
	/// Gets the collection of child automation peers.
	/// </summary>
	/// <returns>A list of child peers.</returns>
	protected virtual IList<AutomationPeer>? GetChildrenCore() => null;

	/// <summary>
	/// Navigates to the specified direction in the automation tree.
	/// Custom APs can override NavigateCore to manage the navigation of APs completely by themselves.
	/// In addition to that they can also use it to return native UIA nodes which is why the return
	/// type is object instead of an IAutomationPeer*. The default implementation still uses
	/// GetChildren and GetParent to have backward compatibility. This method also deprecates GetParent.
	/// </summary>
	/// <param name="direction">The navigation direction.</param>
	/// <returns>The automation peer in the specified direction.</returns>
	protected virtual object? NavigateCore(AutomationNavigationDirection direction)
	{
		IList<AutomationPeer>? children;
		AutomationPeer? result = null;
		int count;

		switch (direction)
		{
			case AutomationNavigationDirection.FirstChild:
				children = GetChildren();
				if (children is not null)
				{
					count = children.Count;
					if (count > 0)
					{
						result = children[0];
					}
				}
				break;

			case AutomationNavigationDirection.LastChild:
				children = GetChildren();
				if (children is not null)
				{
					count = children.Count;
					if (count > 0)
					{
						result = children[count - 1];
					}
				}
				break;

			case AutomationNavigationDirection.PreviousSibling:
				{
					// Prev/Next needs to make sure to handle case where parent is root window, GetParent will be null in that case.
					var parent = GetParent();
					if (parent is not null)
					{
						children = parent.GetChildren();
						if (children is not null)
						{
							var index = children.IndexOf(this);
							if (index != -1 && index > 0)
							{
								result = children[index - 1];
							}
						}
					}
				}
				break;

			case AutomationNavigationDirection.NextSibling:
				{
					var parent = GetParent();
					if (parent is not null)
					{
						children = parent.GetChildren();
						if (children is not null)
						{
							count = children.Count;
							var index = children.IndexOf(this);
							MUX_ASSERT(count == 0 ? index == -1 : true);
							if (index != -1 && index < count - 1)
							{
								result = children[index + 1];
							}
						}
					}
				}
				break;

			case AutomationNavigationDirection.Parent:
				result = GetParent();
				break;

			default:
				throw new NotSupportedException("Unsupported AutomationNavigationDirection");
		}

		return result;
	}

	/// <summary>
	/// Gets the class name.
	/// </summary>
	/// <returns>The class name string.</returns>
	protected virtual string GetClassNameCore() => string.Empty;

	/// <summary>
	/// Gets a clickable point on the element.
	/// </summary>
	/// <returns>A clickable point.</returns>
	protected virtual Point GetClickablePointCore() => default;

	/// <summary>
	/// Gets the help text.
	/// </summary>
	/// <returns>The help text string.</returns>
	protected virtual string GetHelpTextCore() => string.Empty;

	/// <summary>
	/// Gets the item status.
	/// </summary>
	/// <returns>The item status string.</returns>
	protected virtual string GetItemStatusCore() => string.Empty;

	/// <summary>
	/// Gets the item type.
	/// </summary>
	/// <returns>The item type string.</returns>
	protected virtual string GetItemTypeCore() => string.Empty;

	/// <summary>
	/// Gets the peer that labels this element.
	/// </summary>
	/// <returns>The labeling peer.</returns>
	protected virtual AutomationPeer? GetLabeledByCore() => null;

	/// <summary>
	/// Gets the localized control type string based on the automation control type.
	/// </summary>
	/// <returns>The localized control type string.</returns>
	protected virtual string? GetLocalizedControlTypeCore()
	{
		AutomationControlType apType;
		try
		{
			apType = GetAutomationControlType();
		}
		catch (Exception)
		{
			// PopupRoot only has a control type if a light dismiss popup is on top. Otherwise it's not a control and
			// returns S_FALSE. Allow it and return a null string.
			return null;
		}

#if HAS_UNO
		var core = DirectUI.DXamlCore.GetCurrentNoCreate();

		return apType switch
		{
			AutomationControlType.Button => core?.GetLocalizedResourceString("UIA_AP_BUTTON"),
			AutomationControlType.Calendar => core?.GetLocalizedResourceString("UIA_AP_CALENDAR"),
			AutomationControlType.CheckBox => core?.GetLocalizedResourceString("UIA_AP_CHECKBOX"),
			AutomationControlType.ComboBox => core?.GetLocalizedResourceString("UIA_AP_COMBOBOX"),
			AutomationControlType.Edit => core?.GetLocalizedResourceString("UIA_AP_EDIT"),
			AutomationControlType.Hyperlink => core?.GetLocalizedResourceString("UIA_AP_HYPERLINK"),
			AutomationControlType.Image => core?.GetLocalizedResourceString("UIA_AP_IMAGE"),
			AutomationControlType.ListItem => core?.GetLocalizedResourceString("UIA_AP_LISTITEM"),
			AutomationControlType.List => core?.GetLocalizedResourceString("UIA_AP_LIST"),
			AutomationControlType.Menu => core?.GetLocalizedResourceString("UIA_AP_MENU"),
			AutomationControlType.MenuBar => core?.GetLocalizedResourceString("UIA_AP_MENUBAR"),
			AutomationControlType.MenuItem => core?.GetLocalizedResourceString("UIA_AP_MENUITEM"),
			AutomationControlType.ProgressBar => core?.GetLocalizedResourceString("UIA_AP_PROGRESSBAR"),
			AutomationControlType.RadioButton => core?.GetLocalizedResourceString("UIA_AP_RADIOBUTTON"),
			AutomationControlType.ScrollBar => core?.GetLocalizedResourceString("UIA_AP_SCROLLBAR"),
			AutomationControlType.Slider => core?.GetLocalizedResourceString("UIA_AP_SLIDER"),
			AutomationControlType.Spinner => core?.GetLocalizedResourceString("UIA_AP_SPINNER"),
			AutomationControlType.StatusBar => core?.GetLocalizedResourceString("UIA_AP_STATUSBAR"),
			AutomationControlType.Tab => core?.GetLocalizedResourceString("UIA_AP_TAB"),
			AutomationControlType.TabItem => core?.GetLocalizedResourceString("UIA_AP_TABITEM"),
			AutomationControlType.Text => core?.GetLocalizedResourceString("UIA_AP_TEXT"),
			AutomationControlType.ToolBar => core?.GetLocalizedResourceString("UIA_AP_TOOLBAR"),
			AutomationControlType.ToolTip => core?.GetLocalizedResourceString("UIA_AP_TOOLTIP"),
			AutomationControlType.Tree => core?.GetLocalizedResourceString("UIA_AP_TREE"),
			AutomationControlType.TreeItem => core?.GetLocalizedResourceString("UIA_AP_TREEITEM"),
			AutomationControlType.Custom => core?.GetLocalizedResourceString("UIA_AP_CUSTOM"),
			AutomationControlType.Group => core?.GetLocalizedResourceString("UIA_AP_GROUP"),
			AutomationControlType.Thumb => core?.GetLocalizedResourceString("UIA_AP_THUMB"),
			AutomationControlType.DataGrid => core?.GetLocalizedResourceString("UIA_AP_DATAGRID"),
			AutomationControlType.DataItem => core?.GetLocalizedResourceString("UIA_AP_DATAITEM"),
			AutomationControlType.Document => core?.GetLocalizedResourceString("UIA_AP_DOCUMENT"),
			AutomationControlType.SplitButton => core?.GetLocalizedResourceString("UIA_AP_SPLITBUTTON"),
			AutomationControlType.Window => core?.GetLocalizedResourceString("UIA_AP_WINDOW"),
			AutomationControlType.Pane => core?.GetLocalizedResourceString("UIA_AP_PANE"),
			AutomationControlType.Header => core?.GetLocalizedResourceString("UIA_AP_HEADER"),
			AutomationControlType.HeaderItem => core?.GetLocalizedResourceString("UIA_AP_HEADERITEM"),
			AutomationControlType.Table => core?.GetLocalizedResourceString("UIA_AP_TABLE"),
			AutomationControlType.TitleBar => core?.GetLocalizedResourceString("UIA_AP_TITLEBAR"),
			AutomationControlType.Separator => core?.GetLocalizedResourceString("UIA_AP_SEPARATOR"),
			AutomationControlType.SemanticZoom => core?.GetLocalizedResourceString("UIA_AP_SEMANTICZOOM"),
			AutomationControlType.AppBar => core?.GetLocalizedResourceString("UIA_AP_APPBAR"),
			AutomationControlType.FlipView => core?.GetLocalizedResourceString("UIA_AP_FLIPVIEW"),
			_ => throw new NotSupportedException("Unsupported AutomationControlType"),
		};
#else
		// Fallback: humanize the enum name
		return Enum.GetName(apType)?.ToLowerInvariant();
#endif
	}

	/// <summary>
	/// Gets the name of this element.
	/// </summary>
	/// <returns>The name string.</returns>
	protected virtual string GetNameCore() => string.Empty;

	/// <summary>
	/// Gets the orientation.
	/// </summary>
	/// <returns>The orientation.</returns>
	protected virtual AutomationOrientation GetOrientationCore() => AutomationOrientation.None;

	/// <summary>
	/// Gets the live setting.
	/// </summary>
	/// <returns>The live setting value.</returns>
	protected virtual AutomationLiveSetting GetLiveSettingCore() => AutomationLiveSetting.Off;

	/// <summary>
	/// Gets the position in set.
	/// </summary>
	/// <returns>The position in set, or -1 if not applicable.</returns>
	protected virtual int GetPositionInSetCore() => -1;

	/// <summary>
	/// Gets the size of set.
	/// </summary>
	/// <returns>The size of set, or -1 if not applicable.</returns>
	protected virtual int GetSizeOfSetCore() => -1;

	/// <summary>
	/// Gets the level.
	/// </summary>
	/// <returns>The level, or -1 if not applicable.</returns>
	protected virtual int GetLevelCore() => -1;

	/// <summary>
	/// Gets the controlled peers.
	/// </summary>
	/// <returns>A read-only list of controlled peers.</returns>
	protected virtual IReadOnlyList<AutomationPeer>? GetControlledPeersCore() => null;

	/// <summary>
	/// Gets the annotations.
	/// </summary>
	/// <returns>A list of annotations.</returns>
	protected virtual IList<AutomationPeerAnnotation>? GetAnnotationsCore() => null;

	/// <summary>
	/// Gets the landmark type.
	/// </summary>
	/// <returns>The landmark type.</returns>
	protected virtual AutomationLandmarkType GetLandmarkTypeCore() => AutomationLandmarkType.None;

	/// <summary>
	/// Gets the localized landmark type.
	/// </summary>
	/// <returns>The localized landmark type string.</returns>
	protected virtual string GetLocalizedLandmarkTypeCore() => string.Empty;

	/// <summary>
	/// Gets whether this element has keyboard focus.
	/// </summary>
	/// <returns>True if the element has keyboard focus.</returns>
	protected virtual bool HasKeyboardFocusCore() => HasKeyboardFocusHelper();

	/// <summary>
	/// Gets whether this element is a content element.
	/// </summary>
	/// <returns>True if this is a content element.</returns>
	protected virtual bool IsContentElementCore() => false;

	/// <summary>
	/// Gets whether this element is a control element.
	/// </summary>
	/// <returns>True if this is a control element.</returns>
	protected virtual bool IsControlElementCore() => false;

	/// <summary>
	/// Gets whether this element is enabled.
	/// </summary>
	/// <returns>True if the element is enabled.</returns>
	protected virtual bool IsEnabledCore() => true;

	/// <summary>
	/// Gets whether this element can receive keyboard focus.
	/// </summary>
	/// <returns>True if the element can receive keyboard focus.</returns>
	protected virtual bool IsKeyboardFocusableCore() => false;

	/// <summary>
	/// Gets whether this element is offscreen.
	/// </summary>
	/// <returns>True if the element is offscreen.</returns>
	protected virtual bool IsOffscreenCore() => false;

	/// <summary>
	/// Gets whether this element is a password field.
	/// </summary>
	/// <returns>True if this is a password field.</returns>
	protected virtual bool IsPasswordCore() => false;

	/// <summary>
	/// Gets whether this element is required for form completion.
	/// </summary>
	/// <returns>True if required for form.</returns>
	protected virtual bool IsRequiredForFormCore() => false;

	/// <summary>
	/// Sets focus to this element.
	/// </summary>
	protected virtual void SetFocusCore()
	{
		// Lets keep this as it is, that is getting the value from core, Core layer has some exceptional logic that depends upon
		// focus manager.
		SetFocusHelper();
	}

	/// <summary>
	/// Sets automation focus to this element (without keyboard focus).
	/// </summary>
	private void SetAutomationFocus() => SetAutomationFocusHelper();

	/// <summary>
	/// Shows the context menu at the element's location.
	/// </summary>
	protected virtual void ShowContextMenuCore()
	{
	}

	/// <summary>
	/// Gets whether this element is peripheral.
	/// </summary>
	/// <returns>True if the element is peripheral.</returns>
	protected virtual bool IsPeripheralCore() => false;

	/// <summary>
	/// Gets whether the data is valid for form submission.
	/// </summary>
	/// <returns>True if data is valid for form.</returns>
	protected virtual bool IsDataValidForFormCore() => true;

	/// <summary>
	/// Gets the full description of this element.
	/// </summary>
	/// <returns>The full description string.</returns>
	protected virtual string GetFullDescriptionCore() => string.Empty;

	/// <summary>
	/// Gets the peers that describe this element.
	/// </summary>
	/// <returns>An enumerable of describing peers.</returns>
	protected virtual IEnumerable<AutomationPeer>? GetDescribedByCore() => null;

	/// <summary>
	/// Gets the peers that this element flows to.
	/// </summary>
	/// <returns>An enumerable of peers this flows to.</returns>
	protected virtual IEnumerable<AutomationPeer>? GetFlowsToCore() => null;

	/// <summary>
	/// Gets the peers that flow to this element.
	/// </summary>
	/// <returns>An enumerable of peers that flow to this.</returns>
	protected virtual IEnumerable<AutomationPeer>? GetFlowsFromCore() => null;

	/// <summary>
	/// Gets the culture of this element.
	/// </summary>
	/// <returns>The culture LCID.</returns>
	protected virtual int GetCultureCore() => GetCultureHelper();

	/// <summary>
	/// Gets the heading level.
	/// </summary>
	/// <returns>The heading level.</returns>
	protected virtual AutomationHeadingLevel GetHeadingLevelCore() => AutomationHeadingLevel.None;

	/// <summary>
	/// Gets whether this element is a dialog.
	/// </summary>
	/// <returns>True if this is a dialog.</returns>
	protected virtual bool IsDialogCore() => false;

	/// <summary>
	/// Gets the peer from point.
	/// </summary>
	/// <param name="point">The point to hit test.</param>
	/// <returns>The automation peer at the specified point.</returns>
	protected virtual AutomationPeer? GetPeerFromPointCore(Point point) => this;

	/// <summary>
	/// Gets the element from point (internal implementation).
	/// This method deprecates GetPeerFromPoint/Core but maintains compatibility.
	/// </summary>
	internal object? GetElementFromPointCoreImpl(Point point) => GetPeerFromPoint(point);

	/// <summary>
	/// Gets the focused element within this peer's scope.
	/// </summary>
	/// <returns>The focused element.</returns>
	protected virtual object? GetFocusedElementCore() => this;

	/// <summary>
	/// Gets the automation peer from a raw element provider.
	/// </summary>
	/// <param name="provider">The raw element provider.</param>
	/// <returns>The automation peer associated with the provider.</returns>
	protected AutomationPeer? PeerFromProvider(IRawElementProviderSimple provider)
	{
		// In Uno/WinUI, the provider usually holds a reference to the peer
		// or we cast the provider if it is implemented by the peer itself.
		return provider?.AutomationPeer;
	}

	/// <summary>
	/// Gets a raw element provider from an automation peer (internal implementation).
	/// </summary>
	internal IRawElementProviderSimple? ProviderFromPeerImpl(AutomationPeer? automationPeer)
	{
		if (automationPeer is null)
		{
			return null;
		}

		// In C# / Uno, the AutomationPeer usually implements IRawElementProviderSimple directly.
		if (automationPeer is IRawElementProviderSimple provider)
		{
			return provider;
		}

		// If a wrapper is strictly required by the architecture, create one here.
		return new IRawElementProviderSimple(automationPeer);
	}

	/// <summary>
	/// Generates the automation peer events source for list-like items.
	/// We only want to generate EventsSource for FrameworkElementAPs;
	/// for others it's the responsibility of the app author to set one during creation of object.
	/// </summary>
	private void GenerateAutomationPeerEventsSource(AutomationPeer? parent)
	{
		if (this is FrameworkElementAutomationPeer)
		{
			ItemsControlAutomationPeer.GenerateAutomationPeerEventsSourceStatic(
				(FrameworkElementAutomationPeer)this,
				parent);
		}
	}

	/// <summary>
	/// Notify corresponding core object about managed owner (UI) being dead.
	/// </summary>
	internal static void NotifyManagedUIElementIsDead(AutomationPeer? automationPeer)
	{
		// In Uno, this is handled via weak references or Dispose logic.
		// This method exists for API compatibility.
	}

	/// <summary>
	/// Raises the property changed event for the specified peer.
	/// </summary>
	private static void RaisePropertyChangedEventById(AutomationPeer? peer, AutomationProperty propertyId, string? oldValue, string? newValue)
	{
		peer?.RaisePropertyChangedEvent(propertyId, oldValue, newValue);
	}

	/// <summary>
	/// Raises the property changed event for the specified peer.
	/// </summary>
	private static void RaisePropertyChangedEventById(AutomationPeer? peer, AutomationProperty propertyId, bool oldValue, bool newValue)
	{
		peer?.RaisePropertyChangedEvent(propertyId, oldValue, newValue);
	}

	/// <summary>
	/// Gets the described by peers.
	/// </summary>
	/// <returns>An enumerable of peers that describe this element.</returns>
	public IEnumerable<AutomationPeer>? GetDescribedBy() => GetDescribedByCore();

	/// <summary>
	/// Gets the flows to peers.
	/// </summary>
	/// <returns>An enumerable of peers this element flows to.</returns>
	public IEnumerable<AutomationPeer>? GetFlowsTo() => GetFlowsToCore();

	/// <summary>
	/// Gets the flows from peers.
	/// </summary>
	/// <returns>An enumerable of peers that flow to this element.</returns>
	public IEnumerable<AutomationPeer>? GetFlowsFrom() => GetFlowsFromCore();

	/// <summary>
	/// Raises the structure changed event.
	/// </summary>
	/// <param name="structureChangeType">The type of structure change.</param>
	/// <param name="child">The child peer involved in the change (required for ChildRemoved).</param>
	public void RaiseStructureChangedEvent(AutomationStructureChangeType structureChangeType, AutomationPeer? child = null)
	{
#if HAS_UNO
		// TODO Uno: Implement RaiseStructureChangedEvent when UIA infrastructure is available.
		// For now this is a stub that maintains API compatibility.

		if (structureChangeType == AutomationStructureChangeType.ChildRemoved)
		{
			if (child is null)
			{
				throw new ArgumentNullException(nameof(child));
			}
			// UIAutomationCore expects runtime id of the removed child to be returned.
			_ = child.GetRuntimeId();
		}
#endif
	}

	/// <summary>
	/// Helper: Raises the event if there are listeners for it.
	/// </summary>
	internal static void RaiseEventIfListener(UIElement? element, AutomationEvents eventId)
	{
		if (element is not null && ListenerExists(eventId))
		{
			var peer = FrameworkElementAutomationPeer.FromElement(element);
			peer?.RaiseAutomationEvent(eventId);
		}
	}
}
