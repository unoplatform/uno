// MUX Reference NavigationViewItemPresenter.h, commit 8811c96

using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	public partial class NavigationViewItemPresenter
	{
		private double m_compactPaneLengthValue = 40;

		private NavigationViewItemHelper<NavigationViewItemPresenter> m_helper;

		private Grid m_contentGrid = null;
		private Grid m_expandCollapseChevron = null;

		private double m_leftIndentation = 0;

		private Storyboard m_chevronExpandedStoryboard = null;
		private Storyboard m_chevronCollapsedStoryboard = null;
	}
}
