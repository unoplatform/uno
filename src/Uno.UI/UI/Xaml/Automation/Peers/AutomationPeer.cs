// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable
#pragma warning disable CS8604 // Possible null reference argument
#pragma warning disable IDE0051 // Remove unused private members

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Automation.Provider;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Provides a base class that exposes an automation peer for an associated owner class to UI Automation.
/// </summary>
public partial class AutomationPeer : DependencyObject
{
	private AutomationPeer? _parent;

	/// <summary>
	/// Gets a value indicating whether there are any listeners for the specified automation event.
	/// </summary>
	/// <param name="eventId">The automation event to check for listeners.</param>
	/// <returns>True if there are listeners for the event.</returns>
	public static bool ListenerExists(AutomationEvents eventId) => ListenerExistsHelper(eventId);

	#region Public Properties

	/// <summary>
	/// Gets or sets an AutomationPeer that is reported to the automation client as a source for all the events from this AutomationPeer.
	/// </summary>
	public AutomationPeer? EventsSource { get; set; }

	#endregion

	#region Public Methods

	/// <summary>
	/// Gets the control pattern that is associated with the specified PatternInterface.
	/// </summary>
	/// <param name="patternInterface">The pattern identifier.</param>
	/// <returns>The object implementing the pattern interface, or null if the pattern is not supported.</returns>
	public object? GetPattern(PatternInterface patternInterface) => GetPatternCore(patternInterface);

	/// <summary>
	/// Sets the AutomationPeer that is the parent of this AutomationPeer.
	/// </summary>
	/// <param name="peer">The parent peer.</param>
	public void SetParent(AutomationPeer? peer) => _parent = peer;

	/// <summary>
	/// Gets the AutomationPeer that is the parent of this AutomationPeer.
	/// </summary>
	/// <returns>The parent peer.</returns>
	public AutomationPeer? GetParent() => _parent;

	/// <summary>
	/// Gets the accelerator key combination for the element that is associated with the UI Automation peer.
	/// </summary>
	/// <returns>The accelerator key string.</returns>
	public string GetAcceleratorKey() => GetAcceleratorKeyCore();

	/// <summary>
	/// Gets the access key for the element associated with the automation peer.
	/// </summary>
	/// <returns>The access key string.</returns>
	public string GetAccessKey() => GetAccessKeyCore();

	/// <summary>
	/// Gets the automation identifier for the element associated with the UI Automation peer.
	/// </summary>
	/// <returns>The automation ID string.</returns>
	public string GetAutomationId() => GetAutomationIdCore();

	/// <summary>
	/// Gets the bounding rectangle coordinates of the element that is associated with the UI Automation peer.
	/// </summary>
	/// <returns>The bounding rectangle.</returns>
	public Rect GetBoundingRectangle() => GetBoundingRectangleCore();

	/// <summary>
	/// Gets a clickable point on the element that is associated with the automation peer.
	/// </summary>
	/// <returns>A clickable point.</returns>
	public Point GetClickablePoint() => GetClickablePointCore();

	/// <summary>
	/// Gets text that describes the element for which the AutomationPeer is created.
	/// </summary>
	/// <returns>The help text string.</returns>
	public string GetHelpText() => GetHelpTextCore();

	/// <summary>
	/// Gets a string that conveys the visual status of the element.
	/// </summary>
	/// <returns>The item status string.</returns>
	public string GetItemStatus() => GetItemStatusCore();

	/// <summary>
	/// Gets a localized string that describes the type of item that the element represents.
	/// </summary>
	/// <returns>The item type string.</returns>
	public string GetItemType() => GetItemTypeCore();

	/// <summary>
	/// Gets the orientation of the control.
	/// </summary>
	/// <returns>The orientation.</returns>
	public AutomationOrientation GetOrientation() => GetOrientationCore();

