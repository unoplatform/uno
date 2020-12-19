// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.UI.Xaml.Controls;
using Uno.UI.Helpers.WinUI;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;

namespace Microsoft.UI.Xaml.Automation.Peers
{
	public class PipsPagerAutomationPeer : FrameworkElementAutomationPeer
	{
		public PipsPagerAutomationPeer(PipsPager owner) : base(owner)
		{
		}

		// IAutomationPeerOverrides
		protected override object GetPatternCore(PatternInterface patternInterface)
		{
			if (patternInterface == PatternInterface.Selection)
			{
				return this;
			}

			return base.GetPatternCore(patternInterface);
		}

		protected override string GetClassNameCore()
		{
			return nameof(PipsPager);
		}

		protected override string GetNameCore()
		{
			string name = base.GetNameCore();

			if (string.IsNullOrEmpty(name))
			{
				var pipsPager = (PipsPager)Owner;
				if (pipsPager != null)
				{
					name = SharedHelpers.TryGetStringRepresentationFromObject(pipsPager.GetValue(AutomationProperties.NameProperty()));
				}
			}

			return name;
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.Menu;
		}

		internal object GetSelection()
		{
			if (Owner is PipsPager pager)
			{
				return pager.SelectedPageIndex;
			}
			return null;
		}

		internal void RaiseSelectionChanged(double oldIndex, double newIndex)
		{
			if (AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged))
			{
				RaisePropertyChangedEvent(SelectionPatternIdentifiers.SelectionProperty,
					oldIndex,
					newIndex);
			}
		}
	}
}
