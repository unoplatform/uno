// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ComboBoxLightDismiss_Partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

using Microsoft.UI.Xaml.Automation.Peers;
using Uno.UI.DataBinding;

namespace Microsoft.UI.Xaml.Controls.Primitives;

internal partial class ComboBoxLightDismiss : Canvas
{
	private ManagedWeakReference m_wrComboBox;

	// Create ComboBoxLightDismissAutomationPeer to represent the ComboBoxLightDismiss.
	protected override AutomationPeer OnCreateAutomationPeer() => new ComboBoxLightDismissAutomationPeer(this);

	internal void SetOwner(ComboBox pOwner) => m_wrComboBox = WeakReferencePool.RentWeakReference(this, pOwner);

	internal void AutomationClick()
	{
		if (m_wrComboBox?.Target is ComboBox spComboBox)
		{
			// Invoke through UIAutomation
			spComboBox.IsDropDownOpen = false;
		}

		// TODO Uno: ResetAutomationPeer(): Once LightDismiss is executed we no longer need this AP. Uno's
		// peers are recreated on demand and no screen reader caches this one.
	}
}
