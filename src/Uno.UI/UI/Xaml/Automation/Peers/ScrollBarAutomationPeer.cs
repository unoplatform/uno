// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ScrollBarAutomationPeer_Partial.cpp, tag winui3/release/1.8.4
using DirectUI;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes ScrollBar types to UI Automation.
/// </summary>
public partial class ScrollBarAutomationPeer : RangeBaseAutomationPeer
{
	private const string UIA_SCROLLBAR_HORIZONTAL = nameof(UIA_SCROLLBAR_HORIZONTAL);
	private const string UIA_SCROLLBAR_VERTICAL = nameof(UIA_SCROLLBAR_VERTICAL);

	public ScrollBarAutomationPeer(Controls.Primitives.ScrollBar owner) : base(owner)
	{
	}

	protected override string GetClassNameCore() => nameof(Controls.Primitives.ScrollBar);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.ScrollBar;

	protected override Point GetClickablePointCore()
		=> new(DoubleUtil.NaN, DoubleUtil.NaN);

	protected override AutomationOrientation GetOrientationCore()
	{
		var owner = Owner as Controls.Primitives.ScrollBar;

		if (owner.Orientation == Controls.Orientation.Horizontal)
		{
			return AutomationOrientation.Horizontal;
		}
		else
		{
			return AutomationOrientation.Vertical;
		}
	}

	protected override bool IsContentElementCore() => false;

	private protected override bool ChildIsAcceptable(UIElement element)
	{
		var childIsAcceptable = base.ChildIsAcceptable(element);

		if (childIsAcceptable)
		{
			var owner = Owner as Controls.Primitives.ScrollBar;

			if (element == owner.ElementHorizontalTemplate || element == owner.ElementVerticalTemplate)
			{
				return element.Visibility == Visibility.Visible;
			}
		}

		return childIsAcceptable;
	}

	protected override string GetNameCore()
	{
		var returnValue = base.GetNameCore();

		if (string.IsNullOrEmpty(returnValue))
		{
			var owner = Owner as Controls.Primitives.ScrollBar;

			if (owner.Orientation == Controls.Orientation.Horizontal)
			{
				return DXamlCore.Current.GetLocalizedResourceString(UIA_SCROLLBAR_HORIZONTAL);
			}
			else
			{
				return DXamlCore.Current.GetLocalizedResourceString(UIA_SCROLLBAR_VERTICAL);
			}
		}

		return returnValue;
	}
}