	/// <summary>
	/// Gets a value that indicates whether the element has keyboard focus.
	/// </summary>
	/// <returns>True if the element has keyboard focus.</returns>
	public bool HasKeyboardFocus() => HasKeyboardFocusCore();

	/// <summary>
	/// Gets a value that indicates whether the element can accept keyboard focus.
	/// </summary>
	/// <returns>True if the element can accept keyboard focus.</returns>
	public bool IsKeyboardFocusable() => IsKeyboardFocusableCore();

	/// <summary>
	/// Gets a value that indicates whether the element is off the screen.
	/// </summary>
	/// <returns>True if the element is offscreen.</returns>
	public bool IsOffscreen() => IsOffscreenCore();

	/// <summary>
	/// Gets a value that indicates whether the element is required to be completed on a form.
	/// </summary>
	/// <returns>True if required for form.</returns>
	public bool IsRequiredForForm() => IsRequiredForFormCore();

	/// <summary>
	/// Gets the live setting notification behavior info for the object.
	/// </summary>
	/// <returns>The live setting value.</returns>
	public AutomationLiveSetting GetLiveSetting() => GetLiveSettingCore();

	/// <summary>
	/// Gets the AutomationPeer for the specified point.
	/// </summary>
	/// <param name="direction">The navigation direction.</param>
	/// <returns>The peer in the specified direction, or null if there is no peer in that direction.</returns>
	public object? Navigate(AutomationNavigationDirection direction) => NavigateCore(direction);

	/// <summary>
	/// Gets an element from a specified point.
	/// </summary>
	/// <param name="pointInWindowCoordinates">The point.</param>
	/// <returns>The element at the specified point.</returns>
	public object? GetElementFromPoint(Point pointInWindowCoordinates) => GetElementFromPointCoreImpl(pointInWindowCoordinates);

	/// <summary>
	/// Gets the element that currently has the focus.
	/// </summary>
	/// <returns>The focused element.</returns>
	public object? GetFocusedElement() => GetFocusedElementCore();

	/// <summary>
	/// Gets the collection of annotation objects associated with this element.
	/// </summary>
	/// <returns>A list of annotations.</returns>
	public IList<AutomationPeerAnnotation>? GetAnnotations() => GetAnnotationsCore();

	/// <summary>
	/// Gets the ordinal position within a set, or 0 if not in a set.
	/// </summary>
	/// <returns>The position in set.</returns>
	public int GetPositionInSet() => GetPositionInSetCore();

	/// <summary>
	/// Gets the number of items within a set.
	/// </summary>
	/// <returns>The size of set.</returns>
	public int GetSizeOfSet() => GetSizeOfSetCore();

	/// <summary>
	/// Gets the level of the element in hierarchical or broken hierarchical structures.
	/// </summary>
	/// <returns>The level.</returns>
	public int GetLevel() => GetLevelCore();

	/// <summary>
	/// Gets the landmark type for this element.
	/// </summary>
	/// <returns>The landmark type.</returns>
	public AutomationLandmarkType GetLandmarkType() => GetLandmarkTypeCore();

	/// <summary>
	/// Gets the localized landmark type.
	/// </summary>
	/// <returns>The localized landmark type string.</returns>
	public string GetLocalizedLandmarkType() => GetLocalizedLandmarkTypeCore();

	/// <summary>
	/// Gets a value indicating whether this element is for peripheral UI.
	/// </summary>
	/// <returns>True if this is peripheral UI.</returns>
	public bool IsPeripheral() => IsPeripheralCore();

	/// <summary>
	/// Gets a value indicating whether the entered or selected value is valid for the form rule associated with this element.
	/// </summary>
	/// <returns>True if data is valid for form.</returns>
	public bool IsDataValidForForm() => IsDataValidForFormCore();

	/// <summary>
	/// Gets the culture info for this element.
	/// </summary>
	/// <returns>The culture LCID.</returns>
	public int GetCulture() => GetCultureCore();

