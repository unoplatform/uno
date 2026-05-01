// MUX Reference dxaml\xcp\dxaml\lib\ToolTipService_Partial.cpp, tag 5f9e85113
// Contains ported portions of dxaml\xcp\dxaml\lib\ToolTipService_Partial.cpp
//
// NOTE: ToolTipServiceMetadata + static fields live in ToolTipService.partial.h.mux.cs
// (port of ToolTipService_Partial.h). This file ports method bodies in the order
// they appear in ToolTipService_Partial.cpp.

#if __SKIA__

#nullable enable

using DirectUI;

namespace Microsoft.UI.Xaml.Controls;

public partial class ToolTipService
{
#pragma warning disable IDE0051 // Remove unused private members (placeholder until later phases)
#pragma warning disable IDE0060 // Remove unused parameter (placeholder)

	// MUX Reference: ToolTipService_Partial.cpp ToolTipServiceMetadata constructor (line 28).
	// Phase 6 will activate the PowerSettingRegisterNotification display-state hook.
	// For now the metadata is created lazily on first access via EnsureToolTipServiceMetadata.

	private static ToolTipServiceMetadata? s_toolTipServiceMetadata;

	internal static ToolTipServiceMetadata GetToolTipServiceMetadata()
	{
		s_toolTipServiceMetadata ??= new ToolTipServiceMetadata();
		return s_toolTipServiceMetadata;
	}

	// MUX Reference: ToolTipService_Partial.cpp RegisterToolTip (line 110).
	internal static void RegisterToolTip(
		DependencyObject pOwner,
		FrameworkElement pContainer,
		object pToolTipAsIInspectable,
		bool isKeyboardAcceleratorToolTip)
	{
		global::System.Diagnostics.Debug.Assert(pOwner is not null, "ToolTip must have an owner");
		global::System.Diagnostics.Debug.Assert(pContainer is not null, "ToolTip must have an container");
		global::System.Diagnostics.Debug.Assert(pToolTipAsIInspectable is not null, "ToolTip can not be null");

		bool inputEventsAlreadyHookedUp = false;

		ToolTip? spToolTipObject;
		if (isKeyboardAcceleratorToolTip)
		{
			spToolTipObject = GetKeyboardAcceleratorToolTipObject(pOwner);
		}
		else
		{
			spToolTipObject = GetToolTipReference(pOwner);
		}

		if (spToolTipObject is not null)
		{
			inputEventsAlreadyHookedUp = spToolTipObject.m_bInputEventsHookedUp;
		}

		// Set the tooltip before applying the delegates, otherwise the owner
		// will try to call into the tool tip services.
		var spIToolTip = ConvertToToolTip(pToolTipAsIInspectable);

		var pToolTipNoRef = spIToolTip;
		pToolTipNoRef.SetOwner(pOwner);
		pToolTipNoRef.SetContainer(pContainer);

		if (isKeyboardAcceleratorToolTip)
		{
			SetKeyboardAcceleratorToolTipObject(pOwner, pToolTipNoRef);
		}
		else
		{
			SetToolTipReference(pOwner, pToolTipNoRef);
		}

		// If the owner is also the container, then we'll want to attach pointer events,
		// since nothing will be already listening to pointer events for us.
		if (ReferenceEquals(pOwner, pContainer) && !inputEventsAlreadyHookedUp)
		{
			var pOwnerAsFENoRef = (FrameworkElement)pOwner;

			pOwnerAsFENoRef.PointerEntered += OnOwnerPointerEntered;
			pToolTipNoRef.m_ownerPointerEnteredToken.Disposable = global::Uno.Disposables.Disposable.Create(
				() => pOwnerAsFENoRef.PointerEntered -= OnOwnerPointerEntered);

			pOwnerAsFENoRef.PointerExited += OnOwnerPointerExitedOrLostOrCanceled;
			pToolTipNoRef.m_ownerPointerExitedToken.Disposable = global::Uno.Disposables.Disposable.Create(
				() => pOwnerAsFENoRef.PointerExited -= OnOwnerPointerExitedOrLostOrCanceled);

			pOwnerAsFENoRef.PointerCaptureLost += OnOwnerPointerExitedOrLostOrCanceled;
			pToolTipNoRef.m_ownerPointerCaptureLostToken.Disposable = global::Uno.Disposables.Disposable.Create(
				() => pOwnerAsFENoRef.PointerCaptureLost -= OnOwnerPointerExitedOrLostOrCanceled);

			pOwnerAsFENoRef.PointerCanceled += OnOwnerPointerExitedOrLostOrCanceled;
			pToolTipNoRef.m_ownerPointerCanceledToken.Disposable = global::Uno.Disposables.Disposable.Create(
				() => pOwnerAsFENoRef.PointerCanceled -= OnOwnerPointerExitedOrLostOrCanceled);

			pOwnerAsFENoRef.GotFocus += OnOwnerGotFocus;
			pToolTipNoRef.m_ownerGotFocusToken.Disposable = global::Uno.Disposables.Disposable.Create(
				() => pOwnerAsFENoRef.GotFocus -= OnOwnerGotFocus);

			pOwnerAsFENoRef.LostFocus += OnOwnerLostFocus;
			pToolTipNoRef.m_ownerLostFocusToken.Disposable = global::Uno.Disposables.Disposable.Create(
				() => pOwnerAsFENoRef.LostFocus -= OnOwnerLostFocus);

			pToolTipNoRef.m_bInputEventsHookedUp = true;
		}
	}

