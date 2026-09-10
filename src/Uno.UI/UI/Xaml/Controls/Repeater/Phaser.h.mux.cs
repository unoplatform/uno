// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference Phaser.h, commit 4b206bce3

using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Controls;

internal struct ElementInfo
{
	public ElementInfo(UIElement element, VirtualizationInfo virtInfo)
	{
		Element = element;
		VirtInfo = virtInfo;
	}

	public UIElement Element { get; }
	public VirtualizationInfo VirtInfo { get; }
}

partial class Phaser
{
	private ItemsRepeater m_owner = null;
	private List<ElementInfo> m_pendingElements = new List<ElementInfo>();
	private bool m_registeredForCallback = false;
}
