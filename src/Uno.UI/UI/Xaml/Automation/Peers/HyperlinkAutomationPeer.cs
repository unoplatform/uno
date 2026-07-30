// MUX Reference HyperlinkAutomationPeer_Partial.cpp, tag winui3/release/1.8.4

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

	protected override Rect GetBoundingRectangleCore()
	{
		// TODO Uno: GetTextElementBoundingRect is not available in Uno.
		// This needs lower-level text infrastructure to compute the
		// bounding rectangle of the Hyperlink inline within its containing TextBlock.
		return default;
	}

	protected override bool IsKeyboardFocusableCore() => true;

	protected override Point GetClickablePointCore()
	{
		// TODO Uno: Computing clickable point for inline text elements requires
		// text view infrastructure (ITextView, content start/end offsets,
		// TextRangeToTextBounds) which is not yet available.
		return default;
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
