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

	// UNO TODO: Populate automation peer children from block collection recursively,
	// filtering out text elements that overflow to RichTextBlockOverflow.
	// The previous implementation was broken (copy-pasted from RichTextBlockOverflow
	// and used ContentSource which doesn't exist on RichTextBlock).
	// For now, use the base implementation which walks the visual tree.
}
