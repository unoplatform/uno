// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable
#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

public partial class ListViewBaseAutomationPeer : SelectorAutomationPeer, IDropTargetProvider
{
	private static readonly string[] s_emptyDropEffects = Array.Empty<string>();

	public ListViewBaseAutomationPeer(ListViewBase owner) : base(owner)
	{
		ArgumentNullException.ThrowIfNull(owner);
	}

	public string DropEffect => GetDropEffect();

	public string[] DropEffects => GetDropEffects();

	protected override IList<AutomationPeer> GetChildrenCore()
	{
		var owner = ListViewBaseOwner;
		List<AutomationPeer> spAPChildren = new();
		ContentControl? spHeaderContainer = null;
		ContentControl? spFooterContainer = null;

		if (owner?.ItemsPresenter is ItemsPresenter spItemsPresenter)
		{
			spHeaderContainer = spItemsPresenter.HeaderContentControl;
			spFooterContainer = spItemsPresenter.FooterContentControl;
		}

		if (spHeaderContainer is not null)
		{
			spAPChildren.AddRange(GetAutomationPeersForChildrenOfElement(spHeaderContainer));
		}

		var itemsControlChildren = base.GetChildrenCore();
		if (itemsControlChildren is not null)
		{
			spAPChildren.AddRange(itemsControlChildren);
		}

		if (spFooterContainer is not null)
		{
			spAPChildren.AddRange(GetAutomationPeersForChildrenOfElement(spFooterContainer));
		}

		return spAPChildren;
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
		=> patternInterface == PatternInterface.DropTarget ? this : base.GetPatternCore(patternInterface);

	protected override bool IsOffscreenCore()
	{
		var owner = ListViewBaseOwner;
		if (owner?.SemanticZoomOwner is not null)
		{
			return !owner.IsActiveView;
		}

		return base.IsOffscreenCore();
	}

	private string GetDropEffect() => ListViewBaseOwner?.GetDropTargetDropEffect() ?? string.Empty;

	private string[] GetDropEffects() => ListViewBaseOwner?.GetDropTargetDropEffects() ?? s_emptyDropEffects;

	private ListViewBase? ListViewBaseOwner => Owner as ListViewBase;
}
