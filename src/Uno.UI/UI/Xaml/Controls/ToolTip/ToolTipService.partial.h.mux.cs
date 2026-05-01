// MUX Reference dxaml\xcp\dxaml\lib\ToolTipService_Partial.h, tag 5f9e85113
// Contains ported field/constant declarations from ToolTipService_Partial.h

#if __SKIA__

#nullable enable

using System;
using System.Collections.Generic;
using DirectUI;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

// Timer constants for showing/hiding ToolTips.
// The default values for mouse delay and show duration are only used if the calls to fetch those SystemParameters fail.
//  NOTE: GetTickCount() actually measures time in millseconds, but wf::TimeSpan expects ticks.
//  NOTE: 1 Tick == 100 ns == 0.1 us == 0.0001 ms
internal static class ToolTipServiceConstants
{
	internal const int BETWEEN_SHOW_DELAY_MS = 200;             // 0.2 seconds, in milliseconds
	internal const int DEFAULT_SPI_GETMOUSEHOVERTIME = 400;     // 0.4 seconds, in milliseconds
	internal const int DEFAULT_SHOW_DURATION_SECONDS = 5;       // 5 seconds
	internal const int TICKS_PER_MILLISECOND = 10000;           // Number of ticks in a millisecond
	internal const long s_safeZoneCheckTimerDuration = 10000000L; // 1s
}

// MUX Reference: ToolTipService_Partial.h ToolTipServiceMetadata class (line 28).
// Singleton-style metadata holder for the ToolTipService. WinUI tracks one
// instance per DXamlCore; Uno does the same via a static field on ToolTipService
// (created lazily on first access).
internal class ToolTipServiceMetadata
{
#pragma warning disable CS0067
#pragma warning disable CS0169
#pragma warning disable CS0414
#pragma warning disable CS0649
#pragma warning disable IDE0044
#pragma warning disable IDE0051
#pragma warning disable IDE0052

	// Display event is hooked up to avoid SafeZoneCheckTimer introduced performance issue when display is off
	internal bool m_displayOn = true;

	internal DependencyObject? m_tpOwner;
	internal FrameworkElement? m_tpContainer;
	internal ToolTip? m_tpCurrentToolTip;
	// internal IDispatcherQueueTimer? m_tpSafeZoneCheckTimer; // TODO Uno: Phase 6 (DispatcherQueueTimer)

	// ToolTipService puts size change and mouse move handlers on the roots elements of each XamlRoot
	// (i.e. RootVisual & Xaml islands). This collection tracks whether a root has handlers on it already.
	internal readonly List<UIElement> m_rootElementsWithHandlersNoRef = new List<UIElement>();

	// We keep a strong ref to the Popup of the current ToolTip.  Silverlight's ToolTip has its own
	// Popup member, but this creates a circular dependency in C++ when releasing ToolTip, which we can
	// evade by tracking the Popup in ToolTipServiceMetadata instead.
	internal Popup? m_tpCurrentPopup;

	internal object? m_tpLastEnterSource;
	internal DispatcherTimer? m_tpOpenTimer;
	internal DispatcherTimer? m_tpCloseTimer;

	// List of nested owners. Owner is added to the list when entered by the pointer, and
	// removed when left by the pointer. This list supports the pointer coming back to an
	// ancestor after it enters and leaves a nested descendant element, without ever
	// leaving the ancestor.
	internal LinkedList<WeakReference>? m_nestedOwners;

	// Because of Safe Zone, ToolTip is kept even if pointer is out side of the control.
	// We need to remove it from m_nestedOwners when toolTip is closed
	internal WeakReference? m_lastToolTipOwnerInSafeZone;

	// crashes is found in RemoveFromNestedOwners/PurgeInvalidNestedOwners
	// and m_nestedOwners is HEAP CORRUPTION or element in it is empty.
	// One possible reason is nested erase. When we erase one element, it trig another erase
	// and finally make the external iterator invalid.
	// m_isErasingNestedOwners is used to help detect the nested erase.
	internal bool m_isErasingNestedOwners = false;

