// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RichTextBlockAutomationPeer_Partial.cpp, tag winui3/release/1.8.4
using System;
using DirectUI;
using Windows.Foundation;
using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes RichTextBlock types to Microsoft UI Automation.
/// </summary>
public partial class RichTextBlockAutomationPeer : FrameworkElementAutomationPeer
{
#if __SKIA__
	// CRichTextBlockAutomationPeer::m_pTextPattern — the faithfully-ported Text pattern provider.
	private Text.TextAdapter m_textPattern;
#endif

	public RichTextBlockAutomationPeer(Controls.RichTextBlock owner) : base(owner)
	{
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Text)
		{
#if __SKIA__
			// CRichTextBlockAutomationPeer::GetPatternCore — lazily create the TextAdapter over the owner.
			if (m_textPattern is null && Owner is Controls.RichTextBlock owner)
			{
				m_textPattern = new Text.TextAdapter(owner, this);
			}

			return m_textPattern;
#else
			// The Text pattern provider is Skia-only (container/view/position model). On non-Skia targets
			// there is no plain-text position layer, so report no Text pattern.
			return base.GetPatternCore(patternInterface);
#endif
		}
		else
		{
			return base.GetPatternCore(patternInterface);
		}
	}

	protected override string GetClassNameCore() => nameof(Controls.RichTextBlock);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Text;

#if __SKIA__
	// CRichTextBlockAutomationPeer::GetChildrenCore — walk the block collection, stopping at the first
	// block whose content starts at/after the overflow target's content start, and append each block's
	// inline peers. Skia-only: the overflow/position filtering needs the TextPointer position layer.
	protected override IList<AutomationPeer> GetChildrenCore()
	{
		if (Owner is not Controls.RichTextBlock owner)
		{
			return base.GetChildrenCore();
		}

		var children = new List<AutomationPeer>();

		int posOverflowStart = int.MaxValue;
		if (owner.HasOverflowContent && owner.OverflowContentTarget is { } overflowControl)
		{
			// CRichTextBlockOverflow::get_ContentStart offset — the position where overflow content begins.
			posOverflowStart = (int)overflowControl.GetContentStartPosition();
		}

		foreach (var block in owner.Blocks)
		{
			if (posOverflowStart != int.MaxValue)
			{
				var posBlockStart = block.GetContentStart()?.Offset ?? -1;
				if (posBlockStart >= posOverflowStart)
				{
					break;
				}
			}

			block.AppendAutomationPeerChildren(children, 0, posOverflowStart);
		}

		return children;
	}
#endif
}
