// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml/xcp/dxaml/lib/RichEditBoxAutomationPeer_Partial.cpp, commit 3c9c168844f06c6ac000a97977f0bb3f4c90fd75
#nullable enable

using System.Collections.Generic;
using DirectUI;

namespace Microsoft.UI.Xaml.Automation.Peers;

partial class RichEditBoxAutomationPeer
{
#if !HAS_UNO
	// TODO Uno: The owner-taking CLR constructor replaces CreateInstanceWithOwnerImpl's COM aggregation factory.
	// IFCPTR(ppInstance);
	// IFCEXPECT(pOuter == NULL || ppInner != NULL);
	// IFCPTR(owner);
	// IFC(ctl::do_query_interface(ownerAsUIE, owner));
	// IFC(ActivateInstance(pOuter, static_cast<RichEditBox*>(owner)->GetHandle(), &pInner));
	// IFC(ctl::do_query_interface(pInstance, pInner));
	// IFC(static_cast<RichEditBoxAutomationPeer*>(pInstance)->put_Owner(ownerAsUIE));
#endif

	// Initializes a new instance of the RichEditBoxAutomationPeer class.
	/// <summary>Initializes a new instance of the RichEditBoxAutomationPeer class.</summary>
	/// <param name="owner">The RichEditBox that is associated with this automation peer.</param>
	public RichEditBoxAutomationPeer(Controls.RichEditBox owner) : base(owner)
	{
	}

	// Deconstructor
#if !HAS_UNO
	// TODO Uno: The native destructor is empty and does not require a finalizer.
	// RichEditBoxAutomationPeer::~RichEditBoxAutomationPeer()
	// {
	// }
#endif

	/// <inheritdoc />
	protected override string GetClassNameCore() => "RichEditBox";

	/// <inheritdoc />
	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Edit;

	/// <inheritdoc />
	protected override IEnumerable<AutomationPeer>? GetDescribedByCore()
	{
		var spOwner = Owner;
		TextBoxPlaceholderTextHelper.SetupPlaceholderTextBlockDescribedBy(spOwner);
		return base.GetDescribedByCore();
	}
}
