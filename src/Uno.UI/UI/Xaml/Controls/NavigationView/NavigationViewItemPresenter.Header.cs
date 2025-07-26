// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference NavigationViewItemPresenter.h, commit d3fef08

using Uno.Disposables;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public partial class NavigationViewItemPresenter
{
	private double m_compactPaneLengthValue = 40;

	private NavigationViewItemHelper<NavigationViewItemPresenter> m_helper;

	private Grid m_contentGrid;
	private ContentPresenter m_infoBadgePresenter;
	private Grid m_expandCollapseChevron;

	private SerialDisposable m_expandCollapseChevronTappedToken = new();

	private double m_leftIndentation;

	private Storyboard m_chevronExpandedStoryboard;
	private Storyboard m_chevronCollapsedStoryboard;
}
