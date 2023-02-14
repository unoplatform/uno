// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference NavigationViewItemPresenter.h, commit fd22d7f

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	public partial class NavigationViewItemPresenter
	{
		private double m_compactPaneLengthValue = 40;

		private NavigationViewItemHelper<NavigationViewItemPresenter> m_helper;

		private Grid m_contentGrid = null;
		private ContentPresenter m_infoBadgePresenter = null;
		private Grid m_expandCollapseChevron = null;

		private double m_leftIndentation = 0;

		private Storyboard m_chevronExpandedStoryboard = null;
		private Storyboard m_chevronCollapsedStoryboard = null;
	}
}
