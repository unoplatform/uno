// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference PipsPagerAutomationPeer.cpp

using System;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Helpers.WinUI;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes PipsPager types to Microsoft UI Automation.
/// </summary>
public partial class PipsPagerAutomationPeer : FrameworkElementAutomationPeer, ISelectionProvider
{
	/// <summary>
	/// Initializes a new instance of the PipsPagerAutomationPeer class.
	/// </summary>
	/// <param name="owner">The PipsPager control instance to create the peer for.</param>
	public PipsPagerAutomationPeer(PipsPager owner) : base(owner)
	{
	}

	bool ISelectionProvider.CanSelectMultiple => false;

	bool ISelectionProvider.IsSelectionRequired => true;

	// IAutomationPeerOverrides
	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Selection)
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override string GetClassNameCore() => nameof(PipsPager);

	protected override string GetNameCore()
	{
		string name = base.GetNameCore();

		if (string.IsNullOrEmpty(name))
		{
			if (Owner is PipsPager pipsPager)
			{
				name = SharedHelpers.TryGetStringRepresentationFromObject(pipsPager.GetValue(AutomationProperties.NameProperty));
			}
		}

		return name;
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
	{
		return AutomationControlType.Menu;
	}

	private PipsPager GetImpl() => Owner as PipsPager;

	IRawElementProviderSimple[] ISelectionProvider.GetSelection()
	{
		if (GetImpl() is { } pager)
		{
			if (pager.GetSelectedItem() is { } pip)
			{
				if (FrameworkElementAutomationPeer.CreatePeerForElement(pip) is { } peer)
				{
					return new[] { ProviderFromPeer(peer) };
				}
			}
		}
		return Array.Empty<IRawElementProviderSimple>();
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
