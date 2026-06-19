// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RichTextBlockOverflowAutomationPeer_Partial.cpp, tag winui3/release/1.8.4
using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes RichTextBlockOverflow types to Microsoft UI Automation.
/// </summary>
public partial class RichTextBlockOverflowAutomationPeer : FrameworkElementAutomationPeer
{
#if __SKIA__
	// CRichTextBlockOverflowAutomationPeer::m_pTextPattern — the faithfully-ported Text pattern provider.
	private Text.TextAdapter m_textPattern;
#endif

	public RichTextBlockOverflowAutomationPeer(Controls.RichTextBlockOverflow owner) : base(owner)
	{
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Text)
		{
#if __SKIA__
			// CRichTextBlockOverflowAutomationPeer::GetPatternCore — overflows without a master
			// RichTextBlock have no text pattern. Only create the adapter when a master exists.
			if (m_textPattern is null
				&& Owner is Controls.RichTextBlockOverflow owner
				&& owner.ContentSource is not null)
			{
				m_textPattern = new Text.TextAdapter(owner, this);
			}

			return m_textPattern;
#else
			// The Text pattern provider is Skia-only (container/view/position model).
			return base.GetPatternCore(patternInterface);
#endif
		}
		else
		{
			return base.GetPatternCore(patternInterface);
		}
	}

	protected override string GetClassNameCore() => nameof(Controls.RichTextBlockOverflow);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Text;

#if __SKIA__
	// CRichTextBlockOverflowAutomationPeer::GetChildrenCore — append the inline peers from the source
	// (master) RichTextBlock's blocks that fall within this overflow's slice [contentStart, overflowStart).
	protected override IList<AutomationPeer> GetChildrenCore()
	{
		if (Owner is not Controls.RichTextBlockOverflow owner)
		{
			return base.GetChildrenCore();
		}

		var sourceControl = owner.ContentSource;
		if (sourceControl is null)
		{
			return base.GetChildrenCore();
		}

		var children = new List<AutomationPeer>();

		int posContentStart = (int)owner.GetContentStartPosition();

		int posOverflowStart = int.MaxValue;
		if (owner.HasOverflowContent && owner.OverflowContentTarget is { } overflowControl)
		{
			posOverflowStart = (int)overflowControl.GetContentStartPosition();
		}

		foreach (var block in sourceControl.Blocks)
		{
			if (posOverflowStart != int.MaxValue)
			{
				var posBlockStart = block.GetContentStart()?.Offset ?? -1;
				if (posBlockStart >= posOverflowStart)
				{
					break;
				}
			}

			block.AppendAutomationPeerChildren(children, posContentStart, posOverflowStart);
		}

		return children;
	}
#endif
}
