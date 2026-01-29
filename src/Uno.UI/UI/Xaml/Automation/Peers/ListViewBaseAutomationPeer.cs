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

	protected override List<AutomationPeer> GetChildrenCore()
	{
		var owner = ListViewBaseOwner;
		var baseChildren = base.GetChildrenCore() ?? new List<AutomationPeer>();
		if (owner?.ItemsPresenter is not ItemsPresenter presenter)
		{
			return baseChildren;
		}

		var header = presenter.HeaderContentControl;
		var footer = presenter.FooterContentControl;
		if (header is null && footer is null)
		{
			return baseChildren;
		}

		var children = new List<AutomationPeer>(baseChildren.Count + (header is null ? 0 : 1) + (footer is null ? 0 : 1));
		AddPeerForElement(header, children);
		children.AddRange(baseChildren);
		AddPeerForElement(footer, children);
		return children;
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

	private void AddPeerForElement(FrameworkElement? element, List<AutomationPeer> children)
	{
		if (element is null)
		{
			return;
		}

		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(element);
		if (peer != null)
		{
			children.Add(peer);
		}
	}

	private string GetDropEffect()
	{
		if (ListViewBaseOwner is IDropTargetProvider provider)
		{
			return provider.DropEffect;
		}

		return string.Empty;
	}

	private string[] GetDropEffects()
	{
		if (ListViewBaseOwner is IDropTargetProvider provider)
		{
			return provider.DropEffects ?? s_emptyDropEffects;
		}

		return s_emptyDropEffects;
	}

	private ListViewBase? ListViewBaseOwner => Owner as ListViewBase;
}
