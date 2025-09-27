using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Disposables;

namespace Uno.UI.Xaml.Controls;

partial class ContentManager
{
	// The owner is either a DirectUI::Window object, or a DirectUI::XamlIslandRoot
	private object m_owner;

	// This is "true" only in UWP mode when m_owner is a DirectUI::Window
	// it is "false" when serving a XamlIslandRoot, including Desktop.
	private bool m_isUwpWindowContent;

	private UIElement m_content;

	private Microsoft.UI.Xaml.Controls.ScrollViewer m_RootScrollViewer;
	private Microsoft.UI.Xaml.Controls.ScrollContentPresenter m_RootSVContentPresenter;
	private readonly SerialDisposable m_tokRootScrollViewerSizeChanged = new();
}
