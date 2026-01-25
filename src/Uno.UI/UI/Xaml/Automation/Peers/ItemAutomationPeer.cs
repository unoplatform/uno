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
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Provides the base implementation for item automation peers.
/// </summary>
public partial class ItemAutomationPeer : AutomationPeer, IVirtualizedItemProvider
{
	private readonly object _item;
	private readonly ItemsControlAutomationPeer _itemsControlAutomationPeer;

	public ItemAutomationPeer(object item, ItemsControlAutomationPeer parent)
	{
		_item = item ?? throw new ArgumentNullException(nameof(item));
		_itemsControlAutomationPeer = parent ?? throw new ArgumentNullException(nameof(parent));
		SetParent(parent);
	}

	public object Item => _item;

	public ItemsControlAutomationPeer ItemsControlAutomationPeer => _itemsControlAutomationPeer;

	public void Realize()
	{
		_itemsControlAutomationPeer?.EnsureItemRealized(this);
	}

	internal UIElement? GetContainer()
	{
		if (_itemsControlAutomationPeer?.Owner is ItemsControl itemsControl)
		{
			return itemsControl.ContainerFromItem(_item) as UIElement;
		}

		return null;
	}

	internal AutomationPeer? GetContainerPeer()
		=> GetContainer()?.GetOrCreateAutomationPeer();

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.VirtualizedItem)
		{
			return this;
		}

		var containerPeer = GetContainerPeer();
		if (containerPeer != null)
		{
			var pattern = containerPeer.GetPattern(patternInterface);
			if (pattern != null)
			{
				return pattern;
			}

			if (containerPeer is FrameworkElementAutomationPeer frameworkElementAutomationPeer)
			{
				return frameworkElementAutomationPeer.GetDefaultPattern(patternInterface);
			}
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override string GetAcceleratorKeyCore()
		=> ForwardToContainer(p => p.GetAcceleratorKey(), base.GetAcceleratorKeyCore);

	protected override string GetAccessKeyCore()
		=> ForwardToContainer(p => p.GetAccessKey(), base.GetAccessKeyCore);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> ForwardToContainer(p => p.GetAutomationControlType(), () => AutomationControlType.ListItem);

	protected override string GetAutomationIdCore()
		=> ForwardToContainer(p => p.GetAutomationId(), base.GetAutomationIdCore);

	protected override Rect GetBoundingRectangleCore()
		=> ForwardToContainer(p => p.GetBoundingRectangle(), base.GetBoundingRectangleCore);

	protected override IList<AutomationPeer> GetChildrenCore()
		=> ForwardToContainer(p => p.GetChildren(), base.GetChildrenCore);

	protected override string GetClassNameCore()
		=> ForwardToContainer(p => p.GetClassName(), base.GetClassNameCore);

	protected override Point GetClickablePointCore()
		=> ForwardToContainer(p => p.GetClickablePoint(), base.GetClickablePointCore);

	protected override string GetHelpTextCore()
		=> ForwardToContainer(p => p.GetHelpText(), base.GetHelpTextCore);

	protected override string GetItemStatusCore()
		=> ForwardToContainer(p => p.GetItemStatus(), base.GetItemStatusCore);

	protected override string GetItemTypeCore()
		=> ForwardToContainer(p => p.GetItemType(), base.GetItemTypeCore);

	protected override AutomationPeer GetLabeledByCore()
		=> ForwardToContainer(p => p.GetLabeledBy(), base.GetLabeledByCore);

	protected override string GetLocalizedControlTypeCore()
		=> ForwardToContainer(p => p.GetLocalizedControlType(), base.GetLocalizedControlTypeCore);

	protected override string GetNameCore()
		=> ForwardToContainer(p => p.GetName(), base.GetNameCore);

	protected override AutomationOrientation GetOrientationCore()
		=> ForwardToContainer(p => p.GetOrientation(), base.GetOrientationCore);

	// Note: Parent is set via SetParent in constructor - no GetParentCore override needed

	protected override AutomationLiveSetting GetLiveSettingCore()
		=> ForwardToContainer(p => p.GetLiveSetting(), base.GetLiveSettingCore);

	protected override int GetPositionInSetCore()
		=> ForwardToContainer(p => p.GetPositionInSet(), base.GetPositionInSetCore);

	protected override int GetSizeOfSetCore()
		=> ForwardToContainer(p => p.GetSizeOfSet(), base.GetSizeOfSetCore);

	protected override int GetLevelCore()
		=> ForwardToContainer(p => p.GetLevel(), base.GetLevelCore);

	protected override IReadOnlyList<AutomationPeer> GetControlledPeersCore()
		=> ForwardToContainer(p => p.GetControlledPeers(), base.GetControlledPeersCore);

	protected override IList<AutomationPeerAnnotation> GetAnnotationsCore()
		=> ForwardToContainer(p => p.GetAnnotations(), base.GetAnnotationsCore);

	protected override AutomationLandmarkType GetLandmarkTypeCore()
		=> ForwardToContainer(p => p.GetLandmarkType(), base.GetLandmarkTypeCore);

	protected override string GetLocalizedLandmarkTypeCore()
		=> ForwardToContainer(p => p.GetLocalizedLandmarkType(), base.GetLocalizedLandmarkTypeCore);

	protected override int GetCultureCore()
		=> ForwardToContainer(p => p.GetCulture(), base.GetCultureCore);

	protected override IEnumerable<AutomationPeer> GetDescribedByCore()
		=> ForwardToContainer(p => p.GetDescribedBy(), base.GetDescribedByCore);

	protected override IEnumerable<AutomationPeer> GetFlowsToCore()
		=> ForwardToContainer(p => p.GetFlowsTo(), base.GetFlowsToCore);

	protected override IEnumerable<AutomationPeer> GetFlowsFromCore()
		=> ForwardToContainer(p => p.GetFlowsFrom(), base.GetFlowsFromCore);

	protected override AutomationHeadingLevel GetHeadingLevelCore()
		=> ForwardToContainer(p => p.GetHeadingLevel(), base.GetHeadingLevelCore);

	protected override bool IsContentElementCore()
		=> ForwardToContainer(p => p.IsContentElement(), base.IsContentElementCore);

	protected override bool IsControlElementCore()
		=> ForwardToContainer(p => p.IsControlElement(), base.IsControlElementCore);

	protected override bool IsEnabledCore()
		=> ForwardToContainer(p => p.IsEnabled(), base.IsEnabledCore);

	protected override bool IsKeyboardFocusableCore()
		=> ForwardToContainer(p => p.IsKeyboardFocusable(), base.IsKeyboardFocusableCore);

	protected override bool IsOffscreenCore()
		=> ForwardToContainer(p => p.IsOffscreen(), base.IsOffscreenCore);

	protected override bool IsPasswordCore()
		=> ForwardToContainer(p => p.IsPassword(), base.IsPasswordCore);

	protected override bool IsRequiredForFormCore()
		=> ForwardToContainer(p => p.IsRequiredForForm(), base.IsRequiredForFormCore);

	protected override bool IsDataValidForFormCore()
		=> ForwardToContainer(p => p.IsDataValidForForm(), base.IsDataValidForFormCore);

	protected override bool IsPeripheralCore()
		=> ForwardToContainer(p => p.IsPeripheral(), base.IsPeripheralCore);

	protected override string GetFullDescriptionCore()
		=> ForwardToContainer(p => p.GetFullDescription(), base.GetFullDescriptionCore);

	protected override bool IsDialogCore()
		=> ForwardToContainer(p => p.IsDialog(), base.IsDialogCore);

	private T ForwardToContainer<T>(Func<AutomationPeer, T> callback, Func<T> fallback)
	{
		var containerPeer = GetContainerPeer();
		return containerPeer != null ? callback(containerPeer) : fallback();
	}
}
