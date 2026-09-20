// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference HyperlinkAutomationPeer_Partial.cpp, tag winui3/release/2.4.0, commit e8442d07a

using System;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Documents;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes Hyperlink types to Microsoft UI Automation.
/// </summary>
/// <remarks>
/// Unlike most automation peers, HyperlinkAutomationPeer inherits directly from
/// AutomationPeer (not FrameworkElementAutomationPeer) because Hyperlink is a
/// TextElement (Span), not a UIElement.
/// </remarks>
internal partial class HyperlinkAutomationPeer : AutomationPeer, IInvokeProvider
{
	// Keep a weak ref to the owner; we don't want to keep it alive.
	private readonly WeakReference<Hyperlink> _ownerWeak;

	public HyperlinkAutomationPeer(Hyperlink owner)
	{
		ArgumentNullException.ThrowIfNull(owner);
		_ownerWeak = new WeakReference<Hyperlink>(owner);
	}

	/// <summary>
	/// Gets the owner Hyperlink.
	/// </summary>
	private Hyperlink GetOwner()
	{
		if (_ownerWeak.TryGetTarget(out var owner))
		{
			return owner;
		}

		throw new InvalidOperationException("Owner Hyperlink has been garbage collected.");
	}

	// Used by the Text pattern adapter (TextAdapter.RangeFromChild) to map a link peer back to its
	// owning Hyperlink. Returns null if the owner has been collected.
#nullable enable
	internal Hyperlink? Owner => _ownerWeak.TryGetTarget(out var owner) ? owner : null;
#nullable restore

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Invoke)
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override string GetClassNameCore() => "Hyperlink";

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Hyperlink;

	protected override bool IsContentElementCore()
	{
		var owner = GetOwner();
		var accessibilityView = AutomationProperties.GetAccessibilityView(owner);
		return accessibilityView == AccessibilityView.Content;
	}

	protected override bool IsControlElementCore() => true;

	protected override string GetNameCore()
	{
		var owner = GetOwner();

		// P1: Get AutomationProperties.Name
		var name = AutomationProperties.GetName(owner);
		if (!string.IsNullOrEmpty(name))
		{
			return name;
		}

		// P2: Get Hyperlink content text
		name = owner.GetText();
		if (!string.IsNullOrEmpty(name))
		{
			return name;
		}

		// P3: Get URI
		var uri = owner.NavigateUri;
		if (uri != null)
		{
			return uri.ToString();
		}

		return string.Empty;
	}

	protected override bool IsEnabledCore() => true;

	protected override string GetAcceleratorKeyCore()
	{
		var owner = GetOwner();
		return AutomationProperties.GetAcceleratorKey(owner) ?? string.Empty;
	}

	protected override string GetAccessKeyCore()
	{
		var owner = GetOwner();
		var accessKey = AutomationProperties.GetAccessKey(owner);
		if (!string.IsNullOrEmpty(accessKey))
		{
			return accessKey;
		}

		// Fallback to the AccessKey property on the Hyperlink TextElement
		return owner.AccessKey ?? string.Empty;
	}

	protected override string GetAutomationIdCore()
	{
		var owner = GetOwner();
		var automationId = AutomationProperties.GetAutomationId(owner);
		if (!string.IsNullOrEmpty(automationId))
		{
			return automationId;
		}

		return string.Empty;
	}

	protected override string GetHelpTextCore()
	{
		var owner = GetOwner();
		return AutomationProperties.GetHelpText(owner) ?? string.Empty;
	}

	protected override string GetItemStatusCore()
	{
		var owner = GetOwner();
		return AutomationProperties.GetItemStatus(owner) ?? string.Empty;
	}

	protected override string GetItemTypeCore()
	{
		var owner = GetOwner();
		return AutomationProperties.GetItemType(owner) ?? string.Empty;
	}

	protected override AutomationPeer GetLabeledByCore()
	{
		var owner = GetOwner();
		var labeledBy = AutomationProperties.GetLabeledBy(owner);
		if (labeledBy is UIElement uiElement)
		{
			return uiElement.GetOrCreateAutomationPeer();
		}

		return null;
	}

	protected override AutomationLiveSetting GetLiveSettingCore()
	{
		var owner = GetOwner();
		return AutomationProperties.GetLiveSetting(owner);
	}

	// CCoreServices::GetTextElementBoundingRect -> CRichTextBlock::GetTextElementBoundRect: the range's
	// text bounds, unioned because a link wraps, then transformed to screen space.
	protected override Rect GetBoundingRectangleCore()
	{
		if (GetLinkBounds(out var element) is not { Length: > 0 } bounds)
		{
			return default;
		}

		var union = bounds[0];
		for (var i = 1; i < bounds.Length; i++)
		{
			union.Union(bounds[i]);
		}

		return element.TransformToVisual(null).TransformBounds(union);
	}

	protected override bool IsKeyboardFocusableCore() => true;

	protected override Point GetClickablePointCore()
	{
		if (GetLinkBounds(out var element) is not { Length: > 0 } bounds)
		{
			return default;
		}

		// We're looking for the point at the start of the link, so we only care about the first
		// rectangle, and return its top-left because the length is determined from there.
		var first = bounds[0];
		var point = element.TransformToVisual(null).TransformPoint(new Point(first.Left, first.Top));

		// Round up the Y pixel so we don't get the previous line when there is more than one line.
		return new Point(point.X, Math.Ceiling(point.Y));
	}

	// A TextElement has no bounding-box API, so the containing control owns the geometry.
	private Rect[] GetLinkBounds(out FrameworkElement element)
	{
		var owner = GetOwner();
		element = owner.GetContainingFrameworkElement();

		if (Text.TextAdapter.GetTextView(element) is not { } textView ||
			owner.ContentStart is not { } contentStart ||
			owner.ContentEnd is not { } contentEnd)
		{
			return Array.Empty<Rect>();
		}

		return textView.TextRangeToTextBounds((uint)contentStart.Offset, (uint)contentEnd.Offset);
	}

	protected override bool IsOffscreenCore()
	{
		// TODO Uno: Should delegate to containing TextBlock/RichTextBlock's
		// automation peer IsOffscreenHelper. For now, default to visible.
		return false;
	}

	/// <summary>
	/// Invokes the click action on the Hyperlink.
	/// </summary>
	public void Invoke()
	{
		var owner = GetOwner();
		// Simulate a click on the hyperlink for automation
		owner.OnClick();
	}
}
