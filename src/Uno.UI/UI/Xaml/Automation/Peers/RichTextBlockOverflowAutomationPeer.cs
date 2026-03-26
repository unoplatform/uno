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
			if (m_textPattern is null)
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

	// UNO TODO: WinUI populates automation peer children from the block collection,
	// filtering by overflow position using AppendAutomationPeerChildren. That method
	// is currently stubbed on TextElement. Until implemented, fall back to base.
	// See WinUI: RichTextBlockOverflowAutomationPeer_Partial.cpp
	protected override IList<AutomationPeer> GetChildrenCore()
		=> base.GetChildrenCore();
}
