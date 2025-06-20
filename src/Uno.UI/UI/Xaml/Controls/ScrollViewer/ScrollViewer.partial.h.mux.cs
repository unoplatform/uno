namespace Microsoft.UI.Xaml.Controls;

partial class ScrollViewer
{
	private protected virtual void UpdateInputPaneOffsetX() { }

	private protected virtual void UpdateInputPaneOffsetY() { }

	private protected virtual void UpdateInputPaneTransition() { }

	private protected virtual bool IsRootScrollViewer => false;

	private protected virtual bool IsRootScrollViewerAllowImplicitStyle => false;

	private protected virtual bool IsInputPaneShow => false;
}
