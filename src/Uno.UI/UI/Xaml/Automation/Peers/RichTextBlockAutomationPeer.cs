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
	public RichTextBlockAutomationPeer(Controls.RichTextBlock owner) : base(owner)
	{
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Text)
		{
			//if (!m_pTextPattern)
			//{
			//	ctl::ComPtr<IUIElement> spOwner;
			//	ctl::ComPtr<TextAdapter> spTextAdapter;

			//	IFC(get_Owner(&spOwner));
			//	IFCPTR(spOwner.Get());

			//ActivateAutomationInstance

			return null;

			//	IFC(ActivationAPI::ActivateAutomationInstance(KnownTypeIndex::TextAdapter, static_cast<RichTextBlock*>(spOwner.Get())->GetHandle(), spTextAdapter.GetAddressOf()));

			//	m_pTextPattern = spTextAdapter.Detach();
			//	IFC(m_pTextPattern->put_Owner(spOwner.Get()));
			//}
			//*ppReturnValue = ctl::as_iinspectable((m_pTextPattern));
			//ctl::addref_interface(m_pTextPattern);
		}
		else
		{
			return base.GetPatternCore(patternInterface);
		}
	}

	protected override string GetClassNameCore() => nameof(Controls.RichTextBlock);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Text;

	// We populate automation peer children from its block collection recursively
	// Here we need to eliminate all text elements which are overflowing to next RichTextBlockOverflow if any
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

			returnValue = spBlock.AppendAutomationPeerChildren(posContentStart, posOverflowStart);
		}

		return returnValue;
	}
}