	// MUX Reference: ToolTipService_Partial.cpp UnregisterToolTip (line 251).
	internal static void UnregisterToolTip(
		DependencyObject pOwner,
		FrameworkElement pContainer,
		bool isKeyboardAcceleratorToolTip)
	{
		global::System.Diagnostics.Debug.Assert(pOwner is not null, "owner element is required");
		global::System.Diagnostics.Debug.Assert(pContainer is not null, "container element is required");

		ToolTip? spToolTipObject;
		if (isKeyboardAcceleratorToolTip)
		{
			spToolTipObject = GetKeyboardAcceleratorToolTipObject(pOwner);
		}
		else
		{
			spToolTipObject = GetToolTipReference(pOwner);
		}

		var spToolTipObjectConcrete = spToolTipObject;
		global::System.Diagnostics.Debug.Assert(spToolTipObjectConcrete is not null);
		if (spToolTipObjectConcrete is null)
		{
			return;
		}

		if (spToolTipObjectConcrete.m_bInputEventsHookedUp)
		{
			spToolTipObjectConcrete.m_ownerPointerEnteredToken.Disposable = null;
			spToolTipObjectConcrete.m_ownerPointerExitedToken.Disposable = null;
			spToolTipObjectConcrete.m_ownerPointerCaptureLostToken.Disposable = null;
			spToolTipObjectConcrete.m_ownerPointerCanceledToken.Disposable = null;
			spToolTipObjectConcrete.m_ownerGotFocusToken.Disposable = null;
			spToolTipObjectConcrete.m_ownerLostFocusToken.Disposable = null;
		}

		spToolTipObjectConcrete.SetOwner(null);
		spToolTipObjectConcrete.SetContainer(null);

		// Close the ToolTip if it's open, or cancel it from opening if it's in the process of opening
		OnOwnerLeaveInternal(pOwner);

		var toolTipProperty = isKeyboardAcceleratorToolTip
			? KeyboardAcceleratorToolTipObjectProperty
			: ToolTipReferenceProperty;
		pOwner.ClearValue(toolTipProperty);
	}

	// MUX Reference: ToolTipService_Partial.cpp GetActualToolTipObjectStatic (line 302).
	// Renamed to GetActualToolTipObject to match Uno call sites that already exist
	// (DXamlTestHooks.cs etc.).
	internal static ToolTip? GetActualToolTipObject(DependencyObject element)
	{
		// Try to get the actual public tooltip object
		var toolTip = GetToolTipReference(element);

		// If public tooltip doesn't exist, then look for keyboard accelerator tooltip.
		if (toolTip is null)
		{
			toolTip = GetKeyboardAcceleratorToolTipObject(element);
		}

		return toolTip;
	}

