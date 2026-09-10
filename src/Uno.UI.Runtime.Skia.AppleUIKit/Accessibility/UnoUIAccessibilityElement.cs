#nullable enable

using System;
using Accessibility;
using CoreGraphics;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace Uno.UI.Runtime.Skia.AppleUIKit;

/// <summary>
/// Stable managed <see cref="UIAccessibilityElement"/> for a Skia iOS/tvOS/macCatalyst node.
/// All accessibility property getters pull from the live automation peer on demand through a
/// weak adapter reference, so no semantic snapshot is cached here.
/// </summary>
internal sealed class UnoUIAccessibilityElement : UIAccessibilityElement, IAXCustomContentProvider
{
	private readonly nint _nodeId;
	private readonly WeakReference<AppleUIKitAccessibility> _adapterRef;
	private AXCustomContent[]? _customContent;
	private UIAccessibilityCustomAction[]? _customActions;
	private bool _customContentInitialized;
	private bool _customActionsInitialized;

	/// <summary>
	/// When true, VoiceOver treats this element as a modal container and ignores sibling
	/// elements outside it. Set by the adapter when an active modal subtree is detected.
	/// </summary>
	internal bool IsModalContainer { get; set; }

	internal UnoUIAccessibilityElement(
		NSObject container,
		nint nodeId,
		WeakReference<AppleUIKitAccessibility> adapterRef)
		: base(container)
	{
		_nodeId = nodeId;
		_adapterRef = adapterRef;
		IsAccessibilityElement = true;
	}

	/// <summary>The stable node identity used by the adapter registry.</summary>
	internal nint NodeId => _nodeId;

	// Pull properties

	public override string? AccessibilityLabel
		=> _adapterRef.TryGetTarget(out var a) ? a.GetLabel(_nodeId) : null;

	public override string? AccessibilityHint
		=> _adapterRef.TryGetTarget(out var a) ? a.GetHint(_nodeId) : null;

	public override string? AccessibilityValue
		=> _adapterRef.TryGetTarget(out var a) ? a.GetValue(_nodeId) : null;

	public override ulong AccessibilityTraits
		=> (ulong)(_adapterRef.TryGetTarget(out var a) ? a.GetTraits(_nodeId) : UIAccessibilityTrait.None);

	public override string? AccessibilityIdentifier
		=> _adapterRef.TryGetTarget(out var a) ? a.GetIdentifier(_nodeId) : null;

	[Export("accessibilityLanguage")]
	public string? AccessibilityLanguage
		=> _adapterRef.TryGetTarget(out var a) ? a.GetLanguage(_nodeId) : null;

	public AXCustomContent[]? AccessibilityCustomContent
	{
		get
		{
			if (AccessibilityCustomContentHandler is { } handler)
			{
				return handler();
			}

			if (!_customContentInitialized)
			{
				_customContent = _adapterRef.TryGetTarget(out var adapter)
					? adapter.GetCustomContent(_nodeId)
					: null;
				_customContentInitialized = true;
			}

			return _customContent;
		}
		set
		{
			_customContent = value;
			_customContentInitialized = true;
		}
	}

	public Func<AXCustomContent[]>? AccessibilityCustomContentHandler { get; set; }

	public override CGRect AccessibilityFrameInContainerSpace
		=> _adapterRef.TryGetTarget(out var a) ? a.GetFrameInContainerSpace(_nodeId) : CGRect.Empty;

	// Actions

	[Export("accessibilityActivate")]
	public bool AccessibilityActivate()
		=> _adapterRef.TryGetTarget(out var a) && a.Activate(_nodeId);

	public override void AccessibilityIncrement()
	{
		if (_adapterRef.TryGetTarget(out var a))
		{
			a.Increment(_nodeId);
		}
	}

	public override void AccessibilityDecrement()
	{
		if (_adapterRef.TryGetTarget(out var a))
		{
			a.Decrement(_nodeId);
		}
	}

	/// <summary>
	/// VoiceOver-invoked scroll action. Mapped from UIKit's accessibilityScroll: informal
	/// protocol selector, which is on UIResponder but reachable via ObjC messaging on any
	/// NSObject subclass when exported.
	/// </summary>
	public override bool AccessibilityScroll(UIAccessibilityScrollDirection direction)
		=> _adapterRef.TryGetTarget(out var a) && a.Scroll(_nodeId, direction);

	/// <summary>
	/// VoiceOver escape gesture (two-finger Z scrub). Calls the adapter's PerformEscape,
	/// which maps to IWindowProvider.Close or returns false when no Window pattern exists.
	/// </summary>
	public override bool AccessibilityPerformEscape()
		=> _adapterRef.TryGetTarget(out var a) && a.PerformEscape(_nodeId);

	/// <summary>
	/// Returns the currently-valid custom action list for VoiceOver's long-press menu.
	/// The list reflects the peer's live state (e.g., expand vs. collapse, read-only check).
	/// Called by both VoiceOver (via ObjC messaging) and the test action accessor.
	/// </summary>
	public override UIAccessibilityCustomAction[]? AccessibilityCustomActions
	{
		get
		{
			if (!_customActionsInitialized)
			{
				_customActions = _adapterRef.TryGetTarget(out var adapter)
					? adapter.GetCustomActions(_nodeId)
					: null;
				_customActionsInitialized = true;
			}

			return _customActions;
		}
		set
		{
			_customActions = value;
			_customActionsInitialized = true;
		}
	}

	internal void InvalidateCachedAccessibilityData()
	{
		_customContent = null;
		_customContentInitialized = false;
		_customActions = null;
		_customActionsInitialized = false;
	}

	// Modal containment

	/// <summary>
	/// Instructs VoiceOver to ignore sibling accessibility elements when this element is
	/// the active modal container. Mirrors UIView.accessibilityViewIsModal on NSObject.
	/// </summary>
	[Export("accessibilityViewIsModal")]
	public bool AccessibilityViewIsModal => IsModalContainer;

	// Focus notifications

	/// <summary>
	/// Called by VoiceOver (via the UIAccessibilityFocus informal protocol) when this
	/// element receives accessibility focus. Notifies the adapter so it can update XAML
	/// focus without creating a feedback loop.
	/// </summary>
	public override void AccessibilityElementDidBecomeFocused()
	{
		if (_adapterRef.TryGetTarget(out var adapter))
		{
			adapter.OnNativeElementFocused(_nodeId);
		}
	}

	/// <summary>
	/// Called by VoiceOver when this element loses accessibility focus.
	/// </summary>
	public override void AccessibilityElementDidLoseFocus()
	{
		if (_adapterRef.TryGetTarget(out var adapter))
		{
			adapter.OnNativeElementLostFocus(_nodeId);
		}
	}
}
