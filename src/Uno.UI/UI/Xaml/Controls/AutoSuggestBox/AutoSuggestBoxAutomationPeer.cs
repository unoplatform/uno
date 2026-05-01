// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference AutoSuggestBoxAutomationPeer.idl, commit 5f9e85113

using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers
{
	/// <summary>
	/// Exposes AutoSuggestBox types to Microsoft UI Automation.
	/// </summary>
	public partial class AutoSuggestBoxAutomationPeer
		: FrameworkElementAutomationPeer, Provider.IInvokeProvider
	{
		/// <summary>
		/// Initializes a new instance of the AutoSuggestBoxAutomationPeer class.
		/// </summary>
		/// <param name="owner">The AutoSuggestBox to associate with the new automation peer.</param>
		public AutoSuggestBoxAutomationPeer(AutoSuggestBox owner) : base(owner)
		{
		}
	}
}