	// MUX Reference: ToolTipService_Partial.cpp ConvertToToolTip (later in file, ~line 800).
	// Wraps a content object in a ToolTip if it isn't already one. Also handles the case
	// where the object is already parented to a ToolTip (returns that parent).
	private static ToolTip ConvertToToolTip(object objectIn)
	{
		if (objectIn is not ToolTip toolTip)
		{
			if (objectIn is FrameworkElement)
			{
				var objectInParent = (objectIn as DependencyObject)?.GetParent();
				if (objectInParent is ToolTip parentToolTip)
				{
					return parentToolTip;
				}
			}

			toolTip = new ToolTip
			{
				Content = objectIn
			};
		}

		return toolTip;
	}

	// MUX Reference: ToolTipService_Partial.cpp OnToolTipChanged (later in file, ~line 525).
	// The cross-platform OnToolTipChanged in ToolTipService.mux.cs (gated #if !__SKIA__)
	// dispatches the same way.
	private static void OnToolTipChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		if (sender is FrameworkElement senderAsFe)
		{
			bool isKeyboardAcceleratorToolTip = args.Property == KeyboardAcceleratorToolTipProperty;
			if (args.OldValue is not UnsetValue && args.OldValue is not null)
			{
				UnregisterToolTip(sender, senderAsFe, isKeyboardAcceleratorToolTip);
			}

			if (args.NewValue is { } toolTip)
			{
				RegisterToolTip(sender, senderAsFe, toolTip, isKeyboardAcceleratorToolTip);
			}
		}
	}

	private static void OnPlacementChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		// TODO Uno: Phase 3 closeout will mirror the cross-platform Placement-update behavior
		// (propagate to the registered ToolTip's Placement).
	}

	// MUX Reference: ToolTipService_Partial.cpp EnsureHandlersAttachedToRootElement (later in file).
	// Phase 4 (pointer + focus event handling) will port this faithfully. Currently a stub
	// so OpenPopup compiles.
	internal static void EnsureHandlersAttachedToRootElement(XamlRoot? visualTree)
	{
		// TODO Uno: Phase 4 will port EnsureHandlersAttachedToRootElement.
	}

	// === Phase 4 stubs (pointer + focus + leave handlers) ===

	// MUX Reference: ToolTipService_Partial.cpp OnOwnerPointerEntered (later in file).
	private static void OnOwnerPointerEntered(object sender, Input.PointerRoutedEventArgs e)
	{
		// TODO Uno: Phase 4 will port OnOwnerPointerEntered.
	}

	// MUX Reference: ToolTipService_Partial.cpp OnOwnerPointerExitedOrLostOrCanceled (later in file).
	private static void OnOwnerPointerExitedOrLostOrCanceled(object sender, Input.PointerRoutedEventArgs e)
	{
		// TODO Uno: Phase 4 will port OnOwnerPointerExitedOrLostOrCanceled.
	}

	// MUX Reference: ToolTipService_Partial.cpp OnOwnerGotFocus (later in file).
	private static void OnOwnerGotFocus(object sender, RoutedEventArgs e)
	{
		// TODO Uno: Phase 4 will port OnOwnerGotFocus.
	}

	// MUX Reference: ToolTipService_Partial.cpp OnOwnerLostFocus (later in file).
	private static void OnOwnerLostFocus(object sender, RoutedEventArgs e)
	{
		// TODO Uno: Phase 4 will port OnOwnerLostFocus.
	}

	// MUX Reference: ToolTipService_Partial.cpp OnOwnerLeaveInternal (later in file).
	private static void OnOwnerLeaveInternal(object pSender)
	{
		// TODO Uno: Phase 4 will port OnOwnerLeaveInternal (closes/cancels the tooltip when the owner leaves).
	}

#pragma warning restore IDE0060
#pragma warning restore IDE0051
}

// Phase 0 scaffolding: Slider.mux.cs calls ToolTipPositioning.IsLefthandedUser().
// Full port lives under ToolTipService_Partial.h (lines 331-435) and
// ToolTipService_Partial.cpp; will be reconciled in Phase 5.
internal static class ToolTipPositioning
{
	internal static bool IsLefthandedUser() => false;
}

#endif // __SKIA__