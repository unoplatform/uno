// MUX Reference GridViewItemPresenter_Partial.cpp, tag winui3/release/1.8.4

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class GridViewItemPresenter
{
	// MUX Reference GridViewItemPresenter_Partial.cpp, lines 12-34
	// GridViewItemPresenter::GoToElementStateCoreImpl
	protected override bool GoToElementStateCore(string stateName, bool useTransitions)
	{
#if HAS_UNO
		// TODO Uno: GridViewItemPresenter does not yet have its own chrome implementation.
		// In WinUI, both ListViewItemPresenter and GridViewItemPresenter delegate to the
		// same CListViewBaseItemChrome::GoToChromedState via their native handle.
		// Until the chrome logic is shared via a common base or helper, GridViewItemPresenter
		// falls through to the VisualStateManager template-based states.
		return false;
#else
		bool wentToState = false;

		GoToChromedState(stateName, useTransitions, ref wentToState);

		ProcessAnimationCommands();

		return wentToState;
#endif
	}

	// MUX Reference GridViewItemPresenter_Partial.cpp, lines 36-39
	// GridViewItemPresenter::get_GridViewItemPresenterPaddingImpl
	// GridViewItemPresenter::put_GridViewItemPresenterPaddingImpl
	// These legacy properties delegate to ContentPresenter.Padding.
	// TODO Uno: Implement when GridViewItemPresenter properties are ported.

	// MUX Reference GridViewItemPresenter_Partial.cpp, lines 44-51
	// GridViewItemPresenter::get_GridViewItemPresenterHorizontalContentAlignmentImpl
	// GridViewItemPresenter::put_GridViewItemPresenterHorizontalContentAlignmentImpl
	// TODO Uno: Implement when GridViewItemPresenter properties are ported.

	// MUX Reference GridViewItemPresenter_Partial.cpp, lines 52-59
	// GridViewItemPresenter::get_GridViewItemPresenterVerticalContentAlignmentImpl
	// GridViewItemPresenter::put_GridViewItemPresenterVerticalContentAlignmentImpl
	// TODO Uno: Implement when GridViewItemPresenter properties are ported.
}
