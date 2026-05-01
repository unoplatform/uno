// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference AutoSuggestBoxAutomationPeer_Partial.cpp, commit 5f9e85113

using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers
{
	partial class AutoSuggestBoxAutomationPeer
	{
		// AutoSuggestBoxAutomationPeerFactory::CreateInstanceWithOwnerImpl is implicit in the
		// public ctor that takes an AutoSuggestBox owner (see AutoSuggestBoxAutomationPeer.cs).

		/// <inheritdoc />
		protected override string GetClassNameCore() => nameof(AutoSuggestBox);

		/// <inheritdoc />
		protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

		/// <inheritdoc />
		protected override object GetPatternCore(PatternInterface patternInterface)
		{
			if (patternInterface == PatternInterface.Invoke)
			{
				return this;
			}
			else
			{
				return base.GetPatternCore(patternInterface);
			}
		}

		/// <summary>
		/// Sends a request to submit the auto-suggest query to the AutoSuggestBox associated with the automation peer.
		/// </summary>
		public void Invoke()
		{
			if (Owner is AutoSuggestBox owner)
			{
				owner.ProgrammaticSubmitQuery();
			}
		}
	}
}