	/// <summary>
	/// Gets the full description of this element.
	/// </summary>
	/// <returns>The full description string.</returns>
	public string GetFullDescription() => GetFullDescriptionCore();

	/// <summary>
	/// Gets the heading level for this element.
	/// </summary>
	/// <returns>The heading level.</returns>
	public AutomationHeadingLevel GetHeadingLevel() => GetHeadingLevelCore();

	/// <summary>
	/// Gets a value indicating whether the element associated with this automation peer is a dialog window.
	/// </summary>
	/// <returns>True if this is a dialog.</returns>
	public bool IsDialog() => IsDialogCore();

	/// <summary>
	/// Gets a value indicating whether the element is an element that contains data that is presented to the user.
	/// </summary>
	/// <returns>True if this is a content element.</returns>
	public bool IsContentElement() => IsContentElementCore();

	/// <summary>
	/// Gets a value indicating whether the element is an element that is relevant or essential to the user interface.
	/// </summary>
	/// <returns>True if this is a control element.</returns>
	public bool IsControlElement() => IsControlElementCore();

	/// <summary>
	/// Gets a value indicating whether the element is enabled.
	/// </summary>
	/// <returns>True if the element is enabled.</returns>
	public bool IsEnabled() => IsEnabledCore();

	/// <summary>
	/// Gets a value indicating whether the element contains protected content.
	/// </summary>
	/// <returns>True if this is a password field.</returns>
	public bool IsPassword() => IsPasswordCore();

	/// <summary>
	/// Sets the keyboard focus to this element.
	/// </summary>
	public void SetFocus() => SetFocusCore();

	/// <summary>
	/// Gets the class name of the owner type.
	/// </summary>
	/// <returns>The class name string.</returns>
	public string GetClassName() => GetClassNameCore();

	/// <summary>
	/// Gets the control type for the element.
	/// </summary>
	/// <returns>The automation control type.</returns>
	public AutomationControlType GetAutomationControlType() => GetAutomationControlTypeCore();

	/// <summary>
	/// Gets the localized control type.
	/// </summary>
	/// <returns>The localized control type string.</returns>
	public string? GetLocalizedControlType() => GetLocalizedControlTypeCore();

	/// <summary>
	/// Gets the name of the element.
	/// </summary>
	/// <returns>The name string.</returns>
	public string GetName() => GetNameCore();

	/// <summary>
	/// Gets the AutomationPeer for the element that is targeted to the element.
	/// </summary>
	/// <returns>The labeling peer.</returns>
	public AutomationPeer? GetLabeledBy() => GetLabeledByCore();

	/// <summary>
	/// Gets an IRawElementProviderSimple for the specified peer.
	/// </summary>
	/// <param name="peer">The automation peer.</param>
	/// <returns>The raw element provider.</returns>
	protected internal IRawElementProviderSimple ProviderFromPeer(AutomationPeer? peer) => new IRawElementProviderSimple(peer);

	#endregion

	#region Internal Helpers

	// IsKeyboardFocusableHelper and IsOffscreenHelper are implemented in AutomationPeer.mux.cs

	private static string LocalizeControlType(AutomationControlType controlType) =>
		// TODO: Humanize ("AppBarButton" -> "app bar button")
		// TODO: Localize
		Enum.GetName(controlType)?.ToLowerInvariant() ?? "custom";

	/// <summary>
	/// Invokes the default action on the automation peer if supported.
	/// </summary>
	/// <returns>True if the action was invoked.</returns>
	// TODO (DOTI) Not in WinUI?
	internal bool InvokeAutomationPeer()
	{
		// TODO: Add support for ComboBox, Slider, CheckBox, ToggleButton, RadioButton, ToggleSwitch, Selector, etc.
		if (this is IInvokeProvider invokeProvider)
		{
			invokeProvider.Invoke();
			return true;
		}
		else if (this is IToggleProvider toggleProvider)
		{
			toggleProvider.Toggle();
			return true;
		}
		else if (this is ISelectionItemProvider selectionItemProvider)
		{
			selectionItemProvider.Select();
			return true;
		}

		return false;
	}

