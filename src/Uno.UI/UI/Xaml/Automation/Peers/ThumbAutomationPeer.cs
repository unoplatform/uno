// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ThumbAutomationPeer_Partial.cpp

using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Automation.Peers
{
	/// <summary>
	/// Exposes Thumb types to Microsoft UI Automation.
	/// </summary>
	public partial class ThumbAutomationPeer : FrameworkElementAutomationPeer
	{
		/// <summary>
		/// Initializes a new instance of the ThumbAutomationPeer class.
		/// </summary>
		/// <param name="owner">The Thumb to create a peer for.</param>
		public ThumbAutomationPeer(Thumb owner) : base(owner)
		{
		}

		protected override string GetClassNameCore() => nameof(Thumb);

		protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Thumb;
	}
}
