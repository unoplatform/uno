// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference FlipViewAutomationPeer_Partial.cpp, tag winui3/release/1.8.4

using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes FlipView types to Microsoft UI Automation.
/// </summary>
public partial class FlipViewAutomationPeer : SelectorAutomationPeer
{
	public FlipViewAutomationPeer(Controls.FlipView owner) : base(owner)
	{

	}

	protected override IList<AutomationPeer> GetChildrenCore()
	{
		var children = base.GetChildrenCore();

		if ((Owner as Controls.FlipView).SelectedItem is { } selectedItem)
		{
			var itemPeer = CreateItemAutomationPeer(selectedItem);

			if (itemPeer is { })
			{
				var itemPeerAsAP = itemPeer as AutomationPeer;
				children.Add(itemPeerAsAP);

				if (children is { })
				{
					(var previousButton, var nextButton) = (Owner as Controls.FlipView).GetPreviousAndNextButtons();

					if (nextButton is { })
					{
						var nextButtonAP = nextButton.GetOrCreateAutomationPeer();

						if (nextButtonAP is { })
						{
							children.Insert(0, nextButtonAP);
						}
					}
					if (previousButton is { })
					{
						var previousButtonAP = previousButton.GetOrCreateAutomationPeer();

						if (previousButtonAP is { })
						{
							children.Insert(0, previousButtonAP);
						}
					}
				}
			}
		}

		return children;
	}

	protected override string GetClassNameCore() => nameof(Controls.FlipView);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.List;//UNO TODO: AutomationControlType.FlipView;

}