	#endregion

	#region Core Layer Helper Overrides

	/// <summary>
	/// Core layer helper that returns whether this AutomationPeer is enabled.
	/// </summary>
	/// <returns>True if the peer is enabled.</returns>
	internal bool IsEnabledHelper()
	{
		return IsEnabledCore();
	}

	/// <summary>
	/// Returns whether this AutomationPeer has keyboard focus.
	/// </summary>
	/// <returns>True if the peer has keyboard focus.</returns>
	internal bool HasKeyboardFocusHelper()
	{
		return HasKeyboardFocusCore();
	}

	/// <summary>
	/// Returns whether this element can receive keyboard focus.
	/// Implementation is in AutomationPeer.mux.cs (IsKeyboardFocusableImpl).
	/// </summary>
	/// <returns>True if the element can receive keyboard focus.</returns>
	internal bool IsKeyboardFocusableHelper() => IsKeyboardFocusableImpl();

	/// <summary>
	/// Returns whether this element is offscreen.
	/// Implementation is in AutomationPeer.mux.cs (IsOffscreenImpl).
	/// </summary>
	/// <param name="ignoreClippingOnScrollContentPresenters">Whether to ignore clipping on scroll presenters.</param>
	/// <returns>True if the element is offscreen.</returns>
	internal bool IsOffscreenHelper(bool ignoreClippingOnScrollContentPresenters)
		=> IsOffscreenImpl(ignoreClippingOnScrollContentPresenters);

	/// <summary>
	/// Shows the context menu for this automation peer's element.
	/// </summary>
	internal void ShowContextMenuHelper()
	{
		ShowContextMenuCore();
	}

	#endregion

	#region UIA Infrastructure Methods

	/// <summary>
	/// Initializes this AutomationPeer instance.
	/// </summary>
	public void InitInstance()
	{
		// TODO Uno: Platform-specific initialization for UIA infrastructure.
		// In WinUI, this sets up core automation peer state.
	}

	/// <summary>
	/// Deinitializes the members when the managed UIElement is gone or when this object dies itself.
	/// </summary>
	public void Deinit()
	{
		// TODO Uno: Platform-specific cleanup for UIA infrastructure.
		// In WinUI, this cleans up pattern providers, UIA wrapper, and event handlers.
	}

	/// <summary>
	/// Gets the raw UIA element provider for this automation peer.
	/// </summary>
	/// <returns>The IRawElementProviderSimple, or null if unavailable.</returns>
	public object? GetRawElementProviderSimple()
	{
		// TODO Uno: Return platform-specific UIA provider when available.
		// On platforms without UIAutomation infrastructure, return null.
		return null;
	}

	/// <summary>
	/// Sets the UIA wrapper for this automation peer.
	/// When CUIAWrapper gets created for a given AutomationPeer, it caches itself here for reuse.
	/// </summary>
	public void SetUIAWrapper()
	{
		// TODO Uno: Implement UIA wrapper caching when UIA infrastructure is available.
		// In WinUI, this associates the wrapper as a weak reference with validation patterns.
	}

	#endregion

	#region Managed Value Retrieval Helpers

	/// <summary>
	/// Calls into managed to retrieve an AutomationPeer value.
	/// </summary>
	/// <returns>The automation peer value, or null.</returns>
	public object? GetAutomationPeerAPValueFromManaged()
	{
		// TODO Uno: Implement retrieval from managed automation property values.
		// In WinUI, this bridges between native and managed layers.
		return null;
	}

	/// <summary>
	/// Calls into managed to retrieve a rectangle value.
	/// </summary>
	/// <returns>The rectangle value, or null.</returns>
	public object? GetAutomationPeerRectValueFromManaged()
	{
		// TODO Uno: Implement retrieval from managed automation property values.
		// In WinUI, this retrieves XRECTF from managed properties.
		return null;
	}

