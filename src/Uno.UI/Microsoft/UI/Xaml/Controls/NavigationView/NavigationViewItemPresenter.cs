// MUX Reference NavigationViewItemPresenter.cpp, commit de78834

using Uno.UI.Helpers.WinUI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	public partial class NavigationViewItemPresenter : ContentControl
	{
		private const string c_contentGrid = "PresenterContentRootGrid";
		private const string c_expandCollapseChevron = "ExpandCollapseChevron";
		private const string c_expandCollapseRotateExpandedStoryboard = "ExpandCollapseRotateExpandedStoryboard";
		private const string c_expandCollapseRotateCollapsedStoryboard = "ExpandCollapseRotateCollapsedStoryboard";

		private const string c_iconBoxColumnDefinitionName = "IconColumn";

		public NavigationViewItemPresenter()
		{
			DefaultStyleKey = typeof(NavigationViewItemPresenter);
		}

		protected override void OnApplyTemplate()
		{
			//IControlProtected controlProtected = this;

			// Retrieve pointers to stable controls
			m_helper = new NavigationViewItemHelper<NavigationViewItemPresenter>(this);
			m_helper.Init(this);

			var contentGrid = GetTemplateChild(c_contentGrid) as Grid;
			if (contentGrid != null)
			{
				m_contentGrid = contentGrid;
			}

			var navigationViewItem = GetNavigationViewItem();
			if (navigationViewItem != null)
			{
#if IS_UNO
				// TODO: Uno specific: We may be reapplying the template, in which case
				// we need to unsubscribe the previous Tapped event handler.
				// Can be removed when #4689.
				if (m_expandCollapseChevron != null)
				{
					m_expandCollapseChevron.Tapped -= navigationViewItem.OnExpandCollapseChevronTapped;
				}
#endif

				var expandCollapseChevron = GetTemplateChild(c_expandCollapseChevron) as Grid;
				if (expandCollapseChevron != null)
				{
					m_expandCollapseChevron = expandCollapseChevron;
					expandCollapseChevron.Tapped += navigationViewItem.OnExpandCollapseChevronTapped;
				}
				navigationViewItem.UpdateVisualStateNoTransition();


				// We probably switched displaymode, so restore width now, otherwise the next time we will restore is when the CompactPaneLength changes
				var navigationView = navigationViewItem.GetNavigationView();
				if (navigationView != null)
				{
					if (navigationView.PaneDisplayMode != NavigationViewPaneDisplayMode.Top)
					{
						UpdateCompactPaneLength(m_compactPaneLengthValue, true);
					}
				}
			}

			m_chevronExpandedStoryboard = (Storyboard)GetTemplateChild(c_expandCollapseRotateExpandedStoryboard);
			m_chevronCollapsedStoryboard = (Storyboard)GetTemplateChild(c_expandCollapseRotateCollapsedStoryboard);

			UpdateMargin();
		}

		internal void RotateExpandCollapseChevron(bool isExpanded)
		{
			if (isExpanded)
			{
				var openStoryboard = m_chevronExpandedStoryboard;
				if (openStoryboard != null)
				{
					openStoryboard.Begin();
				}
			}
			else
			{
				var closedStoryboard = m_chevronCollapsedStoryboard;
				if (closedStoryboard != null)
				{
					closedStoryboard.Begin();
				}
			}
		}

		internal UIElement GetSelectionIndicator()
		{
#if IS_UNO
			// TODO: Uno specific: This is done to ensure that the presenter
			// was initialized properly - if helper is not null, but content grid
			// is null, it means the presenter was not initialized correctly.
			// Can be removed when #4809 is fixed.
			if (m_contentGrid == null && m_helper != null)
			{
				// Reinitialize
				OnApplyTemplate();
			}
#endif
			// m_helper could be null here, if template was not yet applied
			return m_helper?.GetSelectionIndicator();
		}

		protected override bool GoToElementStateCore(string state, bool useTransitions)
		{
			// GoToElementStateCore: Update visualstate for itself.
			// VisualStateManager.GoToState: update visualstate for it's first child.

			// If NavigationViewItemPresenter is used, two sets of VisualStateGroups are supported. One set is help to switch the style and it's NavigationViewItemPresenter itself and defined in NavigationViewItem
			// Another set is defined in style for NavigationViewItemPresenter.
			// OnLeftNavigation, OnTopNavigationPrimary, OnTopNavigationOverflow only apply to itself.
			if (state == NavigationViewItemHelper.c_OnLeftNavigation || state == NavigationViewItemHelper.c_OnLeftNavigationReveal || state == NavigationViewItemHelper.c_OnTopNavigationPrimary
				|| state == NavigationViewItemHelper.c_OnTopNavigationPrimaryReveal || state == NavigationViewItemHelper.c_OnTopNavigationOverflow)
			{
				return base.GoToElementStateCore(state, useTransitions);
			}
			return VisualStateManager.GoToState(this, state, useTransitions);
		}

		private NavigationViewItem GetNavigationViewItem()
		{
			NavigationViewItem navigationViewItem = null;

			DependencyObject obj = this;

			var item = SharedHelpers.GetAncestorOfType<NavigationViewItem>(VisualTreeHelper.GetParent(obj));
			if (item != null)
			{
				navigationViewItem = item;
			}
			return navigationViewItem;
		}

		internal void UpdateContentLeftIndentation(double leftIndentation)
		{
			m_leftIndentation = leftIndentation;
			UpdateMargin();
		}

		private void UpdateMargin()
		{
			var grid = m_contentGrid;
			if (grid != null)
			{
				var oldGridMargin = grid.Margin;
				grid.Margin = new Thickness(m_leftIndentation, oldGridMargin.Top, oldGridMargin.Right, oldGridMargin.Bottom);
			}
		}

		internal void UpdateCompactPaneLength(double compactPaneLength, bool shouldUpdate)
		{
			m_compactPaneLengthValue = compactPaneLength;
			if (shouldUpdate)
			{
				var iconGridColumn = GetTemplateChild(c_iconBoxColumnDefinitionName) as ColumnDefinition;
				if (iconGridColumn != null)
				{
					var gridLength = iconGridColumn.Width;
					var newGridLength = new GridLength(compactPaneLength, gridLength.GridUnitType);
					iconGridColumn.Width = newGridLength;
				}
			}
		}

		internal void UpdateClosedCompactVisualState(bool isTopLevelItem, bool isClosedCompact)
		{
			// We increased the ContentPresenter margin to align it visually with the expand/collapse chevron. This updated margin is even applied when the
			// NavigationView is in a visual state where no expand/collapse chevrons are shown, leading to more content being cut off than necessary.
			// This is the case for top-level items when the NavigationView is in a compact mode and the NavigationView pane is closed. To keep the original
			// cutoff visual experience intact, we restore  the original ContentPresenter margin for such top-level items only (children shown in a flyout
			// will use the updated margin).
			var stateName = isClosedCompact && isTopLevelItem
			   ? "ClosedCompactAndTopLevelItem"
			   : "NotClosedCompactAndTopLevelItem";

			VisualStateManager.GoToState(this, stateName, false /*useTransitions*/);
		}
	}
}
