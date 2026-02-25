// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextBlockAutomationPeer_Partial.cpp, tag winui3/release/1.8.4
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using DirectUI;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes TextBlock types to UI Automation.
/// </summary>
public partial class TextBlockAutomationPeer : FrameworkElementAutomationPeer
{
	private TextAdapter m_textPattern;

	public TextBlockAutomationPeer(TextBlock owner) : base(owner)
	{
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Text)
		{
			if (m_textPattern == null)
			{
				if (Owner is TextBlock owner)
				{
					m_textPattern = new TextAdapter(owner);
				}
			}
			return m_textPattern;
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override string GetClassNameCore() => nameof(TextBlock);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Text;

	protected override IList<AutomationPeer> GetChildrenCore()
	{
		if (Owner is TextBlock owner)
		{
			var children = new ObservableCollection<AutomationPeer>();
			foreach (var inline in owner.Inlines)
			{
				inline.AppendAutomationPeerChildren(-1, int.MaxValue);
			}
			return children;
		}

		return base.GetChildrenCore();
	}
}
