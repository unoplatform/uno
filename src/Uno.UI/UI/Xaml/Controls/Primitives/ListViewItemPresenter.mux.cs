// MUX Reference ListViewItemPresenter_Partial.cpp, tag winui3/release/1.8.4

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class ListViewItemPresenter
{
	// MUX Reference ListViewItemPresenter_Partial.cpp, lines 12-42
	// ListViewItemPresenter::GoToElementStateCoreImpl
	protected override bool GoToElementStateCore(string stateName, bool useTransitions)
	{
		bool wentToState = false;

		// In WinUI, this delegates to CListViewBaseItemChrome::GoToChromedState via the native handle.
		// In Uno, the chrome logic is embedded directly in the presenter.
		GoToChromedState(stateName, useTransitions, ref wentToState);

		ProcessAnimationCommands();

		// In WinUI, this calls __super::GoToElementStateCoreImpl which delegates to the
		// VisualStateManager. In Uno, returning the wentToState value achieves the same effect:
		// if true, the VSM considers the state handled; if false, it falls through to template VSGs.
		return wentToState;
	}

	// MUX Reference ListViewItemPresenter_Partial.cpp, lines 44-47
	// ListViewItemPresenter::get_ListViewItemPresenterPaddingImpl
	// ListViewItemPresenter::put_ListViewItemPresenterPaddingImpl
	// These legacy properties delegate to ContentPresenter.Padding.
	// Already implemented in ListViewItemPresenter.Properties.cs

	// MUX Reference ListViewItemPresenter_Partial.cpp, lines 52-59
	// ListViewItemPresenter::get_ListViewItemPresenterHorizontalContentAlignmentImpl
	// ListViewItemPresenter::put_ListViewItemPresenterHorizontalContentAlignmentImpl
	// Already implemented in ListViewItemPresenter.Properties.cs

	// MUX Reference ListViewItemPresenter_Partial.cpp, lines 60-67
	// ListViewItemPresenter::get_ListViewItemPresenterVerticalContentAlignmentImpl
	// ListViewItemPresenter::put_ListViewItemPresenterVerticalContentAlignmentImpl
	// Already implemented in ListViewItemPresenter.Properties.cs

#if !HAS_UNO
	// TODO Uno: Debug-only SetRoundedListViewBaseItemChromeFallbackColor and
	// SetRoundedListViewBaseItemChromeFallbackColors are not ported.
	// These set default fallback light theme colors for testing purposes when
	// the rounded corner style is forced. Lines 69-126.
#endif
}
