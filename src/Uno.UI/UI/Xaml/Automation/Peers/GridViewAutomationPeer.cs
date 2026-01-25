// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference GridViewAutomationPeer_Partial.cpp, tag winui3/release/1.8.4

using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes GridView types to Microsoft UI Automation.
/// </summary>
public partial class GridViewAutomationPeer : ListViewBaseAutomationPeer
{
	public GridViewAutomationPeer(GridView owner) : base(owner)
	{

	}

	protected override string GetClassNameCore()
		=> nameof(GridView);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.List;


	//_Check_return_ HRESULT GridViewAutomationPeer::OnCreateItemAutomationPeerImpl(_In_ IInspectable* item, _Outptr_ xaml_automation_peers::IItemAutomationPeer** returnValue)
	//{

	//	HRESULT hr = S_OK;
	//	ctl::ComPtr<xaml_automation_peers::IGridViewItemDataAutomationPeer> spGridViewItemDataAutomationPeer;
	//	ctl::ComPtr<xaml_automation_peers::IGridViewItemDataAutomationPeerFactory> spGridViewItemDataAPFactory;
	//	ctl::ComPtr<IActivationFactory> spActivationFactory;
	//	ctl::ComPtr<IInspectable> spInner;

	//	spActivationFactory.Attach(ctl::ActivationFactoryCreator<DirectUI::GridViewItemDataAutomationPeerFactory>::CreateActivationFactory());
	//	IFC(spActivationFactory.As<xaml_automation_peers::IGridViewItemDataAutomationPeerFactory>(&spGridViewItemDataAPFactory));

	//	IFC(spGridViewItemDataAPFactory.Cast<GridViewItemDataAutomationPeerFactory>()->CreateInstanceWithParentAndItem(item,

	//		this,
	//		NULL,
	//		&spInner,
	//		&spGridViewItemDataAutomationPeer));
	//	IFC(spGridViewItemDataAutomationPeer.CopyTo<xaml_automation_peers::IItemAutomationPeer>(returnValue));

	//	Cleanup:
	//	RRETURN(hr);
	//}
}
