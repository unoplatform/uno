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
	private object m_textPattern;

	public RichTextBlockOverflowAutomationPeer(Controls.RichTextBlockOverflow owner) : base(owner)
	{
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Text)
		{
			if (m_textPattern is not { })
			{
				//UNO TODO:

				//	// RichTextBlockOverflows that don't have a master RichTextBlock don't have a text pattern, and should return nullptr.
				//	if (static_cast<CRichTextBlockOverflow*>((static_cast<RichTextBlockOverflow*>(spOwner.Get())->GetHandle()))->m_pMaster != nullptr)
				//	{
				//		IFC(ActivationAPI::ActivateAutomationInstance(KnownTypeIndex::TextAdapter, static_cast<RichTextBlockOverflow*>(spOwner.Get())->GetHandle(), spTextAdapter.GetAddressOf()));

				//		IFCPTR(spTextAdapter.Get());

				//		m_pTextPattern = spTextAdapter.Detach();
				//		IFC(m_pTextPattern->put_Owner(spOwner.Get()));
				//	}

				m_textPattern = Owner;
			}

			return m_textPattern;
		}
		else
		{
			return base.GetPatternCore(patternInterface);
		}
	}

	protected override string GetClassNameCore() => nameof(Controls.RichTextBlockOverflow);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Text;

	// We populate automation peer children from its block collection recursively
	// Here we need to eliminate all text elements which are
	// are present in the previous RichTextBlock/RichTextBlockOverflow
	// overflowing to next RichTextOverflow if any
	protected override IList<AutomationPeer> GetChildrenCore()
	{
		var owner = Owner as Controls.RichTextBlockOverflow;

		var posContentStart = 0;
		var posOverflowStart = int.MaxValue;

		var returnValue = base.GetChildrenCore();

		if (owner.ContentStart is { } contentStart)
		{
			posContentStart = contentStart.Offset;

			if (owner.HasOverflowContent)
			{
				if (owner.OverflowContentTarget?.ContentStart is { } spOverflowStart)
				{
					posOverflowStart = spOverflowStart.Offset;
				}
			}
		}

		var spSourceControl = owner.ContentSource;
		var spBlocks = spSourceControl.Blocks;
		var count = spBlocks.Count;

		for (var i = 0; i < count; i++)
		{
			var spBlock = spBlocks[i];
			if (owner.HasOverflowContent)
			{
				var blockStart = spBlock.ContentStart;

				if (blockStart.Offset >= posOverflowStart)
				{
					break;
				}
			}

			//UNO TODO: AppendAutomationPeerChildren
			returnValue = spBlock.AppendAutomationPeerChildren(posContentStart, posOverflowStart);
		}

		return returnValue;
	}
}