	// Boolean variables used to catch circumstances where we shouldn't
	// perform any list operations immediately - we'll cache any requests
	// to do so in the vectors below to be done once the current operation completes.
	internal bool m_isAddingToNestedOwners = false;
	internal bool m_isRemovingFromNestedOwners = false;
	internal bool m_isPurgingInvalidNestedOwners = false;

	internal readonly List<WeakReference> m_objectsToAdd = new List<WeakReference>();
	internal readonly List<WeakReference> m_objectsToRemove = new List<WeakReference>();

#pragma warning restore IDE0052
#pragma warning restore IDE0051
#pragma warning restore IDE0044
#pragma warning restore CS0649
#pragma warning restore CS0414
#pragma warning restore CS0169
#pragma warning restore CS0067

	// Phase 1 scaffolding constructor — actual port of ToolTipServiceMetadata::ToolTipServiceMetadata
	// (including PowerSettingRegisterNotification for display state) lands in Phase 6.
	internal ToolTipServiceMetadata()
	{
	}

	// Accessors below match the C++ inline accessors (lines 88-119).

	internal void SetCurrentToolTip(ToolTip? value)
	{
		m_tpCurrentToolTip = value;
	}

	internal void SetCurrentPopup(Popup? value)
	{
		m_tpCurrentPopup = value;
	}

	internal void SetCloseTimer(DispatcherTimer? value)
	{
		m_tpCloseTimer = value;
	}

	internal void SetOpenTimer(DispatcherTimer? value)
	{
		m_tpOpenTimer = value;
	}

	internal void SetOwner(DependencyObject? value)
	{
		m_tpOwner = value;
	}

	internal void SetContainer(FrameworkElement? value)
	{
		m_tpContainer = value;
	}

	internal void SetLastEnterSource(object? value)
	{
		m_tpLastEnterSource = value;
	}

	// MUX Reference: ToolTipService_Partial.cpp ToolTipServiceMetadata::EnsureNestedOwnersInstance (line 40).
	internal void EnsureNestedOwnersInstance()
	{
		if (m_nestedOwners is null)
		{
			m_nestedOwners = new LinkedList<WeakReference>();
		}
	}

	// MUX Reference: ToolTipService_Partial.cpp ToolTipServiceMetadata::DeleteElementFromNestedOwners (line 49).
	// after delete, it automatically moved to next element.
	internal LinkedListNode<WeakReference>? DeleteElementFromNestedOwners(LinkedListNode<WeakReference> node)
	{
		if (m_isErasingNestedOwners)
		{
			// nested erase is detected and we don't expect it. so throw E_UNEXPECTED exception
			throw new global::System.InvalidOperationException("Nested erase detected in ToolTipServiceMetadata.m_nestedOwners.");
		}

		m_isErasingNestedOwners = true;
		var next = node.Next;
		m_nestedOwners!.Remove(node);
		m_isErasingNestedOwners = false;
		return next;
	}
}

// MUX Reference: ToolTipService_Partial.h ToolTipService class (line 129).
// Service class that provides the system implementation for displaying ToolTips.
public partial class ToolTipService
{
#pragma warning disable CS0414
#pragma warning disable CS0649
#pragma warning disable IDE0051

	internal static bool s_bOpeningAutomaticToolTip = false;
	internal static AutomaticToolTipInputMode s_lastEnterInputMode = AutomaticToolTipInputMode.None;
	internal static Point s_lastPointerEnteredPoint = default;

	// Keyboard opens tooltip too, and most of time, pointer is out of safe zone.
	// we should not start the safe zone check until Pointer is moved
	internal static Point s_pointerPointWhenSafeZoneTimerStart = default;

	// Ticks since last open.
	private static long s_lastToolTipOpenedTime = 0;

#pragma warning restore IDE0051
#pragma warning restore CS0649
#pragma warning restore CS0414
}

#endif // __SKIA__