// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference NavigationViewItemBase.h, commit 65718e2813

namespace Microsoft.UI.Xaml.Controls;

public partial class NavigationViewItemBase
{
	protected virtual void OnNavigationViewItemBasePositionChanged()
	{
	}

	protected virtual void OnNavigationViewItemBaseDepthChanged()
	{
	}

	protected virtual void OnNavigationViewItemBaseIsSelectedChanged()
	{
	}

	// Constant is a temporary measure. Potentially expose using TemplateSettings.
	protected const int c_itemIndentation = 31;

	internal bool IsTopLevelItem { get; set; } = false;

	/// <summary>
	/// Flag to keep track of whether this item was created by the custom internal NavigationViewItemsFactory.
	/// This is required in order to achieve proper recycling
	/// </summary>
	internal bool CreatedByNavigationViewItemsFactory { get; set; }

	internal bool IsInNavigationViewOwnedRepeater { get; set; }

	protected NavigationView m_navigationView = null;

	private NavigationViewRepeaterPosition m_position = NavigationViewRepeaterPosition.LeftNav;
	private int m_depth = 0;
}
