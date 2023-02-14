using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	public partial class TabViewListView : ListView
	{
		// TODO: Uno specific: Getting scrollviewer from template and applying scroll properties directly to it
		// until attached property template bindings are supported (issue #4259)

		private ScrollViewer m_scrollViewer;

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			m_scrollViewer = GetTemplateChild<ScrollViewer>("ScrollViewer");
		}

		internal void SetHorizontalScrollBarVisibility(ScrollBarVisibility scrollBarVisibility)
		{
			if (m_scrollViewer != null)
			{
				ScrollViewer.SetHorizontalScrollBarVisibility(m_scrollViewer, scrollBarVisibility);
			}
		}
	}
}