	#endregion

	#region Core Virtual Methods

	/// <summary>
	/// Gets the root children of this automation peer.
	/// Walks the root DO tree to return the children APs.
	/// </summary>
	/// <returns>The collection of root child peers, or null.</returns>
	protected virtual object? GetRootChildrenCore()
	{
		// Default implementation returns null; root window peers override this.
		// In WinUI, this is implemented in the root automation peer to walk the visual tree.
		return null;
	}

	#endregion

	#region Error Handling

	/// <summary>
	/// Throws an ElementNotAvailableException for UIA.
	/// </summary>
	public void ThrowElementNotAvailableError()
	{
		throw new InvalidOperationException("UIA element is not available");
	}

	#endregion

	#region NotImplemented / Stubs

	/// <summary>
	/// [Deprecated - typo in name] Gets a value indicating whether there are any listeners for the specified automation event.
	/// </summary>
	[Uno.NotImplemented]
	[Obsolete("Use ListenerExists instead.")]
	public static bool ListenerfExists(AutomationEvents eventId)
	{
		ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Automation.Peers.AutomationPeer", "bool AutomationPeer.ListenerExists");
		return false;
	}

	/// <summary>
	/// Triggers recalculation of the main properties of the AutomationPeer.
	/// </summary>
	public void InvalidatePeer()
	{
		// TODO Uno: Implement InvalidatePeer when UIA infrastructure is available.
		// In WinUI, this triggers an async callback to re-evaluate cached properties.
	}

	/// <summary>
	/// Raises an automation event.
	/// </summary>
	/// <param name="eventId">The event to raise.</param>
	public void RaiseAutomationEvent(AutomationEvents eventId)
	{
#if __SKIA__
		if (ListenerExists(eventId))
		{
			AutomationPeerListener?.NotifyAutomationEvent(this, eventId);
		}
#else
		ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Automation.Peers.AutomationPeer", "void AutomationPeer.RaiseAutomationEvent(AutomationEvents eventId)", LogLevel.Warning);
#endif
	}

	/// <summary>
	/// Initiates a notification event.
	/// </summary>
	/// <param name="notificationKind">The notification kind.</param>
	/// <param name="notificationProcessing">The notification processing hint.</param>
	/// <param name="displayString">The notification string.</param>
	/// <param name="activityId">The activity ID.</param>
	public void RaiseNotificationEvent(AutomationNotificationKind notificationKind, AutomationNotificationProcessing notificationProcessing, string displayString, string activityId)
	{
#if __SKIA__
		// TODO (DOTI): Validate the use of: UIAXcp::AENotification, In docs there is no notifi. only in the source code
		if (ListenerExists(AutomationEvents.Notification))
		{
			AutomationPeerListener?.NotifyNotificationEvent(this, notificationKind, notificationProcessing, displayString, activityId);
		}
#else
		ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Automation.Peers.AutomationPeer", "void AutomationPeer.RaiseNotificationEvent(AutomationNotificationKind notificationKind, AutomationNotificationProcessing notificationProcessing, string displayString, string activityId)", LogLevel.Warning);
#endif
	}

#if !__SKIA__
	/// <summary>
	/// Raises an event to notify the automation client of a changed property value.
	/// </summary>
	/// <param name="automationProperty">The changed property.</param>
	/// <param name="oldValue">The old value.</param>
	/// <param name="newValue">The new value.</param>
	[global::Uno.NotImplemented("__ANDROID__", "__APPLE_UIKIT__", "IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__")]
	public void RaisePropertyChangedEvent(AutomationProperty automationProperty, object? oldValue, object? newValue)
	{
		ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Automation.Peers.AutomationPeer", "void AutomationPeer.RaisePropertyChangedEvent(AutomationProperty automationProperty, object oldValue, object newValue)", LogLevel.Warning);
	}
#endif

	#endregion
}
